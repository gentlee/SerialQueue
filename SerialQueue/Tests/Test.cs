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
        public async Task EnqueueAction()
        {
            // Assign

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
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

            var queue = new SerialQueue();
            var tasks = new List<Task<int>>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
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

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 5000);

            // Act

            foreach (var number in range) {
                tasks.Add(queue.Enqueue(async () => {
                    await Task.Delay(1);
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

            var queue = new SerialQueue();
            var tasks = new List<Task<int>>();
            var range = Enumerable.Range(0, 5000);

            // Act

            foreach (var number in range) {
                tasks.Add(queue.Enqueue(async () => {
                    await Task.Delay(1);
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

            var queue = new SerialQueue();
            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
                if (number % 4 == 0) {
                    tasks.Add(queue.Enqueue(() => list.Add(number)));
                }
                else if (number % 3 == 0) {
                    tasks.Add(queue.Enqueue(() => {
                        list.Add(number);
                        return number;
                    }));
                }
                else if (number % 2 == 0) {
                    tasks.Add(queue.Enqueue(async () => {
                        await Task.Delay(1);
                        list.Add(number);
                    }));
                }
                else {
                    tasks.Add(queue.Enqueue(async () => {
                        await Task.Delay(1);
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
                    queue.Enqueue(() => list.Add(counter++));
                });
            }

            while (list.Count != count) { };

            // Assert

            Assert.True(list.SequenceEqual(Enumerable.Range(0, count)));
        }
    }
}

