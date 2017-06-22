using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;
using JetBrains.Annotations;
using NLog;

namespace ActorTestingFramework
{
    public class TestingActorRuntime : IActorRuntime, ITestingRuntime
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, ActorId> taskIdToActorId =
            new Dictionary<int, ActorId>();

        private int nextActorId;

        private readonly Dictionary<ActorId, ActorInfo> actors =
            new Dictionary<ActorId, ActorInfo>();

        private readonly List<ActorInfo> actorList = new List<ActorInfo>();

        private volatile bool terminated;

        private volatile bool sleepSetBlocked;

        private Exception error = null;

        private readonly IScheduler scheduler;

        public bool allowTaskQueue = true;

        public bool WasSleepSetBlocked => sleepSetBlocked;

        public TestingActorRuntime(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        #region Implementation of IActorRuntime

        public void RegisterMainTask(Task mainTask)
        {
            CreateActor(
                mainTask,
                null,
                "MainTask");
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

        public void TaskQueued(Task task, Action action)
        {
            CheckTerminated(OpType.CREATE);

            if (!taskIdToActorId.ContainsKey(task.Id))
            {
                // Task was created externally.
                LOGGER.Trace($"TaskQueued {task.Id}");

                if (!allowTaskQueue)
                {
                    throw new InvalidOperationException("A Task was queued outside of the testing runtime!");
                }

                Schedule(OpType.CREATE, TargetType.Thread, - 1);

                var actorInfo = CreateActor(task, null);
                var oldAction = action;
                action = delegate
                {
                    ActorBody(() =>
                    {
                        oldAction();
                        return (object) null;
                    },
                        this,
                        false,
                        actorInfo);
                };

                LaunchThread(action, task);

                lock (actorInfo.mutex)
                {
                    while (actorInfo.active)
                    {
                        Monitor.Wait(actorInfo.mutex);
                    }
                }
            }
            else
            {
                LaunchThread(action, task);
            }
        }

        public IMailbox<object> Create<TResult>(Func<TResult> entryPoint, string name = null)
        {
            var res = CreateActor(entryPoint, name);

            return res.Mailbox;
        }

        public IMailbox<T> CreateMailbox<T>()
        {
            if (Task.CurrentId == null)
            {
                InternalError(
                    "Cannot call actor operation from non-Task context");
            }
            return new Mailbox<T>(GetCurrentActorInfo(), this);
        }

        public IMailbox<object> CurrentMailbox()
        {
            return GetCurrentActorInfo().Mailbox;
        }

        public Task<T> StartNew<T>(Func<T> func, string name = null)
        {
            var res = CreateActor(func, name);

            return (Task<T>) res.task;
        }

        public void Sleep(int millisecondsTimeout)
        {
            Yield();
        }

        public void Yield()
        {
            var info = GetCurrentActorInfo();
            info.enabled = false;
            Schedule(OpType.Yield, TargetType.Thread, info.id.id);
        }

        public IMailbox<object> MailboxFromTask(Task task)
        {
            return GetActorInfo(task.Id).Mailbox;
        }

        public void WaitForActor(IMailbox<object> mailbox)
        {
            var actorInfo = GetCurrentActorInfo();
            var otherInfo = ((Mailbox<object>) mailbox).ownerActorInfo;
            WaitHelper(actorInfo, otherInfo);
            ThrowWaitExceptionIfNeeded(otherInfo);
        }

        public void WaitForActor(Task task, bool throwExceptions = true)
        {
            var actorInfo = GetCurrentActorInfo();
            var otherInfo = GetActorInfoUnsafe(task.Id);
            if (otherInfo == null)
            {
                if (!task.Wait(1000))
                {
                    InternalError("Task was not created by us and was not completed.");
                }
                return;
            }

            WaitHelper(actorInfo, otherInfo);
            // Need to also wait for the actual task to end
            // so that threads can access Result, Status, etc.
            try
            {
                task.Wait();
            }
            catch (AggregateException)
            {
            }

            if (throwExceptions)
            {
                ThrowWaitExceptionIfNeeded(otherInfo);
            }
        }

        public void CancelSelf()
        {
            var actorInfo = GetCurrentActorInfo();
            if (actorInfo.cts == null)
            {
                InternalError("Missing cancellation token source!");
            }
            actorInfo.cts.Cancel();
            actorInfo.cts.Token.ThrowIfCancellationRequested();
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
            Schedule(OpType.WaitForDeadlock, TargetType.Thread, actorInfo.id.id);
        }

        #endregion

        private void WaitHelper(ActorInfo actorInfo, ActorInfo otherInfo)
        {
            if (!otherInfo.terminated)
            {
                actorInfo.enabled = false;
                otherInfo.terminateWaiters.Add(actorInfo);
            }

            Schedule(OpType.JOIN, TargetType.Thread, otherInfo.id.id);

            Safety.Assert(otherInfo.terminated);
        }

        private static void ThrowWaitExceptionIfNeeded(ActorInfo otherInfo)
        {
            if (otherInfo.exceptions.Count > 0)
            {
                throw new AggregateException(otherInfo.exceptions);
            }
        }

        public Exception GetError()
        {
            return error;
        }

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
            bool mainThread,
            ActorInfo info = null)
        {
            if (info == null)
            {
                info = runtime.GetCurrentActorInfo();
            }
            lock (info.mutex)
            {
                Safety.Assert(info.active);
                info.currentOp = OpType.START;
                info.currentOpTarget = info.id.id;

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
            catch (OperationCanceledException ex)
            {
                lock (info.mutex)
                {

                    if (info.cts.Token ==
                        ex.CancellationToken &&
                        info.cts.IsCancellationRequested)
                    {
                        info.cancelled = true;
                    }
                    info.exceptions.Add(ex);
                }
                throw;
            }
            catch (ActorTerminatedException ex)
            {
                if (mainThread && runtime.error == null && !runtime.sleepSetBlocked)
                {
                    runtime.error = new Exception("Main actor did not terminate.", ex);
                }
            }
            catch (Exception ex)
            {
                runtime.error = ex;
                runtime.terminated = true;
                runtime.ActivateAllActors();
            }
            finally
            {
                try
                {
                    if (!runtime.terminated)
                    {
                        runtime.Schedule(OpType.END, TargetType.Thread, info.id.id, info);

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

                        runtime.Schedule(OpType.END, TargetType.Thread, info.id.id, info);
                    }

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

        private ActorInfo CreateActor(
            Task actorTask,
            CancellationTokenSource cts,
            string name = null)
        {
            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(actorTask.Id, actorId);
            ActorInfo actorInfo = new ActorInfo(
                actorId,
                name,
                actorTask,
                cts,
                this);
            actors.Add(actorId, actorInfo);
            actorList.Add(actorInfo);
            return actorInfo;
        }

        private ActorInfo CreateActor<T>(Func<T> func, string name)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            Schedule(OpType.CREATE, TargetType.Thread , - 1);

            CancellationTokenSource cts = new CancellationTokenSource();
            Task<T> actorTask = new Task<T>(() => ActorBody(func, this, false), cts.Token);
            ActorInfo actorInfo = CreateActor(actorTask, cts, name);

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


        public int GetCurrentSchedulerStep()
        {
            return scheduler.GetNumSteps();
        }

//        public ulong HashOperation(ActorInfo nextActor)
//        {
//        }

        public void Schedule(OpType opType, TargetType targetType, int opTarget, ActorInfo currentActor = null)
        {
            if (currentActor == null)
            {
                currentActor = GetCurrentActorInfo();
            }

            if (CheckTerminated(opType))
            {
                return;
            }

            currentActor.currentOp = opType;
            currentActor.currentOpTargetType = targetType;
            currentActor.currentOpTarget = opTarget;
            ActorInfo nextActor;
            var res = scheduler.GetNext(actorList, currentActor, out nextActor);

//            if (nextActor == null)
//            {
//                bool changed = false;
//                foreach (
//                    var waiter in
//                        actorList.Where(info => info.waitingForDeadlock))
//                {
//                    waiter.waitingForDeadlock = false;
//                    waiter.enabled = true;
//                    changed = true;
//                }
//                if (changed)
//                {
//                    nextActor = scheduler.GetNext(actorList, currentActor);
//                }
//            }

            if (nextActor == null)
            {
                if (res == NextActorResult.SleepsetBlocked)
                {
                    Console.Write("\nSleep set blocked.\n");
                    sleepSetBlocked = true;
                }
                // Deadlock
                terminated = true;
                ActivateAllActors();

                CheckTerminated(opType);
                return;
            }

            // TODO: Hash



            if (nextActor == currentActor)
            {
                return;
            }

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

        private void ActivateAllActors()
        {
            foreach(var info in actorList)
            {
                lock (info.mutex)
                {
                    info.active = true;
                    info.enabled = true;
                    Monitor.PulseAll(info.mutex);
                }
            }
        }

        private bool CheckTerminated(OpType opType)
        {
            if (terminated)
            {
                throw new ActorTerminatedException();
            }
            return terminated;
        }

        public ActorInfo GetCurrentActorInfo()
        {
            if (Task.CurrentId == null)
            {
                InternalError(
                    "Cannot call actor operation from non-Task context");
            }

            return GetActorInfo(Task.CurrentId.Value);
        }

        public ActorInfo GetActorInfoUnsafe(int taskId)
        {
            ActorId actorId;
            taskIdToActorId.TryGetValue(taskId, out actorId);

            if (actorId == null)
            {
                return null;
            }

            return actors[actorId];
        }

        public ActorInfo GetActorInfo(int taskId)
        {
//            CheckTerminated(OpType.INVALID);
            var res = GetActorInfoUnsafe(taskId);
            if (res == null)
            {
                InternalError();
            }
            return res;
        }

        private void LaunchThread(Action action, Task task)
        {
            
            Thread thread = new Thread(() =>
            {
                action();

            })
            {
                Name = $"TaskId({task.Id})"
            };
            thread.Start();
        }

        [ContractAnnotation(" => halt")]
        public void InternalError(string message = null)
        {
            Trace.Assert(false, message);
        }

    }
}