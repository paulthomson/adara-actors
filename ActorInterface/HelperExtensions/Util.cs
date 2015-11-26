using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActorInterface.HelperExtensions
{
    public static class Util
    {
        public static Task<T> StartNewActor<T>(
            this TaskFactory tf,
            IActorRuntime runtime,
            Func<T> func)
        {
            return runtime.StartNew(func);
        }

        public static Task StartNewActor(
            this TaskFactory tf,
            IActorRuntime runtime,
            Action action)
        {
            return runtime.StartNew<object>(() =>
            {
                action();
                return null;
            });
        }

        public static Task<T> StartNewActor<T>(
            this TaskFactory tf,
            IActorRuntime runtime,
            Func<T> func,
            CancellationToken ct)
        {
            return runtime.StartNew(func);
        }

        public static void WaitActor(this Task task, IActorRuntime runtime)
        {
            runtime.WaitForActor(task);
        }

        public static void ContinueWithActor<TResult, TNewResult>(
            this Task<TResult> task,
            IActorRuntime runtime,
            Task<TNewResult> nextText)
        {
            runtime.StartNew<object>(() =>
            {
                runtime.WaitForActor(task);
                return null;
            });
        }

        public static Task<TResult> ContinueWithActor<TResult>(
            this Task task,
            IActorRuntime runtime,
            Func<Task, TResult> continuationFunction)
        {
            return runtime.StartNew(() =>
            {
                runtime.WaitForActor(task);
                return continuationFunction(task);
            });
        }

        public static Task<TNewResult> ContinueWithActor<TResult, TNewResult>(
            this Task<TResult> task,
            IActorRuntime runtime,
            Func<Task<TResult>, TNewResult> continuationFunction)
        {
            return runtime.StartNew(() =>
            {
                runtime.WaitForActor(task);
                return continuationFunction(task);
            });
        }

        public static Task ContinueWithActor<TResult>(
            this Task<TResult> task,
            IActorRuntime runtime,
            Action<Task<TResult>, object> continuationAction,
            object state,
            CancellationToken cancellationToken)
        {
            return runtime.StartNew<object>(() =>
            {
                runtime.WaitForActor(task);
                continuationAction(task, state);
                return null;
            });
        }

        public static T ResultActor<T>(this Task<T> task, IActorRuntime runtime)
        {
            runtime.WaitForActor(task);
            return task.Result;
        }
    }
}