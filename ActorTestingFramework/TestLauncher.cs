using System;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestLauncher : ITestLauncher
    {
        private IScheduler scheduler;
        private TestingActorRuntime runtime;


        #region Implementation of ITestLauncher

        public void Execute(Action<IActorRuntime, ITestingRuntime> action)
        {
            runtime = new TestingActorRuntime(scheduler);
            scheduler.NextSchedule();

            var task = runtime.StartMain(() =>
            {
                TestingActorRuntime.ActorBody<object>(
                    () =>
                    {
                        action(runtime, runtime);
                        return null;
                    },
                    runtime,
                    true);
            });

            task.Wait();
            runtime.WaitForAllActorsToTerminate();

        }

        public void SetScheduler(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        #endregion
    }
}