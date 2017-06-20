
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActorTestingFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;
        public readonly Task task;
        public string name;
        public readonly Mailbox<object> Mailbox;

        public readonly ISet<ActorInfo> terminateWaiters;

        public bool active = true;
        public bool enabled = true;
        public bool terminated;
        public OpType currentOp = OpType.INVALID;
        public int currentOpTarget = -1;
        public TargetType currentOpTargetType = TargetType.Thread;

        /// <summary>
        /// If the current op is a receive, then currentOpSendStepIndex
        /// gives the index of the earlier corresponding send operation.
        /// </summary>
        public int currentOpSendStepIndex = -1;

        public CancellationTokenSource cts;
        public List<Exception> exceptions;
        public bool cancelled;

        public object mutex = new object();

        public ActorInfo(
            ActorId id,
            string name,
            Task task,
            CancellationTokenSource cts,
            TestingActorRuntime runtime)
        {
            this.id = id;
            this.task = task;
            this.name = name;
            this.cts = cts;
            Mailbox = new Mailbox<object>(this, runtime);
            terminateWaiters = new HashSet<ActorInfo>();
            exceptions = new List<Exception>();
        }

        #region Overrides of Object

        public override string ToString()
        {
            return $"{name ?? "Unnamed"} ({id.id})";
        }

        #endregion
    }
}