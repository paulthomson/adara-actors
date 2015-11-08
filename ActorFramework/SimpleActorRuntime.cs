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

        public IMailbox<object> Create(IActor actorInstance, string name = null)
        {
            // Ensure that calling Task has an id.
            GetCurrentActorInfo();

            lock (mutex)
            {
                var actorTask = new Task(
                    () => { ActorBody(actorInstance, this); });

                ActorInfo actorInfo = CreateActor(actorTask.Id, name);
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

            return new Mailbox<T>(GetCurrentActorInfo(), this);
        }

        public IMailbox<object> CurrentMailbox()
        {
            return GetCurrentActorInfo().Mailbox;
        }

        public void AssignNameToCurrent(string name)
        {
            GetCurrentActorInfo().name = name;
        }

        #endregion

        private ActorInfo CreateActor(int taskId, string name = null)
        {
            ActorId actorId = new ActorId(nextActorId++);
            taskIdToActorId.Add(taskId, actorId);
            ActorInfo res = new ActorInfo(actorId, name, taskId, this);
            actors.Add(actorId, res);
            return res;
        }

        private static void ActorBody(
            IActor actor,
            IActorRuntime runtime)
        {
            actor.EntryPoint(runtime);
        }

        public ActorInfo GetCurrentActorInfo()
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
                    return CreateActor(taskId);
                }

                return actors[actorId];
            }
        }

    }
}