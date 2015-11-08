using ActorInterface;

namespace TypedActorInterface
{
    public interface ITypedActorRuntime
    {
        T Create<T>(T typeActorInstance, string name = null) 
            where T : ITypedActor;

        T ProxyFromMailbox<T>(IMailbox<object> mailbox) 
            where T : ITypedActor;

        void ReceiveCall<T>(IMailbox<object> mailbox, T actorInstance)
            where T : ITypedActor;
    }
}