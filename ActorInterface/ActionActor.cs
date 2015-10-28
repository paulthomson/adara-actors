using System;

namespace ActorInterface
{
    public class ActionActor : IActor
    {
        private readonly Action<IActorRuntime> action;

        public ActionActor(Action<IActorRuntime> action)
        {
            this.action = action;
        }

        #region Implementation of IActor

        public void EntryPoint(IActorRuntime runtime)
        {
            action(runtime);
        }

        #endregion
    }
}