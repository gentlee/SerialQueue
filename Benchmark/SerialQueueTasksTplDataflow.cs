using System;
using System.Threading.Tasks.Dataflow;

namespace Threading
{
    // THIS CLASS DOESN'T PASS TESTS AND IS NOT FIFO

    public class SerialQueueTasksTplDataflow
    {
        ActionBlock<object> _actionBlock = new ActionBlock<object>(async action =>
        {
            if (action.GetType() == typeof(Action))
            {
                (action as Action)!();
            }
            else
            {
                await (action as Func<Task>)!();
            }
        }, new ExecutionDataflowBlockOptions()
        {
            MaxDegreeOfParallelism = 1
        });

        public Task Enqueue(Action action)
        {
            return _actionBlock.SendAsync(action);
        }

        public Task Enqueue(Func<Task> asyncAction)
        {
            return _actionBlock.SendAsync(asyncAction);
        }

        public async Task<T> Enqueue<T>(Func<T> function)
        {
            T? result = default(T);
            await _actionBlock.SendAsync(() =>
            {
                result = function();
            });
            return result!;
        }

        public async Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
        {
            T? result = default(T);
            await _actionBlock.SendAsync(async () =>
            {
                result = await asyncFunction();
            });
            return result!;
        }
    }
}

