using System.Collections.Generic;
using ActorInterface;

namespace ActorHelpers
{
    public class MonitorActor : IMonitor
    {
        private readonly List<IMailbox<object>> lockQueue = new List<IMailbox<object>>();
        private readonly List<IMailbox<object>> waitQueue = new List<IMailbox<object>>();
        private IMailbox<object> owner;

        #region Implementation of IMonitor

        public void LockMutex(IMailbox<object> me)
        {
            if (owner == null)
            {
                owner = me;
                me.Send(new object());
                return;
            }
            
            lockQueue.Add(me);
        }

        public void UnlockMutex(IMailbox<object> me)
        {
            Safety.Assert(owner == me);
            owner = null;
            if (lockQueue.Count <= 0) return;
            owner = lockQueue[0];
            lockQueue.RemoveAt(0);
            owner.Send(new object());
        }

        public void WaitMutex(IMailbox<object> me)
        {
            UnlockMutex(me);
            waitQueue.Add(me);
        }

        public void PulseAllMutex(IMailbox<object> me)
        {
            Safety.Assert(owner == me);
            foreach (var mailbox in waitQueue)
            {
                mailbox.Send(new object());
            }
            waitQueue.Clear();
        }

        #endregion

    }
}