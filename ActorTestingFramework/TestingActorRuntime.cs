using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestingActorRuntime : IActorRuntime, ITestingRuntime
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

        public void RegisterMainTask(Task mainTask)
        {
            CreateActor(mainTask, "MainTask");
        }

        public Task StartMain(Action action)
        {
            Task task = new Task(action);
            RegisterMainTask(task);
            task.Start();
            return task;
        }

        public Task<T> StartMain<T>(Func<T> func)
        {
            Task<T> task = new Task<T>(func);
            RegisterMainTask(task);
            task.Start();
            return task;
        }

        public IMailbox<object> Create(IActor actorInstance, string name = null)
        {
            var res = CreateActor<object>(() =>
            {
                actorInstance.EntryPoint(this);
                return null;
            }, name);

            return res.Mailbox;
        }

        public IMailbox<T> CreateMailbox<T>()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }
            return new Mailbox<T>(GetCurrentActorInfo(), this);
        }

        public IMailbox<object> CurrentMailbox()
        {
            return GetCurrentActorInfo().Mailbox;
        }

        public Task StartNew(Action action, string name = null)
        {
            var res = CreateActor<object>(() =>
            {
                action();
                return null;
            },
                name);

            return res.task;
        }

        public Task<T> StartNew<T>(Func<T> func, string name = null)
        {
            var res = CreateActor<T>(func, name);

            return (Task<T>) res.task;
        }

        public void Sleep(int millisecondsTimeout)
        {
            // TODO: Perhaps yield?
        }

        public IMailbox<object> MailboxFromTask(Task task)
        {
            return GetActorInfo(task.Id).Mailbox;
        }

        public void WaitForActor(IMailbox<object> mailbox)
        {
            var actorInfo = GetCurrentActorInfo();
            var otherInfo = ((Mailbox<object>) mailbox).ownerActorInfo;

            if (!otherInfo.terminated)
            {
                actorInfo.enabled = false;
            }

            Schedule(OpType.JOIN);

            Safety.Assert(otherInfo.terminated);
        }

        public void WaitForActor(Task task)
        {
            var actorInfo = GetCurrentActorInfo();
            var otherInfo = GetActorInfo(task.Id);

            if (!otherInfo.terminated)
            {
                actorInfo.enabled = false;
                otherInfo.terminateWaiters.Add(actorInfo);
            }

            Schedule(OpType.JOIN);

            Safety.Assert(otherInfo.terminated);
        }

        public void AssignNameToCurrent(string name)
        {
            GetCurrentActorInfo().name = name;
        }

        #endregion

        #region Implementation of ITestingRuntime

        public void WaitForDeadlock()
        {
            var actorInfo = GetCurrentActorInfo();
            actorInfo.enabled = false;
            actorInfo.waitingForDeadlock = true;
            Schedule(OpType.WaitForDeadlock);
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

        public static T ActorBody<T>(
            Func<T> func,
            TestingActorRuntime runtime,
            bool mainThread)
        {
            ActorInfo info = runtime.GetCurrentActorInfo();

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

            try
            {
                return func();
            }
            catch (ActorTerminatedException)
            {
                
            }
            finally
            {
                try
                {
                    runtime.Schedule(OpType.END);

                    lock (info.mutex)
                    {
                        info.enabled = false;
                        info.terminated = true;
                        foreach (var waiter in info.terminateWaiters)
                        {
                            waiter.enabled = true;
                        }
                        info.terminateWaiters.Clear();
                    }

                    runtime.Schedule(OpType.END);
                }
                catch (ActorTerminatedException)
                {
                    
                }
                finally
                {
                    lock (info.mutex)
                    {
                        info.terminated = true;
                    }
                }

            }
            return default(T);
        }

        private ActorInfo CreateActor(Task actorTask, string name = null)
        {
            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(actorTask.Id, actorId);
            ActorInfo actorInfo = new ActorInfo(actorId, name, actorTask, this);
            actors.Add(actorId, actorInfo);
            actorList.Add(actorInfo);
            return actorInfo;
        }

        private ActorInfo CreateActor<T>(Func<T> func, string name)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            Task<T> actorTask = new Task<T>(() => ActorBody(func, this, false));

            Schedule(OpType.CREATE);

            ActorInfo actorInfo = CreateActor(actorTask, name);

            actorTask.Start();

            lock (actorInfo.mutex)
            {
                while (actorInfo.active)
                {
                    Monitor.Wait(actorInfo.mutex);
                }
            }

            return actorInfo;
        }


        public void Schedule(OpType opType)
        {
            ActorInfo currentActor = GetCurrentActorInfo();

            if (CheckTerminated(opType))
            {
                return;
            }

            currentActor.currentOp = opType;

            if (actorList.Count(info => info.enabled) == 0)
            {
                foreach (
                    var waiter in
                        actorList.Where(info => info.waitingForDeadlock))
                {
                    waiter.waitingForDeadlock = false;
                    waiter.enabled = true;
                }
            }

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
                    if (currentActor.terminated && opType == OpType.END)
                    {
                        return;
                    }

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
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }

            return GetActorInfo(Task.CurrentId.Value);
        }

        public ActorInfo GetActorInfo(int taskId)
        {
//            CheckTerminated(OpType.INVALID);
            
            ActorId actorId;
            taskIdToActorId.TryGetValue(taskId, out actorId);

            if (actorId == null)
            {
                throw new InvalidOperationException();
            }

            return actors[actorId];
        }

        
    }
}