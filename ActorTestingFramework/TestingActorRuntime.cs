using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestingActorRuntime : IActorRuntime
    {
        private readonly Dictionary<int, ActorId> taskIdToActorId =
            new Dictionary<int, ActorId>();

        private int nextActorId;

        private readonly Dictionary<ActorId, ActorInfo> actors =
            new Dictionary<ActorId, ActorInfo>();

        private readonly List<ActorInfo> actorList = new List<ActorInfo>();

        private bool terminated;

        private readonly IScheduler scheduler;

        public TestingActorRuntime(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        #region Implementation of IActorRuntime

        public IMailbox<object> Create(IActor actorInstance)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            var actorTask = new Task(
                () => { ActorBody(actorInstance, this, false); });

            var actorInfo = CreateActor(actorTask.Id, true);
            actorTask.Start();

            lock (actorInfo.mutex)
            {
                while (actorInfo.active)
                {
                    Monitor.Wait(actorInfo.mutex);
                }
            }

            return actorInfo.Mailbox;
        }

        public IMailbox<T> CreateMailbox<T>()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }
            return new Mailbox<T>(Task.CurrentId.Value, this);
        }

        public IMailbox<object> CurrentMailbox()
        {
            return GetCurrentActorInfo().Mailbox;
        }

        #endregion

        public void WaitForAllActorsToTerminate()
        {
            foreach (var info in actorList)
            {
                while (true)
                {
                    lock (info.mutex)
                    {
                        if (info.terminated)
                        {
                            break;
                        }
                        Thread.Yield();
                    }

                }
            }
        }

        public static void ActorBody(
            IActor actor,
            TestingActorRuntime runtime,
            bool mainThread)
        {
            ActorInfo info = runtime.GetCurrentActorInfo();

            try
            {
                lock (info.mutex)
                {
                    Safety.Assert(info.active);
                    info.currentOp = OpType.START;

                    if (!mainThread)
                    {
                        info.active = false;
                        Monitor.PulseAll(info.mutex);

                        while (!info.active)
                        {
                            Monitor.Wait(info.mutex);
                        }
                    }
                }
                actor.EntryPoint(runtime);

                runtime.Schedule(OpType.END);

                lock (info.mutex)
                {
                    info.enabled = false;
                }

                runtime.Schedule(OpType.END);
            }
            catch (ActorTerminatedException)
            {
                
            }

            lock (info.mutex)
            {
                info.terminated = true;
            }
        }

        private ActorInfo CreateActor(int taskId, bool schedule)
        {
            if (schedule)
            {
                Schedule(OpType.CREATE);
            }
            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(taskId, actorId);
            ActorInfo res = new ActorInfo(actorId, taskId, this);
            actors.Add(actorId, res);
            actorList.Add(res);
            return res;
        }

        public void Schedule(OpType opType)
        {
            ActorInfo currentActor = GetCurrentActorInfo();

            if (CheckTerminated(opType))
            {
                return;
            }

            currentActor.currentOp = opType;

            ActorInfo nextActor = scheduler.GetNext(actorList, currentActor);

            if (nextActor == null)
            {
                // Deadlock
                terminated = true;
                ActivateAllActors();

                if (CheckTerminated(opType))
                {
                    return;
                }
            }

            if (nextActor != currentActor)
            {
                Safety.Assert(currentActor.active);
                currentActor.active = false;

                lock (nextActor.mutex)
                {
                    Safety.Assert(nextActor.enabled);

                    Safety.Assert(!nextActor.active);
                    nextActor.active = true;

                    Monitor.PulseAll(nextActor.mutex);
                }

                lock (currentActor.mutex)
                {
                    while (!currentActor.active)
                    {
                        Monitor.Wait(currentActor.mutex);
                    }

                    if (CheckTerminated(opType))
                    {
                        return;
                    }

                    Safety.Assert(currentActor.enabled);
                    Safety.Assert(currentActor.active);
                }
            }
        }

        private void ActivateAllActors()
        {
            foreach(var info in actorList)
            {
                lock (info.mutex)
                {
                    info.active = true;
                    info.enabled = false;
                    Monitor.PulseAll(info.mutex);
                }
            }
        }

        private bool CheckTerminated(OpType opType)
        {
            if (terminated && opType != OpType.END)
            {
                throw new ActorTerminatedException();
            }
            return terminated;
        }

        public ActorInfo GetCurrentActorInfo()
        {
            CheckTerminated(OpType.INVALID);

            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }
            int taskId = Task.CurrentId.Value;
            ActorId actorId;
            taskIdToActorId.TryGetValue(taskId, out actorId);

            if (actorId == null)
            {
                return CreateActor(taskId, false);
            }

            return actors[actorId];
        }

    }
}