using ActorInterface;

namespace ActorFramework
{
    public class ActorId : IActorId
    {
        public readonly int id;

        public ActorId(int id)
        {
            this.id = id;
        }
    }
}