using System;
using System.Threading.Tasks;

namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create<TResult>(Func<TResult> entryPoint, string name = null);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();

        Task<TResult> StartNew<TResult>(Func<TResult> entryPoint, string name = null);

        void Sleep(int millisecondsTimeout);

        IMailbox<object> MailboxFromTask(Task task);

        void WaitForActor(IMailbox<object> mailbox);
        void WaitForActor(Task task);

        void CancelSelf();

        void RegisterMainTask(Task mainTask);

        Task StartMain(Action action);
        Task<T> StartMain<T>(Func<T> func);

        void TaskQueued(Task task, string name = null);

        void AssignNameToCurrent(string name);
    }
}