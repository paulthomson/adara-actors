using System;
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

            Task = runtime.StartNew(() =>
            {
                TaskCompletionSourceHelper.Msg res =
                    (TaskCompletionSourceHelper.Msg)
                        runtime.CurrentMailbox().Receive();

                if (res.state == TaskStatus.Canceled)
                {
                    runtime.CancelSelf();
                }

                if (res.state == TaskStatus.Faulted)
                {
                    throw res.exception;
                }

                return (T) res.result;
            });

            source =
                typedRuntime.Create<ITaskCompletionSource>(
                    new TaskCompletionSourceHelper(runtime, runtime.MailboxFromTask(Task), Task));
        }

        public void SetResult(T res)
        {
            source.SetResult(res);
        }

        public bool TrySetResult(T res)
        {
            return source.TrySetResult(res);
        }

        public void SetException(Exception ex)
        {
            source.SetException(ex);
        }

        public void SetCanceled()
        {
            source.SetCanceled();
        }


    }
}