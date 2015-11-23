using System;
using System.Threading.Tasks;

namespace ActorInterface
{
    public interface IActorRuntime
    {

        IMailbox<object> Create(IActor actorInstance, string name = null);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();

        Task StartNew(Action action, string name = null);
        Task<T> StartNew<T>(Func<T> func, string name = null);

        void Sleep(int millisecondsTimeout);

        IMailbox<object> MailboxFromTask(Task task);

        void WaitForActor(IMailbox<object> mailbox);
        void WaitForActor(Task task);

        void RegisterMainTask(Task mainTask);

        Task StartMain(Action action);
        Task<T> StartMain<T>(Func<T> func);

        void AssignNameToCurrent(string name);
    }
}