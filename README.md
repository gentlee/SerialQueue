# SerialQueue
Lightweight C# implementation of FIFO serial queues from ObjC, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

    readonly SerialQueue queue = new SerialQueue();
    
    async Task SomeAsyncMethod()
    {
        // C# 5+
        var result = await queue.Enqueue(WorkForSerialQueue);
    
        // Old approach
        queue.Enqueue(WorkForSerialQueue).ContinueWith(t => {
            var result = t.Result;
        });
    }