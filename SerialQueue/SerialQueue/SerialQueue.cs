using System;
using System.Threading.Tasks;

namespace Threading
{
    public class SerialQueue
    {
        readonly object _locker = new object();
        Task _lastTask;

        public Task RunAsync(Action action)
        {
            lock (_locker) {
                _lastTask = _lastTask != null ? _lastTask.ContinueWith(_ => action()) : Task.Run(action);
                return _lastTask;
            }
        }

        public Task<T> RunAsync<T>(Func<T> function)
        {
            lock (_locker) {
                var task = _lastTask != null ? _lastTask.ContinueWith(_ => function()) : Task.Run(function);
                _lastTask = task;
                return task;
            }
        }
    }
}

