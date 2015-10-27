namespace TypedActorInterface
{
    public interface ITypedActorRuntime
    {
        T Create<T>(T actorInstance) where T : ITypedActor;
    }
}