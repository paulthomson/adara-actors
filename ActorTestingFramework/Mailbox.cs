using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActorInterface;
using NLog;

namespace ActorTestingFramework
{
    public class Mailbox<T> : IMailbox<T>
    {
        private struct Msg
        {
            public T msg;
            public int sendIndex;

            public Msg(T msg_, int sendIndex_)
            {
                msg = msg_;
                sendIndex = sendIndex_;
            }
        }
        private static Logger LOGGER = LogManager.GetCurrentClassLogger();

        public readonly ActorInfo ownerActorInfo;
        private readonly TestingActorRuntime runtime;
        private readonly IList<Msg> mailbox;

        private ActorInfo waiter = null;

        public Mailbox(ActorInfo ownerActorInfo, TestingActorRuntime runtime)
        {
            this.ownerActorInfo = ownerActorInfo;
            this.runtime = runtime;
            mailbox = new List<Msg>();
        }

        public void Send(T msg)
        {
            runtime.Schedule(OpType.SEND, TargetType.Queue, ownerActorInfo.id.id);
            LogSend(msg);
            Msg wrappedMsg = new Msg(msg, runtime.GetCurrentSchedulerStep());
            mailbox.Add(wrappedMsg);

            if (waiter != null)
            {
                Safety.Assert(!waiter.enabled);
                waiter.enabled = true;
                waiter.currentOpSendStepIndex = wrappedMsg.sendIndex;
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
            else
            {
                ownerActorInfo.currentOpSendStepIndex = mailbox[0].sendIndex;
            }

            runtime.Schedule(OpType.RECEIVE, TargetType.Queue, ownerActorInfo.id.id);

            Safety.Assert(mailbox.Count > 0);
            Safety.Assert(waiter == null);

            var res = mailbox[0];
            mailbox.RemoveAt(0);
            return res.msg;
        }
    }
}