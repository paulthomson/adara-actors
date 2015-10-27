namespace ActorInterface
{
    public interface IActorRuntime
    {
        IActorId Create(IEntryPoint entryPoint);
        void Join(IActorId actorId);

        void Send(IActorId actorId, object msg);
        object Receive();
        
        void CurrentId();
    }
}