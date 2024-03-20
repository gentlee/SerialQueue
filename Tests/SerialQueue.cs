using Threading;

namespace Tests
{
    [TestFixture]
    public class SerialQueueTests
    {
        [Test]
        public void DispatchAsyncFromSingleThread()
        {
            // Assign

            const int count = 100000;
            var queue = new SerialQueue();
            var list = new List<int>();
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                queue.DispatchAsync(() => list.Add(number));
            }

            queue.DispatchSync(() => { });

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task DispatchAsyncFromMultipleThreads()
        {
            // Assign

            const int count = 100000;
            var counter = -123;
            var queue = new SerialQueue();
            var list = new List<int>(count);
            var tasks = new List<Task>(count);

            // Act

            queue.DispatchSync(() =>
            {
                counter = 0;
            });

            for (int i = 0; i < count; i += 1)
            {
                tasks.Add(Task.Run(() =>
                {
                    queue.DispatchAsync(() =>
                    {
                        list.Add(counter);
                        counter += 1;
                    });
                }));
            }

            await Task.WhenAll(tasks);

            queue.DispatchSync(() =>
            {
                counter *= 2;
            });

            // Assert

            Assert.AreEqual(count * 2, counter);
            Assert.True(list.SequenceEqual(Enumerable.Range(0, count)));
        }
    }
}

