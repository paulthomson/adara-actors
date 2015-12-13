using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ActorInterface
{
    public interface IActorRuntime
    {
        IMailbox<object> Create<TResult>(Func<TResult> entryPoint, string name = null);
        IMailbox<T> CreateMailbox<T>();
        IMailbox<object> CurrentMailbox();

        Task<TResult> StartNew<TResult>(Func<TResult> entryPoint, string name = null);

        void Sleep(int millisecondsTimeout);

        void Yield();

        IMailbox<object> MailboxFromTask(Task task);

        void WaitForActor(IMailbox<object> mailbox);
        void WaitForActor(Task task, bool throwExceptions = true);

        void CancelSelf();

        void RegisterMainTask(Task mainTask);

        Task StartMain(Action action);
        Task<T> StartMain<T>(Func<T> func);

        void TaskQueued(Task task, Action action);

        void AssignNameToCurrent(string name);

        [ContractAnnotation(" => halt")]
        void InternalError(string message = null);
    }
}