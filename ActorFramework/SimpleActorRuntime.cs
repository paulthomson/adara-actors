using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;
using JetBrains.Annotations;
using NLog;

namespace ActorFramework
{
    public class SimpleActorRuntime : IActorRuntime
    {
        private static Logger LOGGER = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, ActorId> taskIdToActorId =
            new Dictionary<int, ActorId>();

        private int nextActorId;

        private readonly Dictionary<ActorId, ActorInfo> actors =
            new Dictionary<ActorId, ActorInfo>();

        private readonly object mutex = new object();

        #region Implementation of IActorRuntime

        public void RegisterMainTask(Task mainTask)
        {
            CreateActor(mainTask, null, "MainTask");
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

        public void TaskQueued(Task task, ref Action action, string name = null)
        {
            if (!taskIdToActorId.ContainsKey(task.Id))
            {
                LOGGER.Trace($"TaskQueued {task.Id}");
                CreateActor(task, null);
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
            Thread.Sleep(millisecondsTimeout);
        }

        public void Yield()
        {
            Thread.Yield();
        }

        public IMailbox<object> MailboxFromTask(Task task)
        {
            return GetActorInfo(task.Id).Mailbox;
        }

        public void WaitForActor(IMailbox<object> mailbox)
        {
            ((Mailbox<object>) mailbox).ownerActorInfo.task.Wait();
        }

        public void WaitForActor(Task task, bool throwExceptions = true)
        {
            if (throwExceptions)
            {
                task.Wait();
            }
            else
            {
                try
                {
                    task.Wait();
                }
                catch (AggregateException)
                {
                }
            }
        }

        public void AssignNameToCurrent(string name)
        {
            GetCurrentActorInfo().name = name;
        }

        public void CancelSelf()
        {
            var actorInfo = GetCurrentActorInfo();
            actorInfo.cts.Cancel();
            actorInfo.cts.Token.ThrowIfCancellationRequested();
        }

        #endregion

        private ActorInfo CreateActor<T>(Func<T> func, string name)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            lock (mutex)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task<T> actorTask = new Task<T>(
                    func, cts.Token);

                var actorInfo = CreateActor(actorTask, cts, name);

                actorTask.Start();
                return actorInfo;
            }
        }

        private ActorInfo CreateActor(Task actorTask, CancellationTokenSource cts, string name = null)
        {
            int taskId = actorTask?.Id ?? Task.CurrentId.Value;

            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(taskId, actorId);
            ActorInfo actorInfo = new ActorInfo(
                actorId,
                name,
                actorTask,
                cts,
                this);
            actors.Add(actorId, actorInfo);
            return actorInfo;
        }

        public ActorInfo GetCurrentActorInfo()
        {
            if (Task.CurrentId == null)
            {
                InternalError(
                    "Cannot call actor operation from non-Task context");
            }
            var res = GetActorInfo(Task.CurrentId.Value);
            return res;
        }

        public ActorInfo GetActorInfo(int taskId)
        {
            lock (mutex)
            {
                ActorId actorId;
                taskIdToActorId.TryGetValue(taskId, out actorId);

                if (actorId == null)
                {
                    InternalError();
                }

                return actors[actorId];
            }
        }

        [ContractAnnotation(" => halt")]
        public void InternalError(string message = null)
        {
            Trace.Assert(false, message);
        }

    }
}