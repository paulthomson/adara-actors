using System;
using System.Threading;
using ActorInterface;

namespace ActorFramework
{
    public class ActorInfo
    {
        public readonly ActorId id;

        public Mailbox<object> Mailbox { get; private set; }

        public ActorInfo(ActorId id, int? owner)
        {
            this.id = id;
            if (owner.HasValue)
            {
                Mailbox = new Mailbox<object>(owner.Value);
            }
        }
    }
}