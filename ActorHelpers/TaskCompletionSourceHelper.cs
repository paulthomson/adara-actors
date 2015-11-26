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
            public TaskStatus state;

            public Msg(object result, TaskStatus state)
            {
                this.result = result;
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
            taskMailbox.Send(new Msg(result, state));
            rt.Send(null);
        }

        public void SetCanceled(IMailbox<object> rt)
        {
            if (CheckInvalidOperation(rt))
            {
                return;
            }

            result = null;
            state = TaskStatus.Canceled;
            taskMailbox.Send(new Msg(result, state));
            rt.Send(null);
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