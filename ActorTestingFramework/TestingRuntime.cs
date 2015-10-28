using System;
using System.Threading.Tasks;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestingRuntime : ITestingRuntime
    {
        private IScheduler scheduler;
        private TestingActorRuntime runtime;


        #region Implementation of ITestingRuntime

        public void Execute(Action<IActorRuntime> action)
        {
            runtime = new TestingActorRuntime(scheduler);
            scheduler.NextSchedule();

            var task = Task.Factory.StartNew(() =>
            {
                TestingActorRuntime.ActorBody(
                    new ActionActor(action),
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