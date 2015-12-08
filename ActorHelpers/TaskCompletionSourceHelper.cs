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

        public TaskCompletionSourceHelper(IMailbox<object> taskMailbox)
        {
            this.taskMailbox = taskMailbox;
        }

        #region Implementation of ITaskCompletionSource

        public void SetResult(object res, IMailbox<object> rt)
        {
            if (CheckInvalidOperation(rt))
            {
                return;
            }
            
            result = res;
            state = TaskStatus.RanToCompletion;
            taskMailbox.Send(new Msg(result, null, state));
            rt.Send(null);
        }

        public bool TrySetResult(object res)
        {
            if (state == TaskStatus.Canceled ||
                state == TaskStatus.Faulted ||
                state == TaskStatus.RanToCompletion)
            {
                return false;
            }
            result = res;
            state = TaskStatus.RanToCompletion;
            taskMailbox.Send(new Msg(result, null, state));
            return true;
        }

        public void SetCanceled(IMailbox<object> rt)
        {
            if (CheckInvalidOperation(rt))
            {
                return;
            }

            result = null;
            state = TaskStatus.Canceled;
            taskMailbox.Send(new Msg(result, null, state));
            rt.Send(null);
        }

        public object SetException(Exception exception)
        {
            if (state == TaskStatus.Canceled ||
                state == TaskStatus.Faulted ||
                state == TaskStatus.RanToCompletion)
            {
                throw new InvalidOperationException();
            }

            result = null;
            state = TaskStatus.Faulted;
            taskMailbox.Send(new Msg(result, exception, state));
            return null;
        }

        #endregion

        private bool CheckInvalidOperation(IMailbox<object> rt)
        {
            if (state == TaskStatus.Canceled ||
                state == TaskStatus.Faulted ||
                state == TaskStatus.RanToCompletion)
            {
                rt.Send(new InvalidOperationException());
                return true;
            }
            return false;
        }
    }
}