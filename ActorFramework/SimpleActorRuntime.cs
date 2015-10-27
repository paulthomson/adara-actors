using System;
using System.Collections.Generic;
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

        public IMailbox<object> Create(IEntryPoint entryPoint)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            lock (mutex)
            {
                ActorId actorId = new ActorId(nextActorId++);
                var actorTask = new Task(() => { ActorBody(entryPoint, this); });
                ActorInfo actorInfo = new ActorInfo(actorId, actorTask.Id);

                taskIdToActorId.Add(actorTask.Id, actorId);
                actors.Add(actorId, actorInfo);

                actorTask.Start();

                return actorInfo.Mailbox;
            }
        }

        public IMailbox<T> CreateMailbox<T>()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }
            return new Mailbox<T>(Task.CurrentId.Value);
        }

        public IMailbox<object> CurrentMailbox()
        {
            return GetCurrentActorInfo().Mailbox;
        }

        #endregion

        private static void ActorBody(
            IEntryPoint entryPoint,
            IActorRuntime runtime)
        {
            entryPoint.EntryPoint(runtime);
        }

        private ActorInfo GetCurrentActorInfo()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException(
                    "Cannot call actor operation from non-Task context");
            }
            int taskId = Task.CurrentId.Value;
            lock (mutex)
            {

                ActorId actorId;
                taskIdToActorId.TryGetValue(taskId, out actorId);

                if (actorId == null)
                {
                    actorId = new ActorId(nextActorId++);
                    taskIdToActorId.Add(taskId, actorId);
                    actors.Add(actorId, new ActorInfo(actorId, taskId));
                }

                return actors[actorId];
            }
        }

    }
}