using System;
using ActorInterface;

namespace ActorTestingFramework
{
    public interface ITestLauncher
    {
        void Execute(Action<IActorRuntime, ITestingRuntime> action);
        void SetScheduler(IScheduler scheduler);
    }
}