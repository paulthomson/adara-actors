using System.Threading.Tasks;

namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create(IActor actorInstance, string name = null);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();
        IMailbox<object> MailboxFromTask(Task task);

        void AssignNameToCurrent(string name);
    }
}