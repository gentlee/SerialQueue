using Threading.Tasks;

namespace Tests.Tasks
{
    [TestFixture]
    public class SerialQueueTasksTests
    {
        [Test]
        public async Task EnqueueAction()
        {
            // Assign

            const int count = 100000;
            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>(count);
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                tasks.Add(queue.Enqueue(() => list.Add(number)));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task EnqueueFunction()
        {
            // Assign

            const int count = 100000;
            var queue = new SerialQueue();
            var tasks = new List<Task<int>>(count);
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                tasks.Add(queue.Enqueue(() => number));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(tasks.Select(x => x.Result).SequenceEqual(range));
        }

        [Test]
        public async Task EnqueueAsyncAction()
        {
            // Assign

            const int count = 10000;
            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>(count);
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                tasks.Add(queue.Enqueue(async () =>
                {
                    await TestUtils.RandomDelay();
                    list.Add(number);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task EnqueueAsyncFunction()
        {
            // Assign

            const int count = 10000;
            var queue = new SerialQueue();
            var tasks = new List<Task<int>>(count);
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                tasks.Add(queue.Enqueue(async () =>
                {
                    await TestUtils.RandomDelay();
                    return number;
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(tasks.Select(x => x.Result).SequenceEqual(range));
        }

        [Test]
        public async Task EnqueueMixed()
        {
            // Assign

            const int count = 10000;
            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>(count);
            var range = Enumerable.Range(0, count);

            // Act

            foreach (var number in range)
            {
                var index = number % 4;
                if (index == 0)
                {
                    await TestUtils.RandomDelay();
                    tasks.Add(queue.Enqueue(() => list.Add(number)));
                }
                else if (index == 1)
                {
                    await TestUtils.RandomDelay();
                    tasks.Add(queue.Enqueue(() =>
                    {
                        list.Add(number);
                        return number;
                    }));
                }
                else if (index == 2)
                {
                    tasks.Add(queue.Enqueue(async () =>
                    {
                        await TestUtils.RandomDelay();
                        list.Add(number);
                    }));
                }
                else
                {
                    tasks.Add(queue.Enqueue(async () =>
                    {
                        await TestUtils.RandomDelay();
                        list.Add(number);
                        return number;
                    }));
                }
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task EnqueueFromMultipleThreads()
        {
            // Assign

            const int count = 10000;
            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>(count);

            // Act

            var counter = 0;
            for (int i = 0; i < count; i += 1)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await queue.Enqueue(async () =>
                    {
                        var index = counter;
                        counter += 1;
                        await TestUtils.RandomDelay();
                        list.Add(index);
                    });
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(list.SequenceEqual(Enumerable.Range(0, count)));
        }

        [Test]
        public async Task CatchExceptionFromAction()
        {
            // Assign

            var queue = new SerialQueue();
            Exception? exception = null;
            Action action = () => throw new Exception("Test");

            // Act

            await queue.Enqueue(() => Thread.Sleep(10));
            try
            {
                await queue.Enqueue(action);
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Assert

            Assert.AreEqual("Test", exception?.Message);
        }

        [Test]
        public async Task CatchExceptionFromAsyncAction()
        {
            // Assign

            var queue = new SerialQueue();
            var exceptionCatched = false;

            // Act

            await queue.Enqueue(() => Thread.Sleep(10));
            try
            {
                await queue.Enqueue(async () =>
                {
                    await Task.Delay(50);
                    throw new Exception("Test");
                });
            }
            catch (Exception e)
            {
                if (e.Message == "Test")
                {
                    exceptionCatched = true;
                }
            }

            // Assert

            Assert.True(exceptionCatched);
        }


        [Test]
        public async Task CatchExceptionFromAsyncFunction()
        {
            // Assign

            var queue = new SerialQueue();
            var exceptionCatched = false;

            // Act

            await queue.Enqueue(() => Thread.Sleep(10));
            try
            {
                await queue.Enqueue(asyncFunction: async () =>
                {
                    await Task.Delay(50);
                    throw new Exception("Test");
#pragma warning disable CS0162 // Unreachable code detected
                    return false;
#pragma warning restore CS0162 // Unreachable code detected
                });
            }
            catch (Exception e)
            {
                if (e.Message == "Test")
                {
                    exceptionCatched = true;
                }
            }

            // Assert

            Assert.True(exceptionCatched);
        }

        //[Test]
        //public async Task TplIsNotFifo()
        //{
        //    // Assign

        //    const int count = 1000000;
        //    var list = new List<int>();
        //    var tasks = new List<Task>(count);
        //    var range = Enumerable.Range(0, count);

        //    // Act

        //    foreach (var number in range)
        //    {
        //        tasks.Add(Task.Factory.StartNew(() => list.Add(number), TaskCreationOptions.PreferFairness));
        //    }

        //    await Task.WhenAll(tasks);

        //    // Assert

        //    Assert.False(range.SequenceEqual(list));
        //}
    }
}

