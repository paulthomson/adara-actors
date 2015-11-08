
namespace ActorFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;
        public readonly int taskId;

        public Mailbox<object> Mailbox { get; }

        public string name;

        public ActorInfo(ActorId id, string name, int taskId, SimpleActorRuntime runtime)
        {
            this.id = id;
            this.name = name;
            this.taskId = taskId;
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