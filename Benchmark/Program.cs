using System.Diagnostics;
using System.Globalization;
using Threading;

const string STRETCH_MEMORY_LABEL = "stretch memory";
const string NO_SYNC_LABEL = "work duration without sync, ticks";
const char DELIMITER = ';';
const int DEFAULT_ITERATIONS = 100_000;

int[] workDurationsNs = { 100, 200, 300, 400, 500, 1000, 5000, 10_000, 50_000, 100_000, 500_000, 1000_000, 5_000_000, 10_000_000 };
var workIterationsPerDuration = new Dictionary<int, int> {
    { 10_000_000, 2000 },
    { 5_000_000, 4000 },
    { 1_000_000, 10_000 },
    { 500_000, 20_000 }
};
var stopwatch = new Stopwatch();
var workStopwatch = new Stopwatch();
var enLocale = new CultureInfo("en-En");
var benchmarks = new Dictionary<string, Func<int, int, Task>> {
    { STRETCH_MEMORY_LABEL, serialQueueTasksMonitor },
    { NO_SYNC_LABEL, noSync },

    { "spinlock", spinLock },
    { "monitor", monitor },
    { "mutex", mutex },
    { "serial-queue-tasks-spinlock", serialQueueTasksSpinLock },
    { "serial-queue-tasks-monitor", serialQueueTasksMonitor },
    { "serial-queue-tasks-semaphoreslim", serialQueueTasksSemaphoreSlim },
    { "serial-queue-spinlock", serialQueueSpinLock },
    { "serial-queue-monitor", serialQueueMonitor },
    { "serial-queue-borland", serialQueueBorland },
};

// log headers

Console.WriteLine("work duration, ms" + DELIMITER + string.Join(DELIMITER, workDurationsNs.Select(x => x / 1000.0)));

// run benchmarks

var noSyncResults = new List<float>(workDurationsNs.Length);

foreach (var (label, benchmark) in benchmarks)
{
    List<float>? results = null;
    var runBenchmark = (int iterations, int workDurationNs) => runAndGetDuration(() => benchmark(iterations, workDurationNs));

    // warmup

    await runBenchmark(10_000, 1000);
    GC.Collect();

    // run benchmark for all work duration values

    for (int i = 0; i < workDurationsNs.Length; i += 1)
    {
        int workDurationNs = workDurationsNs[i];
        int workIterations = workIterationsPerDuration.GetValueOrDefault(workDurationNs, DEFAULT_ITERATIONS);

        // run benchmark

        var elapsed = await runBenchmark(workIterations, workDurationNs);
        GC.Collect();

        // log result

        var ticksPerWork = elapsed.Ticks / (float)workIterations;
        if (label == NO_SYNC_LABEL)
        {
            results ??= noSyncResults;
            noSyncResults.Add(ticksPerWork); // log duration for no-sync benchmark
        }
        else if (label != STRETCH_MEMORY_LABEL)
        {
            results ??= new List<float>(workDurationsNs.Length);
            results.Add(ticksPerWork - noSyncResults[i]); // log diff with no-sync duration for other benchmarks
        }
    }

    // log results

    if (label != STRETCH_MEMORY_LABEL)
    {
        Console.WriteLine(label + DELIMITER + string.Join(DELIMITER, results!.Select(x => x.ToString(enLocale))));
    }
}

// utils

void work(int ns)
{
    workStopwatch!.Restart();
    while (workStopwatch.Elapsed.TotalNanoseconds < ns) { }
}

Task ParallelFor(int count, Action<Action> action, bool noParallelism = false)
{
    var options = new ParallelOptions();
    if (noParallelism)
    {
        options.MaxDegreeOfParallelism = 1;
    }
    var tasksLeft = count;
    var tcs = new TaskCompletionSource();
    var callback = () =>
    {
        tasksLeft -= 1;
        if (tasksLeft == 0)
        {
            tcs.SetResult();
        }
    };
    Parallel.For(0, count, options, (_, _) =>
    {
        action(callback);
    });
    return tcs.Task;
}

async Task<(long Ticks, long Ns)> runAndGetDuration(Func<Task> action)
{
    stopwatch.Restart();
    await action();
    stopwatch.Stop();
    return (stopwatch.ElapsedTicks, stopwatch.Elapsed.Nanoseconds);
}

// benchmarks

Task noSync(int workIterations, int workNs)
{
    return ParallelFor(workIterations, (callback) =>
    {
        work(workNs); ;
        callback();
    }, true);
}

Task monitor(int workIterations, int workNs)
{
    var locker = new object();
    return ParallelFor(workIterations, (callback) =>
    {
        lock (locker)
        {
            work(workNs); ;
            callback();
        }
    });
}

Task spinLock(int workIterations, int workNs)
{
    var spinLock = new SpinLock(false);
    return ParallelFor(workIterations, (callback) =>
    {
        bool gotLock = false;
        try
        {
            spinLock.Enter(ref gotLock);
            work(workNs);
            callback();
        }
        finally
        {
            if (gotLock) spinLock.Exit(false);
        }
    });
}

Task mutex(int workIterations, int workNs)
{
    var mutex = new Mutex();
    return ParallelFor(workIterations, (callback) =>
    {
        mutex.WaitOne();
        try
        {
            work(workNs);
            callback();
        }
        finally
        {
            mutex.ReleaseMutex(); ;
        }
    });
}

Task serialQueueTasksMonitor(int workIterations, int workNs)
{
    var serialQueue = new SerialQueueTasksMonitor();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.Enqueue(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueTasksSpinLock(int workIterations, int workNs)
{
    var serialQueue = new SerialQueueTasksSpinLock();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.Enqueue(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueTasksSemaphoreSlim(int workIterations, int workNs)
{
    var serialQueue = new SerialQueueTasksSemaphoreSlim();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.Enqueue(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueMonitor(int workIterations, int workNs)
{
    var serialQueue = new SerialQueueMonitor();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.DispatchAsync(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueSpinLock(int workIterations, int workNs)
{
    var serialQueue = new SerialQueueSpinlock();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.DispatchAsync(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueBorland(int workIterations, int workNs)
{
    var serialQueue = new Dispatch.SerialQueue();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.DispatchAsync(() =>
        {
            work(workNs);
            callback();
        });
    });
}

