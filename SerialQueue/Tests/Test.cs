using NUnit.Framework;
using System;
using Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace Tests
{
    [TestFixture()]
    public class Test
    {
        SerialQueue _queue;

        [SetUp]
        public void SetUp()
        {
            _queue = new SerialQueue();
        }

        [TearDown]
        public void TearDown()
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = _queue.GetType().GetField("_lastTask", bindFlags);
            Assert.Null(field.GetValue(_queue));
        }

        [Test]
        public async Task TplIsNotFifo()
        {
            // Assign

            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 10000);

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

            var list = new List<int>();
            var tasks = new List<Task>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
                tasks.Add(_queue.Run(() => list.Add(number)));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(range.SequenceEqual(list));
        }

        [Test]
        public async Task QueueFunction()
        {
            // Assign

            var list = new List<int>();
            var tasks = new List<Task<int>>();
            var range = Enumerable.Range(0, 10000);

            // Act

            foreach (var number in range) {
                tasks.Add(_queue.Run(() => {
                    list.Add(number);
                    return number;
                }));
            }

            await Task.WhenAll(tasks);

            // Assert

            Assert.True(tasks.Select(x => x.Result).SequenceEqual(list));
        }
    }
}

