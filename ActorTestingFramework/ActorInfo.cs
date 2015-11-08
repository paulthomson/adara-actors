
namespace ActorTestingFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;
        public readonly int taskId;
        public string name;
        public readonly Mailbox<object> Mailbox;

        public bool active = true;
        public bool enabled = true;
        public bool terminated = false;
        public OpType currentOp = OpType.INVALID;

        public object mutex = new object();

        public ActorInfo(
            ActorId id,
            string name,
            int taskId,
            TestingActorRuntime runtime)
        {
            this.id = id;
            this.taskId = taskId;
            this.name = name;
            Mailbox = new Mailbox<object>(this, runtime);
        }

        #region Overrides of Object

        public override string ToString()
        {
            return $"{name ?? "Unnamed"} ({id.id})";
        }

        #endregion
    }
}