namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create(IEntryPoint entryPoint);
        // void Join(IActorId actorId);
        // void Send(IActorId actorId, object msg);
        // object Receive();

        IMailbox<T> CreateMailbox<T>();
            
        IMailbox<object> CurrentMailbox();
    }
}