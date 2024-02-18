using System.Diagnostics;
using System.Globalization;
using Threading;

const string NO_SYNC_LABEL = "no-sync(duration)";
const char DELIMITER = ';';
const int WARMUP_ITERATIONS = 200;
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
    { NO_SYNC_LABEL, noSync },
    { "spinlock", spinLock },
    { "monitor", monitor },
    { "mutex", mutex },
    { "serial-queue-tasks-spinlock", serialQueueTasksSpinLock },
    { "serial-queue-tasks-monitor", serialQueueTasksMonitor },
    { "serial-queue-tasks-semaphoreslim", serialQueueTasksSemaphoreSlim },
    { "serial-queue-borland", serialQueueBorland },
    { "serial-queue-spinlock", serialQueueSpinLock },
    { "serial-queue-monitor", serialQueueMonitor },
};

// log headers

Console.WriteLine("ms{0}{1}", DELIMITER, string.Join(DELIMITER, benchmarks.Keys));

// run benchmarks for each work duration value

foreach (var workDurationNs in workDurationsNs)
{
    var results = new List<float>(benchmarks.Count + 2) { workDurationNs / 1000.0f };
    int workIterations = workIterationsPerDuration.GetValueOrDefault(workDurationNs, DEFAULT_ITERATIONS);
    float noSyncWorkDurationTicks = 0; // later taken from no-sync benchmark

    // run benchmarks

    foreach (var (label, benchmark) in benchmarks)
    {
        var runBenchmark = (int iterations) => runAndGetDuration(() => benchmark(iterations, workDurationNs));

        // warmup

        await runBenchmark(WARMUP_ITERATIONS);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

        // run benchmark

        var elapsed = await runBenchmark(workIterations);

        // log result

        var ticksPerWork = elapsed.Ticks / (float)workIterations;
        if (label == NO_SYNC_LABEL)
        {
            noSyncWorkDurationTicks = ticksPerWork;
            results.Add(ticksPerWork); // log duration for no-sync benchmark
        }
        else
        {
            results.Add(ticksPerWork - noSyncWorkDurationTicks); // log diff with no-sync for other benchmarks
        }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
    }

    // log results

    Console.WriteLine(string.Join(DELIMITER, results.Select(x => x.ToString(enLocale))));
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
    stopwatch!.Restart();
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

