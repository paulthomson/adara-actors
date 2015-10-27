using TypedActorInterface;

namespace TypedActorFramework
{
    public class ActorId<T> where T : ITypedActor
    {
        public int id;
    }
}