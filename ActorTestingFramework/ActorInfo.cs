
namespace ActorTestingFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;
        public readonly Mailbox<object> Mailbox;

        public bool active = true;
        public bool enabled = true;
        public bool terminated = false;
        public OpType currentOp = OpType.INVALID;

        public object mutex = new object();


        public ActorInfo(ActorId id, int owner, TestingActorRuntime runtime)
        {
            this.id = id;
            Mailbox = new Mailbox<object>(owner, runtime);
        }
    }
}