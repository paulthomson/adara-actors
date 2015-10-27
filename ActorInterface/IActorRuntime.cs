namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create(IActor actorInstance);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();
    }
}