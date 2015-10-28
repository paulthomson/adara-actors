using System;
using ActorInterface;

namespace ActorTestingFramework
{
    public interface ITestingRuntime
    {
        void Execute(Action<IActorRuntime> action);
        void SetScheduler(IScheduler scheduler);
    }
}