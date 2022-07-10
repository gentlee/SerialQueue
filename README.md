# SerialQueue
Lightweight C# Task-based implementation of FIFO serial queues from ObjC, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

### Interface

```C#
class SerialQueue {
    Task Enqueue(Action action)
    Task<T> Enqueue<T>(Func<T> function)
    Task Enqueue(Func<Task> asyncAction)
    Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
}
```
    
### Example

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

But it is better to implement code not synced first, but later sync it in the upper layer that uses that code:

```C#
// Bad

async Task Test()
{
  await FunctionA();
  await FunctionB();
  await FunctionC(); // deadlock
}

async Task FunctionA() => await queue.Enqueue(async () =>
  // job A
});
async Task FunctionB() => await queue.Enqueue(async () =>
  // job B
});
async Task FunctionC() => await queue.Enqueue(async () =>
  await FunctionA();
  // job C
  await FunctionB();
});

// Good

async Task Test()
{
    await queue.Enqueue(FunctionA);
    await queue.Enqueue(FunctionB);
    await queue.Enqueue(FunctionC);
}

async Task FunctionA()
{
  // job A
};

async Task FunctionB()
{
  // job B
};

async Task FunctionC()
{
  await FunctionA();
  // job C
  await FunctionB();
};
```
