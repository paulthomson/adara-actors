namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create(IActor actorInstance, string name = null);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();

        void AssignNameToCurrent(string name);
    }
}