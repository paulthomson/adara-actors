using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public interface IMonitor : ITypedActor
    {
        void LockMutex(IMailbox<object> me);
        void UnlockMutex(IMailbox<object> me);
        void WaitMutex(IMailbox<object> me);
        void PulseAllMutex(IMailbox<object> me);
    }
}