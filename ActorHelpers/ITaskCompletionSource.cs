using System;
using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public interface ITaskCompletionSource : ITypedActor
    {
        object SetResult(object res);
        bool TrySetResult(object res);
        object SetCanceled();
        object SetException(Exception exception);
    }
}