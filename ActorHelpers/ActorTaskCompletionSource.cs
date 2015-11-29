using System;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public class ActorTaskCompletionSource<T>
    {
        private readonly IActorRuntime runtime;
        private readonly ITaskCompletionSource source;

        public Task<T> Task { get; }

        public ActorTaskCompletionSource(IActorRuntime r, ITypedActorRuntime typedRuntime)
        {
            runtime = r;
            CancellationToken ct = new CancellationToken();

            Task = runtime.StartNew(() =>
            {
                TaskCompletionSourceHelper.Msg res =
                    (TaskCompletionSourceHelper.Msg)
                        runtime.CurrentMailbox().Receive();

                if(res.state == TaskStatus.Canceled)
                    throw new TaskCanceledException();

                return (T) res.result;
            });

            source =
                typedRuntime.Create<ITaskCompletionSource>(
                    new TaskCompletionSourceHelper(runtime.MailboxFromTask(Task)));
        }

        public void SetResult(T res)
        {
            var mailbox = runtime.CreateMailbox<object>();
            source.SetResult(res, mailbox);
            var ex = (Exception) mailbox.Receive();
            if (ex != null)
            {
                throw ex;
            }
        }

        public void SetCanceled()
        {
            var mailbox = runtime.CreateMailbox<object>();
            source.SetCanceled(mailbox);
            var ex = (Exception) mailbox.Receive();
            if (ex != null)
            {
                throw ex;
            }
        }


    }
}