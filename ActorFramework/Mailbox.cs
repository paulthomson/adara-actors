using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorFramework
{
    public class Mailbox<T> : IMailbox<T>
    {
        private readonly int owner;
        private readonly IList<T> mailbox;
        private readonly object mutex;

        public Mailbox(int owner)
        {
            this.owner = owner;
            mailbox = new List<T>();
            mutex = new object();
        }

        public void Send(T msg)
        {
            lock (mutex)
            {
                mailbox.Add(msg);
                Monitor.PulseAll(mutex);
            }
        }

        public T Receive()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException("Tried to receive from a non-Task context");
            }
            if (Task.CurrentId.Value != owner)
            {
                throw new InvalidOperationException("Only the owner can receive from a Mailbox");
            }
            T res;
            lock (mutex)
            {
                while (mailbox.Count <= 0)
                {
                    Monitor.Wait(mutex);
                }
                res = mailbox[0];
                mailbox.RemoveAt(0);
            }
            return res;
        }
    }
}