# SerialQueue
Lightweight C# implementations of FIFO serial queues from Apple's GCD, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

Task-based implementation is more simple and convenient, while non-task is faster (check benchmark results).

Covered with tests.

### Table of contents

 - [Benchmark results](https://github.com/gentlee/SerialQueue#benchmarkresults)
 - [Interface](https://github.com/gentlee/SerialQueue#interface)
 - [Installation](https://github.com/gentlee/SerialQueue#installation)
 - [Task-based example](https://github.com/gentlee/SerialQueue#task-basedexample)
 - [Troubleshooting](https://github.com/gentlee/SerialQueue#troubleshooting)
   - [Deadlocks](https://github.com/gentlee/SerialQueue#deadlocks)

### Benchmark results

<details>
<summary>Chart 1: Approximate synchronization costs depending on the operation duration (smaller is better).</summary>

![chart-1](https://github.com/gentlee/SerialQueue/assets/2361140/e2ea4a5a-fe3c-4e6c-9af2-f01e8ef7e2a2)

</details>

<details>
<summary>Chart 2: Zoomed in (smaller is better).</summary>

![chart-2](https://github.com/gentlee/SerialQueue/assets/2361140/d1cc4ccb-2eb4-429b-901a-b9b8d7746fd0)

</details>

<details>
<summary>Chart 3: Zoomed in for the shortest operations (smaller is better).</summary>

![chart-3](https://github.com/gentlee/SerialQueue/assets/2361140/45539b3f-7356-4766-b9c7-85e5d482fdab)

</details>

- The X axis is the time of the operation to be synchronized, in milliseconds.
- The Y axis shows approximate synchronization costs in processor ticks.

Synchronization mechanisms:
- **SpinLock**, **Monitor**, **Mutex** - standard synchronization primitives.
- **SemaphoreSlim** is a simplified alternative to Semaphore.
- **TPL Dataflow ActionBlock** - implementation of a queue using TPL Dataflow ActionBlock.
- **SerialQueue Borland** - queue implementation from Borland.
- **SerialQueue** is a lightweight serial queue implementation from **this repository**.
- **SerialQueue** Tasks is a Task-based serial queue implementation from **this repository**.


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

// Lightweight version
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
