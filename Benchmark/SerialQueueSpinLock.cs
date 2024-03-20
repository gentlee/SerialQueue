﻿namespace Threading
{
    public class SerialQueueSpinlock
    {
        class LinkedListNode
        {
            public readonly Action Action;
            public LinkedListNode? Next;

            public LinkedListNode(Action action)
            {
                Action = action;
            }
        }

        public event Action<Action, Exception> UnhandledException = delegate { };

        private SpinLock _spinLock = new(false);
        private LinkedListNode? _queueFirst;
        private LinkedListNode? _queueLast;
        private bool _isRunning = false;

        public void DispatchSync(Action action)
        {
            var mre = new ManualResetEvent(false);
            DispatchAsync(() =>
            {
                action();
                mre.Set();
            });
            mre.WaitOne();
        }

        public void DispatchAsync(Action action)
        {
            var newNode = new LinkedListNode(action);

            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);

                if (_queueFirst == null)
                {
                    _queueFirst = newNode;
                    _queueLast = newNode;

                    if (!_isRunning)
                    {
                        _isRunning = true;
                        ThreadPool.QueueUserWorkItem(Run);
                    }
                }
                else
                {
                    _queueLast!.Next = newNode;
                    _queueLast = newNode;
                }
            }
            finally
            {
                if (lockTaken) _spinLock.Exit(false);
            }
        }

        private void Run(object? _)
        {
            while (true)
            {
                LinkedListNode? firstNode;

                bool lockTaken = false;
                try
                {
                    _spinLock.Enter(ref lockTaken);
                    if (_queueFirst == null)
                    {
                        _isRunning = false;
                        return;
                    }
                    firstNode = _queueFirst;
                    _queueFirst = null;
                    _queueLast = null;
                }
                finally
                {
                    if (lockTaken) _spinLock.Exit(false);
                }

                while (firstNode != null)
                {
                    var action = firstNode.Action;
                    firstNode = firstNode.Next;
                    try
                    {
                        action();
                    }
                    catch (Exception error)
                    {
                        UnhandledException.Invoke(action, error);
                    }
                }
            }
        }
    }
}

