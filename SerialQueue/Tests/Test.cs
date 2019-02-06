using NUnit.Framework;
using System;
using Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Tests
{
    [TestFixture()]
    public class Test
    {
        [Test]
        public async Task TplIsNotFifo()
        {
            // Assign

            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 100000);

            // Act

            foreach (var number in range) {
                tasks.Add(Task.Factory.StartNew(() => list.Add(number), TaskCreationOptions.PreferFairness));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.False(range.SequenceEqual(list));
        }

        [Test]
        public async Task QueueAction()
        {
            // Assign

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
                tasks.Add(queue.EnqueueAction(() => list.Add(number)));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task QueueAsyncFunctionAsNormalFunction()
        {
            // Assign

            var queue = new SerialQueue();
            bool success = false;

            // Act

            try
            {
                await queue.EnqueueFunction(async () =>
                {
                    await Task.Delay(50);
                });
                success = true;
            }
            catch (Exception)
            {
            }

            Assert.False(success);
        }

        [Test]
        public async Task QueueAsyncFunction()
        {
            // Assign

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 100);

            // Act

            foreach (var number in range) {
                tasks.Add(queue.EnqueueAsyncFunction(async () =>
                {
                    await Task.Delay(50);
                    list.Add(number);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task QueueAsyncFunctionWithResult()
        {
            // Assign

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 100);

            // Act

            foreach (var number in range) {
                list.Add(await queue.EnqueueAsyncFunction(async () =>
                {
                    await Task.Delay(50);
                    return number;
                }));
            }

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task QueueFunction()
        {
            // Assign

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task<int>>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
                tasks.Add(queue.EnqueueFunction(() => {
                    list.Add(number);
                    return number;
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(tasks.Select(x => x.Result).SequenceEqual(list));
        }

        [Test]
        public void EnqueueFromMultipleThreads()
        {
            // Assign

            const int count = 10000;
            var queue = new SerialQueue();
            var list = new List<int>();

            // Act

            var counter = 0;
            for (int i = 0; i < count; i++) {
                Task.Run(() => {
                    queue.EnqueueAction(() => list.Add(counter++));
                });
            }

            while (list.Count != count) {};

            // Assert

            Assert.True(list.SequenceEqual(Enumerable.Range(0, count)));
        }
    }
}

