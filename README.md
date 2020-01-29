# SerialQueue
Lightweight C# implementation of FIFO serial queues from ObjC, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

### Interface

    class SerialQueue {
        Task Enqueue(Action action)
        Task<T> Enqueue<T>(Func<T> function)
        Task Enqueue(Func<Task> asyncAction)
        Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
    }
    
### Example

    readonly SerialQueue queue = new SerialQueue();
    
    async Task SomeAsyncMethod()
    {
        // C# 5+
        await queue.Enqueue(SyncAction);
        
        var result = await queue.Enqueue(AsyncFunction);
    
        // Old approach
        queue.Enqueue(AsyncFunction).ContinueWith(t => {
            var result = t.Result;
        });
    }
