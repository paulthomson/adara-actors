using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestingActorRuntime : ITestingActorRuntime
    {
        private readonly Dictionary<int, ActorId> taskIdToActorId =
            new Dictionary<int, ActorId>();

        private int nextActorId;

        private readonly Dictionary<ActorId, ActorInfo> actors =
            new Dictionary<ActorId, ActorInfo>();

        private readonly List<ActorInfo> actorList = new List<ActorInfo>();

        private IScheduler scheduler = new RandomScheduler();

        #region Implementation of IActorRuntime

        public IMailbox<object> Create(IActor actorInstance)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            var actorTask = new Task(
                () => { ActorBody(actorInstance, this); });

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

        public void PrepareForNextSchedule()
        {
            scheduler.NextSchedule();
            taskIdToActorId.Clear();
            nextActorId = 0;
            actors.Clear();
            actorList.Clear();
        }

        public void Wait()
        {
            ActorInfo currentInfo = GetCurrentActorInfo();
            currentInfo.enabled = false;

            Schedule(OpType.END);
        }

        #endregion

        private static void ActorBody(
            IActor actor,
            TestingActorRuntime runtime)
        {
            ActorInfo info = runtime.GetCurrentActorInfo();

            try
            {
                lock (info.mutex)
                {
                    Safety.Assert(info.active);
                    info.currentOp = OpType.START;
                    info.active = false;
                    Monitor.PulseAll(info.mutex);

                    while (!info.active)
                    {
                        Monitor.Wait(info.mutex);
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

            if (currentActor.terminated)
            {
                throw new ActorTerminatedException();
            }

            currentActor.currentOp = opType;

            ActorInfo nextActor = scheduler.GetNext(actorList, currentActor);

            if (nextActor == null)
            {
                // Deadlock
                TerminateAll();
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

                    if (currentActor.terminated)
                    {
                        throw new ActorTerminatedException();
                    }

                    Safety.Assert(currentActor.enabled);
                    Safety.Assert(currentActor.active);
                }
            }
        }

        private void TerminateAll()
        {
            foreach(var info in actorList)
            {
                lock (info.mutex)
                {
                    info.active = true;
                    info.enabled = false;
                    info.terminated = true;
                    Monitor.PulseAll(info.mutex);
                }
            }
            throw new ActorTerminatedException();
        }

        public ActorInfo GetCurrentActorInfo()
        {
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