using System;
using System.Threading;
using System.Threading.Tasks;
using ActorInterface;
using TypedActorInterface;

namespace ActorHelpers
{
    public static class TaskHelper
    {
        public static IActorRuntime runtime;
        public static ITypedActorRuntime typedRuntime;

        public static TaskScheduler taskScheduler;

        public static void Yield()
        {
            runtime.Yield();
        }

        public static void Sleep(int milliseconds)
        {
//            runtime.Sleep(milliseconds);
            runtime.Yield();
        }

        public static Task Delay(int milliseconds)
        {
//            runtime.Sleep(milliseconds);
            runtime.Yield();
            return runtime.StartNew(() => (object)null);
        }

        public static Task Delay(TimeSpan timeSpan)
        {
            return Delay((int) timeSpan.TotalMilliseconds);
        }

        public static Task<T> StartNewActor<T>(
            this TaskFactory tf,
            Func<T> func)
        {
            return runtime.StartNew(func);
        }

        public static Task StartNewActor(
            this TaskFactory tf,
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
            Func<T> func,
            CancellationToken ct)
        {
            return runtime.StartNew(func);
        }

        public static void WaitActor(this Task task)
        {
            runtime.WaitForActor(task);
        }

        public static void ContinueWithActor<TResult, TNewResult>(
            this Task<TResult> task,
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
            Action<Task<TResult>> continuationFunction)
        {
            return runtime.StartNew(() =>
            {
                runtime.WaitForActor(task);
                continuationFunction(task);
                return (object) null;
            });
        }

        public static Task ContinueWithActor(
            this Task task,
            Action<Task> continuationFunction)
        {
            return runtime.StartNew(() =>
            {
                runtime.WaitForActor(task);
                continuationFunction(task);
                return (object) null;
            });
        }

        public static Task ContinueWithActor<TResult>(
            this Task<TResult> task,
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

        public static T ResultActor<T>(this Task<T> task)
        {
            runtime.WaitForActor(task);
            return task.Result/*OK*/;
        }

        public static Task<T> FromResult<T>(T result)
        {
            return runtime.StartNew(() => result);
        }

        public static Task UnwrapActor(this Task<Task> task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            return runtime.StartNew<object>(() =>
            {
                runtime.WaitForActor(task, false);
                switch (task.Status)
                {
                    case TaskStatus.Canceled:
                        runtime.CancelSelf();
                        break;
                    case TaskStatus.Faulted:
                        throw new AggregateException();
                    case TaskStatus.RanToCompletion:
                        var res = task.Result/*OK*/;
                        if (res == null)
                        {
                            runtime.CancelSelf();
                        }
                        else
                        {
                            runtime.WaitForActor(res, false);
                            
                            switch (res.Status)
                            {
                                case TaskStatus.Canceled:
                                    runtime.CancelSelf();
                                    break;
                                case TaskStatus.Faulted:
                                    throw new AggregateException();
                                case TaskStatus.RanToCompletion:
                                    return null;
                            }
                        }
                        break;
                    default:
                        runtime.InternalError();
                        break;
                }
                throw new InvalidOperationException();
            });
        }

        public static Task<T> UnwrapActor<T>(this Task<Task<T>> task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            
            return runtime.StartNew<T>(() =>
            {
                runtime.WaitForActor(task, false);

                switch (task.Status)
                {
                    case TaskStatus.Canceled:
                        runtime.CancelSelf();
                        break;
                    case TaskStatus.Faulted:
                        throw new AggregateException();
                    case TaskStatus.RanToCompletion:
                        var res = task.ResultActor();
                        if (res == null)
                        {
                            runtime.CancelSelf();
                        }
                        else
                        {
                            runtime.WaitForActor(res, false);
                            
                            switch (res.Status)
                            {
                                case TaskStatus.Canceled:
                                    runtime.CancelSelf();
                                    break;
                                case TaskStatus.Faulted:
                                    throw new AggregateException();
                                case TaskStatus.RanToCompletion:
                                    var res2 = res.ResultActor();
                                    if (res2 == null)
                                    {
                                        runtime.CancelSelf();
                                    }
                                    else
                                    {
                                        return res2;
                                    }
                                    break;
                                default:
                                    runtime.InternalError();
                                    break;
                            }
                        }
                        break;
                    default:
                        runtime.InternalError();
                        break;
                }
                throw new InvalidOperationException();
            });
            
        }


    }
}