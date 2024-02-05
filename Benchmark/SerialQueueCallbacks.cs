namespace Threading
{
    public class SerialQueueCallbacks
    {
        class LinkedNode
        {
            public readonly Action Action;
            public LinkedNode? Next;

            public LinkedNode(Action action)
            {
                Action = action;
            }
        }

        public event Action<Action, Exception> UnhandledException = delegate { };

        private LinkedNode? _first;
        private LinkedNode? _last;
        private bool _isRunning = false;
        private System.Threading.SpinLock _spinLock = new();

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
            var newNode = new LinkedNode(action);

            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);

                if (_first == null)
                {
                    _first = newNode;
                    _last = newNode;

                    if (!_isRunning)
                    {
                        _isRunning = true;
                        ThreadPool.QueueUserWorkItem(Run);
                    }
                }
                else
                {
                    _last!.Next = newNode;
                    _last = newNode;
                }
            }
            finally
            {
                if (lockTaken) _spinLock.Exit();
            }
        }

        private void Run(object? _)
        {
            while (true)
            {
                LinkedNode? firstNode;

                bool lockTaken = false;
                try
                {
                    _spinLock.Enter(ref lockTaken);
                    if (_first == null)
                    {
                        _isRunning = false;
                        return;
                    }
                    firstNode = _first;
                    _first = null;
                    _last = null;
                }
                finally
                {
                    if (lockTaken) _spinLock.Exit();
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

