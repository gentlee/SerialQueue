﻿namespace Threading
{
    public class SerialQueueMonitor
    {
        readonly object _locker = new object();
        readonly WeakReference<Task?> _lastTask = new(null);

        public Task Enqueue(Action action)
        {
            return Enqueue(() =>
            {
                action();
                return true;
            });
        }

        public Task<T> Enqueue<T>(Func<T> function)
        {
            lock (_locker)
            {
                Task? lastTask;
                Task<T> resultTask;

                if (_lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(_ => function(), TaskContinuationOptions.ExecuteSynchronously);
                }
                else
                {
                    resultTask = Task.Run(function);
                }

                _lastTask.SetTarget(resultTask);

                return resultTask;
            }
        }

        public Task Enqueue(Func<Task> asyncAction)
        {
            lock (_locker)
            {
                Task? lastTask;
                Task resultTask;

                if (_lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(_ => asyncAction(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
                }
                else
                {
                    resultTask = Task.Run(asyncAction);
                }

                _lastTask.SetTarget(resultTask);

                return resultTask;
            }
        }

        public Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
        {
            lock (_locker)
            {
                Task? lastTask;
                Task<T> resultTask;

                if (_lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(_ => asyncFunction(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
                }
                else
                {
                    resultTask = Task.Run(asyncFunction);
                }

                _lastTask.SetTarget(resultTask);

                return resultTask;
            }
        }
    }
}
