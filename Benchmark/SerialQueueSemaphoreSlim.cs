namespace Threading
{
    public class SerialQueueSemaphoreSlim
    {
        SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public async Task Enqueue(Action action)
        {
            await Enqueue(() => {
                action();
                return true;
            });
        }

        public async Task<T> Enqueue<T>(Func<T> function)
        {
            await _semaphore.WaitAsync();
            try
            {
                return function();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

