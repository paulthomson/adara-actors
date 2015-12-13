using System;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorHelpers
{
    public class TaskCompletionSourceHelper : ITaskCompletionSource
    {
        public class Msg
        {
            public object result;
            public Exception exception;
            public TaskStatus state;

            public Msg(object result, Exception exception, TaskStatus state)
            {
                this.result = result;
                this.exception = exception;
                this.state = state;
            }
        }

        private object result;
        private TaskStatus state = TaskStatus.Running;
        private readonly IMailbox<object> taskMailbox;
        private readonly Task task;
        private readonly IActorRuntime runtime;

        public TaskCompletionSourceHelper(
            IActorRuntime runtime,
            IMailbox<object> taskMailbox,
            Task task)
        {
            this.runtime = runtime;
            this.taskMailbox = taskMailbox;
            this.task = task;
        }

        #region Implementation of ITaskCompletionSource

        public object SetResult(object res)
        {
            if (InFinalState(state))
            {
                throw new InvalidOperationException();
            }
            
            result = res;
            state = TaskStatus.RanToCompletion;
            taskMailbox.Send(new Msg(result, null, state));
            runtime.WaitForActor(task, false);
            return null;
        }

        public bool TrySetResult(object res)
        {
            if (InFinalState(state))
            {
                return false;
            }
            result = res;
            state = TaskStatus.RanToCompletion;
            taskMailbox.Send(new Msg(result, null, state));
            runtime.WaitForActor(task, false);
            return true;
        }

        public object SetCanceled()
        {
            if (InFinalState(state))
            {
                throw new InvalidOperationException();
            }

            result = null;
            state = TaskStatus.Canceled;
            taskMailbox.Send(new Msg(result, null, state));
            runtime.WaitForActor(task, false);
            return null;
        }

        public object SetException(Exception exception)
        {
            if (InFinalState(state))
            {
                throw new InvalidOperationException();
            }

            result = null;
            state = TaskStatus.Faulted;
            taskMailbox.Send(new Msg(result, exception, state));
            runtime.WaitForActor(task, false);
            return null;
        }

        #endregion

        private static bool InFinalState(TaskStatus state)
        {
            return state == TaskStatus.Canceled ||
                   state == TaskStatus.Faulted ||
                   state == TaskStatus.RanToCompletion;
        }

    }
}