using System;
using System.Threading.Tasks;

namespace Threading
{
    public class SerialQueue
    {
        readonly object _locker = new object();
        WeakReference<Task> _lastTask;

        public Task EnqueueAction(Action action)
        {
            return EnqueueFunction<object>(() => {
                action();
                return null;
            });
        }

        public Task<T> EnqueueFunction<T>(Func<T> function)
        {
            if (typeof(T).Equals(typeof(Task)))
                throw new InvalidOperationException("You provided async function - use EnqueueAsyncFunction for this.");

            lock (_locker)
            {
                Task lastTask = null;
                Task<T> resultTask = null;

                if (_lastTask != null && _lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(_ => function());
                }
                else
                {
                    resultTask = Task.Run(function);
                }

                _lastTask = new WeakReference<Task>(resultTask);
                return resultTask;
            }
        }

        public Task EnqueueAsyncFunction(Func<Task> function)
        {
            lock (_locker)
            {
                Task lastTask = null;
                Task resultTask = null;

                if (_lastTask != null && _lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(async _ => await function.Invoke()).Unwrap();
                }
                else
                {
                    resultTask = Task.Run(async () => await function.Invoke());
                }

                _lastTask = new WeakReference<Task>(resultTask);
                return resultTask;
            }
        }

        public Task<T> EnqueueAsyncFunction<T>(Func<Task<T>> function)
        {
            lock (_locker)
            {
                Task lastTask = null;
                Task<T> resultTask = null;

                if (_lastTask != null && _lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(async _ => await function.Invoke()).Unwrap();
                }
                else
                {
                    resultTask = Task.Run(async () => await function.Invoke());
                }

                _lastTask = new WeakReference<Task>(resultTask);
                return resultTask;
            }
        }
    }
}
