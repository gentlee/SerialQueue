using System.Diagnostics;
using Threading;
using Threading.Tasks;

const char DELIMITER = ';';

double[] workDurationsNs = GetDurationsNs();
int[] workIterations = workDurationsNs.Select(GetIterationsForDuration).ToArray();
var stopwatch = new Stopwatch();
var workStopwatch = new Stopwatch();
var benchmarks = new Dictionary<string, Func<int, double, Task>> {
    // used to calculate synchronization cost for other benchmarks
    { "No Sync", noSync },

    { "SpinLock", spinLock },
    { "Monitor", monitor },
    { "Mutex", mutex },
    { "SemaphoreSlim", serialQueueTasksSemaphoreSlim },
    { "Tpl Dataflow ActionBlock", tplDataflowActionBlock },
    { "SerialQueue (Borland)", serialQueueBorland },

    // my implementations
    { "SerialQueue (Task based, SpinLock)", serialQueueTasksSpinLock },
    { "SerialQueue (Task based, Monitor)", serialQueueTasksMonitor },
    { "SerialQueue (SpinLock)", serialQueueSpinLock },
    { "SerialQueue (Monitor)", serialQueueMonitor },
};

// log work durations & iterations

Console.WriteLine("Work durations,ms / Iterations \n" + String.Join('\n', workIterations.Select((x, i) => nsToMs(workDurationsNs[i]) + ": " + x)));

// init results

double[][] results = new double[benchmarks.Count][];
for (int i = 0; i < results.Length; i += 1)
{
    results[i] = new double[workDurationsNs.Length];
}

// run benchmarks

for (int i = 0; i < benchmarks.Count; i += 1)
{
    var (label, benchmark) = benchmarks.ElementAt(i);
    var runBenchmark = (int iterations, double workDurationNs) => RunAndGetDuration(() => benchmark(iterations, workDurationNs));

    // warmup

    await runBenchmark(100_000, 1_000);
    GC.Collect();

    // run benchmark for all work duration values

    for (int j = 0; j < workDurationsNs.Length; j += 1)
    {
        double workDurationNs = workDurationsNs[j];
        int iterations = workIterations[j];

        // run benchmark

        var elapsed = await runBenchmark(iterations, workDurationNs);
        GC.Collect();

        // set result and log

        var ticksPerWork = elapsed.Ticks / (double)iterations;
        if (i == 0)
        {
            results[i][j] = ticksPerWork; // log duration for "No Sync" benchmark
        }
        else
        {
            results[i][j] = ticksPerWork - results[0][j]; // log diff with "No Sync" duration for other benchmarks
        }
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + label + " w:" + nsToMs(workDurationNs) + "ms i:" + iterations + " finished in " + nsToSec(elapsed.Ns).ToString("0.###") + "s");
    }
}

// log headers & results

Console.WriteLine("Work duration, ms" + DELIMITER + string.Join(DELIMITER, workDurationsNs.Select(nsToMs)));

for (int i = 0; i < benchmarks.Count; i += 1)
{
    var (label, benchmark) = benchmarks.ElementAt(i);
    Console.WriteLine(label + DELIMITER + string.Join(DELIMITER, results[i].Select(x => x.ToString("0.##"))));
}

#region Utils

void work(double durationNs)
{
    workStopwatch!.Restart();
    while (workStopwatch.Elapsed.TotalNanoseconds < durationNs) { }
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

async Task<(long Ticks, double Ns)> RunAndGetDuration(Func<Task> action)
{
    stopwatch.Restart();
    await action();
    stopwatch.Stop();
    return (stopwatch.ElapsedTicks, stopwatch.Elapsed.TotalNanoseconds);
}

double[] GetDurationsNs()
{
    var enumerator = DurationEnumerator();
    var list = new List<double>();
    while (enumerator.MoveNext())
        list.Add(enumerator.Current);
    return list.ToArray();

    IEnumerator<int> DurationEnumerator()
    {
        int last = 50;

        while (last < 500_000_000)
        {
            last = last.ToString()![0] == '5' ? last * 2 : last * 5;
            yield return last;
        }
    }
}

int GetIterationsForDuration(double durationNs)
{
    const int maxIterations = 100_000;
    const int minIterations = 5_000;
    double maxDurationMins = 5;
    double durationMins = nsToSec(durationNs) / 60;

    return Math.Max(minIterations, Math.Min(maxIterations, (int)Math.Round(maxDurationMins / durationMins)));
}

double nsToSec(double ns)
{
    return ns / 1_000_000_000;
}

double nsToMs(double ns)
{
    return ns / 1_000_000;
}

#endregion

#region Benchmarks

Task noSync(int workIterations, double workNs)
{
    return ParallelFor(workIterations, (callback) =>
    {
        work(workNs); ;
        callback();
    }, true);
}

Task monitor(int workIterations, double workNs)
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

Task spinLock(int workIterations, double workNs)
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

Task mutex(int workIterations, double workNs)
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

Task serialQueueTasksMonitor(int workIterations, double workNs)
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

Task serialQueueTasksSpinLock(int workIterations, double workNs)
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

Task serialQueueTasksSemaphoreSlim(int workIterations, double workNs)
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

Task serialQueueMonitor(int workIterations, double workNs)
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

Task serialQueueSpinLock(int workIterations, double workNs)
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

Task tplDataflowActionBlock(int workIterations, double workNs)
{
    var serialQueue = new SerialQueueTplDataflow();
    return ParallelFor(workIterations, (callback) =>
    {
        serialQueue.Enqueue(() =>
        {
            work(workNs);
            callback();
        });
    });
}

Task serialQueueBorland(int workIterations, double workNs)
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

#endregion

