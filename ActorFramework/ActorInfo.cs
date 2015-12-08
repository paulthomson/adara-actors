
using System.Threading;
using System.Threading.Tasks;

namespace ActorFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;
        public readonly Task task;

        public Mailbox<object> Mailbox { get; }

        public string name;

        public CancellationTokenSource cts;

        public ActorInfo(ActorId id, string name, Task task, CancellationTokenSource cts, SimpleActorRuntime runtime)
        {
            this.id = id;
            this.name = name;
            this.task = task;
            this.cts = cts;
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