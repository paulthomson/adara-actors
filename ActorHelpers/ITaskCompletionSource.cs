using System;
using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public interface ITaskCompletionSource : ITypedActor
    {
        void SetResult(object res, IMailbox<object> rt);
        bool TrySetResult(object res);
        void SetCanceled(IMailbox<object> rt);
        object SetException(Exception exception);
    }
}