using System;
using System.Threading;
using System.Threading.Tasks;
using ActorHelpers;
using ActorInterface;

namespace ActorTestingFramework
{
    public class TestLauncher : ITestLauncher
    {
        private TestingActorRuntime runtime;
        private readonly TaskScheduler taskScheduler;

        public TestLauncher(TaskScheduler taskScheduler)
        {
            this.taskScheduler = taskScheduler;
        }

        public TestLauncher() 
            : this(TaskScheduler.Current)
        {

        }

        #region Implementation of ITestLauncher

        public void Execute(Action<IActorRuntime, ITestingRuntime> action, IScheduler scheduler)
        {
            runtime = new TestingActorRuntime(scheduler);
            scheduler.NextSchedule();

            // TODO: Remove this somehow.
            TaskHelper.runtime = runtime;

            var task = new Task(() =>
            {
                TestingActorRuntime.ActorBody<object>(
                    () =>
                    {
                        action(runtime, runtime);
                        return null;
                    },
                    runtime,
                    true);
            },
                CancellationToken.None,
                TaskCreationOptions.RunContinuationsAsynchronously);

            runtime.RegisterMainTask(task);

            task.Start(taskScheduler);

            task.Wait();
            runtime.WaitForAllActorsToTerminate();

        }

        #endregion
    }
}