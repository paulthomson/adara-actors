using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActorInterface;
using NLog;

namespace ActorTestingFramework
{
    public class Mailbox<T> : IMailbox<T>
    {
        private static Logger LOGGER = LogManager.GetCurrentClassLogger();

        public readonly ActorInfo ownerActorInfo;
        private readonly TestingActorRuntime runtime;
        private readonly IList<T> mailbox;

        private ActorInfo waiter = null;

        public Mailbox(ActorInfo ownerActorInfo, TestingActorRuntime runtime)
        {
            this.ownerActorInfo = ownerActorInfo;
            this.runtime = runtime;
            mailbox = new List<T>();
        }

        public void Send(T msg)
        {
            runtime.Schedule(OpType.SEND);
            LogSend(msg);
            mailbox.Add(msg);

            if (waiter != null)
            {
                Safety.Assert(!waiter.enabled);
                waiter.enabled = true;
                waiter = null;
            }
        }

        private void LogSend(T msg)
        {
            var currentActor = runtime.GetCurrentActorInfo();
            LOGGER.Trace($"{currentActor} -- {msg} --> {ownerActorInfo}");
        }

        public T Receive()
        {
            if (Task.CurrentId == null)
            {
                throw new InvalidOperationException("Tried to receive from a non-Task context");
            }
            if (Task.CurrentId.Value != ownerActorInfo.task.Id)
            {
                throw new InvalidOperationException("Only the owner can receive from a Mailbox");
            }

            if (mailbox.Count <= 0)
            {
                Safety.Assert(waiter == null);
                waiter = runtime.GetCurrentActorInfo();
                Safety.Assert(waiter.enabled);
                waiter.enabled = false;
            }

            runtime.Schedule(OpType.RECEIVE);

            Safety.Assert(mailbox.Count > 0);
            Safety.Assert(waiter == null);

            var res = mailbox[0];
            mailbox.RemoveAt(0);
            return res;
        }
    }
}