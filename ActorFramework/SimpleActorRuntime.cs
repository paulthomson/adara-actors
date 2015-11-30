using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorFramework
{
    public class SimpleActorRuntime : IActorRuntime
    {
        private readonly Dictionary<int, ActorId> taskIdToActorId =
            new Dictionary<int, ActorId>();

        private int nextActorId;

        private readonly Dictionary<ActorId, ActorInfo> actors =
            new Dictionary<ActorId, ActorInfo>();

        private readonly object mutex = new object();

        #region Implementation of IActorRuntime

        public void CancelSelf()
        {
            throw new NotImplementedException();
        }

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

        public IMailbox<object> Create<TResult>(Func<TResult> entryPoint, string name = null)
        {
            var res = CreateActor(entryPoint, name);

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

        public Task<T> StartNew<T>(Func<T> func, string name = null)
        {
            var res = CreateActor(func, name);

            return (Task<T>) res.task;
        }

        public void Sleep(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }

        public IMailbox<object> MailboxFromTask(Task task)
        {
            return GetActorInfo(task.Id).Mailbox;
        }

        public void WaitForActor(IMailbox<object> mailbox)
        {
            ((Mailbox<object>) mailbox).ownerActorInfo.task.Wait();
        }

        public void WaitForActor(Task task)
        {
            task.Wait();
        }

        public void AssignNameToCurrent(string name)
        {
            GetCurrentActorInfo().name = name;
        }

        #endregion

        private ActorInfo CreateActor<T>(Func<T> func, string name)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            lock (mutex)
            {
                Task<T> actorTask = new Task<T>(
                    func);

                var actorInfo = CreateActor(actorTask, name);

                actorTask.Start();
                return actorInfo;
            }
        }

        private ActorInfo CreateActor(Task actorTask, string name = null)
        {
            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(actorTask.Id, actorId);
            ActorInfo actorInfo = new ActorInfo(
                actorId,
                name,
                actorTask,
                this);
            actors.Add(actorId, actorInfo);
            return actorInfo;
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
            lock (mutex)
            {
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
}