using ActorInterface;

namespace ActorTestingFramework
{
    public interface ITestingActorRuntime : IActorRuntime
    {
        void PrepareForNextSchedule();
        void Wait();
    }
}