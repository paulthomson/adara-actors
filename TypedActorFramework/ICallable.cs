using TypedActorInterface;

namespace TypedActorFramework
{
    public interface ICallable
    {
        void Call(ITypedActor actorProxy);
    }
}