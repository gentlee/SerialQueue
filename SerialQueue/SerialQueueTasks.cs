namespace Threading.Tasks
{
    public class SerialQueue
    {
        SpinLock _spinLock = new(false);
        readonly WeakReference<Task?> _lastTask = new(null);

        public Task Enqueue(Action action)
        {
            bool gotLock = false;
            try
            {
                Task? lastTask;
                Task resultTask;

                _spinLock.Enter(ref gotLock);

                if (_lastTask.TryGetTarget(out lastTask))
                {
                    resultTask = lastTask.ContinueWith(_ => action(), TaskContinuationOptions.ExecuteSynchronously);
                }
                else
                {
                    resultTask = Task.Run(action);
                }

                _lastTask.SetTarget(resultTask);

                return resultTask;
            }
            finally
            {
                if (gotLock) _spinLock.Exit(false);
            }
        }

        public Task<T> Enqueue<T>(Func<T> function)
        {
            bool gotLock = false;
            try
            {
                Task? lastTask;
                Task<T> resultTask;

                _spinLock.Enter(ref gotLock);

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
            finally
            {
                if (gotLock) _spinLock.Exit(false);
            }
        }

        public Task Enqueue(Func<Task> asyncAction)
        {
            bool gotLock = false;
            try
            {
                Task? lastTask;
                Task resultTask;

                _spinLock.Enter(ref gotLock);

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
            finally
            {
                if (gotLock) _spinLock.Exit(false);
            }
        }

        public Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
        {
            bool gotLock = false;
            try
            {
                Task? lastTask;
                Task<T> resultTask;

                _spinLock.Enter(ref gotLock);

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
            finally
            {
                if (gotLock) _spinLock.Exit(false);
            }
        }
    }
}