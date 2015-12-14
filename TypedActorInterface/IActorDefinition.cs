namespace TypedActorInterface
{
    public interface IActorDefinition<T>
    {
        T InitialState { get; }
    }
}