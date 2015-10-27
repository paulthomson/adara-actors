using ActorInterface;

namespace ActorTestingFramework
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