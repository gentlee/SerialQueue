<details>
  <summary>Donations 🙌</summary>
  <b>BTC:</b> bc1qs0sq7agz5j30qnqz9m60xj4tt8th6aazgw7kxr <br>
  <b>ETH:</b> 0x1D834755b5e889703930AC9b784CB625B3cd833E <br>
  <b>USDT(Tron):</b> TPrCq8LxGykQ4as3o1oB8V7x1w2YPU2o5n <br>
  <b>TON:</b> EQAtBuFWI3H_LpHfEToil4iYemtfmyzlaJpahM3tFSoxojvV <br>
  <b>DOGE:</b> D7GMQdKhKC9ymbT9PtcetSFTQjyPRRfkwT <br>
</details>

# SerialQueue

Lightweight, high-performance C# implementations of FIFO serial queues from Apple's GCD, which are often much better to use for synchronization rather than locks - they don't block caller's thread, and rather than creating new threads - they use thread pool.

Task-based implementation (recommended) is more simple and convenient, while non-task is faster (check benchmark results).

Covered with tests.

👉 Read [my article](https://alexanderdanilov.dev/en/articles/serial-queues) about serial queues.

### Table of contents

 - [Interface](https://github.com/gentlee/SerialQueue#interface)
 - [Installation](https://github.com/gentlee/SerialQueue#installation)
 - [Examples](https://github.com/gentlee/SerialQueue#examples)
 - [Benchmark results](https://github.com/gentlee/SerialQueue#benchmark-results)
 - [Troubleshooting](https://github.com/gentlee/SerialQueue#troubleshooting)
   - [Deadlocks](https://github.com/gentlee/SerialQueue#deadlocks)

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
    
### Examples

Task-based queue usage:
```C#
readonly SerialQueue queue = new SerialQueue();

async Task SomeAsyncMethod()
{
    await queue.Enqueue(() => {
        // synchronized code
    });
}
```

Non-task based example:
```C#
readonly SerialQueue queue = new SerialQueue();

void SomeAsyncMethod()
{
    queue.DispatchAsync(() => {
        // synchronized code
    });
}
```

Previous examples do the same as the next one with Monitor (lock):

```C#
readonly object locker = new object();

async Task SomeAsyncMethod()
{
    lock(locker) {
         // synchronized code
    }
}
```

But serial queues are **asynchronous**, **don't block callers threads** while waiting for synced operation to start, evaluate synced operations on **thread pool** and often **perform better**, especially for long synced operations.

### Benchmark results

<details>
<summary>Chart 1: Approximate synchronization costs depending on the operation duration (smaller is better).</summary>

![chart-1](https://github.com/gentlee/SerialQueue/assets/2361140/bab377e6-15a2-4ed2-9db1-621243f30e5b)

</details>

<details>
<summary>Chart 2: Zoomed in (smaller is better).</summary>

![chart-2](https://github.com/gentlee/SerialQueue/assets/2361140/a9b52ae0-a455-4e78-a721-81b3146c0db4)

</details>

<details>
<summary>Chart 3: Zoomed in for the shortest operations (smaller is better).</summary>

![chart-3](https://github.com/gentlee/SerialQueue/assets/2361140/70e442a5-314a-42cc-9ab8-354b6514f6ae)

</details>

- The X axis is the time of the operation to be synchronized, in milliseconds.
- The Y axis shows approximate synchronization costs in processor ticks.

Synchronization mechanisms:
- **SpinLock**, **Monitor**, **Mutex** - standard synchronization primitives.
- **SemaphoreSlim** is a simplified alternative to Semaphore.
- **TPL Dataflow ActionBlock** - implementation of a queue using TPL Dataflow ActionBlock.
- **SerialQueue (by @borland)** - queue [implementation](https://github.com/borland/SerialQueue) from user @borland.
- **SerialQueue** is a lightweight serial queue implementation from **this repository**.
- **SerialQueue** Tasks is a Task-based serial queue implementation from **this repository**.

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
