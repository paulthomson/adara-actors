using ActorInterface;
using TypedActorInterface;

namespace TypedActorFramework
{
    public class TypeActorActor : IActor
    {
        private ITypedActor typedActor;

        #region Implementation of IActor

        public void EntryPoint(IActorRuntime runtime)
        {
            var mailbox = runtime.CurrentMailbox();

            typedActor = (ITypedActor) mailbox.Receive();

            while (true)
            {
                var msg = (ICallable) mailbox.Receive();
                msg.Call(typedActor);
            }

        }

        #endregion
    }
}