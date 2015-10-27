namespace TypedActorInterface
{
    public interface ITypedActorRuntime
    {
        T Create<T>(T typeActorInstance) where T : ITypedActor;
    }
}