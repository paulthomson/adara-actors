using TypedActorInterface;

namespace TypedActorFramework
{
    public interface IActorRuntimeInternal
    {
        void Send(ITypedActor id, object msg);
        T GetActorProxy<T>(ActorId<T> id) where T : ITypedActor;
        ActorId<T> GetActorId<T>(T actor) where T : ITypedActor;
    }
}