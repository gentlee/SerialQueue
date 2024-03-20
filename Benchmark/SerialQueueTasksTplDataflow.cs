using System;
using System.Threading.Tasks.Dataflow;

namespace Threading.Tasks
{
    public class SerialQueueTplDataflow
    {
        ActionBlock<object> _actionBlock = new ActionBlock<object>(
            async action =>
            {
                if (action is Action)
                {
                    (action as Action)!();
                }
                else
                {
                    await (action as Func<Task>)!();
                }
            }
        );

        public Task Enqueue(Action action)
        {
            var tsc = new TaskCompletionSource();
            _actionBlock.SendAsync(() =>
            {
                try
                {
                    action();
                    tsc.SetResult();
                }
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });
            return tsc.Task;
        }

        public Task Enqueue(Func<Task> asyncAction)
        {
            var tsc = new TaskCompletionSource();
            _actionBlock.SendAsync(async () =>
            {
                try
                {
                    await asyncAction();
                    tsc.SetResult();
                }
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });
            return tsc.Task;
        }

        public Task<T> Enqueue<T>(Func<T> function)
        {
            var tsc = new TaskCompletionSource<T>();
            _actionBlock.SendAsync(() =>
            {
                try
                {
                    var result = function();
                    tsc.SetResult(result);
                }
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });
            return tsc.Task;
        }

        public Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
        {
            var tsc = new TaskCompletionSource<T>();
            _actionBlock.SendAsync(async () =>
            {
                try
                {
                    var result = await asyncFunction();
                    tsc.SetResult(result);
                }
                catch (Exception e)
                {
                    tsc.SetException(e);
                }
            });
            return tsc.Task;
        }
    }
}

