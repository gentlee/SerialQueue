# SerialQueue
Lightweight C# implementation of FIFO serial queues from ObjC, which is often much better to use for synchronization rather than locks - it doesn't block caller's thread, and than creating new thread - it uses thread pool.

    private readonly SerialQueue queue = new SerialQueue();
    
    async Task SomeAsyncMethod()
    {
      // C# 5
      var result = await queue.RunAsync(LongRunningWork);
    
      // Old approach
      queue.RunAsync(LongRunningWork).ContinueWith(t => {
          var result = t.Result;
      })
    }
