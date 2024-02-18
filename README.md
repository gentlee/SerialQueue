# SerialQueue
Lightweight C# implementations of FIFO serial queues from Apple's GCD, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

Task-based implementation is more simple and convenient, while non-task is slightly faster (check benchmark results).

Covered with tests.

### Interface

```C#
// Task based version (recommended)
// SerialQueue/SerialQueueTasks.cs
class SerialQueue {
    Task Enqueue(Action action);
    Task<T> Enqueue<T>(Func<T> function);
    Task Enqueue(Func<Task> asyncAction);
    Task<T> Enqueue<T>(Func<Task<T>> asyncFunction);
}

// SerialQueue/SerialQueue.cs
class SerialQueue {
  void DispatchSync(Action action);
  void DispatchAsync(Action action);
}
```

### Installation

Just copy the source code of `SerialQueue/SerialQueueTasks.cs` or `SerialQueue/SerialQueue.cs` file to your project.
    
### Task-based example

```C#
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
```

### Troubleshooting

#### Deadlocks

Nesting and awaiting `queue.Enqueue` leads to deadlock in the queue:

```C#
var queue = new SerialQueue();

await queue.Enqueue(async () =>
{
  await queue.Enqueue(async () =>
  {
    // This code will never run because it waits until the first task executes,
    // and first task awaits while this one finishes.
    // Queue is locked.
  });
});
```
This particular case can be fixed by either not awaiting nested Enqueue or not putting nested task to queue at all, because it is already in the queue.

Overall it is better to implement code not synced first, but later sync it in the upper layer that uses that code, or in a synced wrapper:

```C#
// Bad

async Task Run()
{
  await FunctionA();
  await FunctionB();
  await FunctionC(); // deadlock
}

async Task FunctionA() => await queue.Enqueue(async () => { ... });

async Task FunctionB() => await queue.Enqueue(async () => { ... });

async Task FunctionC() => await queue.Enqueue(async () =>
  await FunctionA();
  ...
  await FunctionB();
});

// Good

async Task Run()
{
    await queue.Enqueue(FunctionA);
    await queue.Enqueue(FunctionB);
    await queue.Enqueue(FunctionC);
}

async Task FunctionA() { ... };

async Task FunctionB() { ... };

async Task FunctionC()
{
  await FunctionA();
  ...
  await FunctionB();
};
```
