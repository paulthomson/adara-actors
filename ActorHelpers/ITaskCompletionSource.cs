using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public interface ITaskCompletionSource : ITypedActor
    {
        void SetResult(object res, IMailbox<object> rt);
        void SetCanceled(IMailbox<object> rt);
    }
}