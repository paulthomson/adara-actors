using ActorInterface;
using TypedActorInterface;

namespace TypedActorFramework
{
    public class TypeActorEntryPoint : IEntryPoint
    {
        private readonly ITypedActor typedActor;

        public TypeActorEntryPoint(ITypedActor typedActor)
        {
            this.typedActor = typedActor;
        }

        #region Implementation of IEntryPoint

        public void EntryPoint(IActorRuntime runtime)
        {
            var mailbox = runtime.CurrentMailbox();

            while (true)
            {
                var msg = (ICallable) mailbox.Receive();
                msg.Call(typedActor);
            }

        }

        #endregion
    }
}