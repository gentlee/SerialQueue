using System;
using System.Threading.Tasks;

namespace Threading
{
    public class SerialQueue
    {
        readonly object _locker = new object();
        Task _lastTask;

        public Task Run(Action action)
        {
            lock (_locker) {
                var task = _lastTask != null ? _lastTask.ContinueWith(_ => action()) : Task.Run(action);
                _lastTask = RemoveReferenceOnCompleted(task);
                return task;
            }
        }

        public Task<T> Run<T>(Func<T> function)
        {
            lock (_locker) {
                var task = _lastTask != null ? _lastTask.ContinueWith(_ => function()) : Task.Run(function);
                _lastTask = RemoveReferenceOnCompleted(task);
                return task;
            }
        }

        Task RemoveReferenceOnCompleted(Task task)
        {
            Task continuationTask = null;
            continuationTask = task.ContinueWith(t => {
                if (continuationTask == _lastTask) {
                    lock (_locker) {
                        if (continuationTask == _lastTask) {
                            _lastTask = null;
                        }
                    }
                }
            });
            return continuationTask;
        }
    }
}

