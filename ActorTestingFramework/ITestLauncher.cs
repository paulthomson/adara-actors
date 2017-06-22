using System;
using ActorInterface;

namespace ActorTestingFramework
{
    public interface ITestLauncher
    {
        void Execute(Action<IActorRuntime, ITestingRuntime> action, IScheduler scheduler);
        Exception GetError();
        bool WasSleepSetBlocked { get; }
    }
}