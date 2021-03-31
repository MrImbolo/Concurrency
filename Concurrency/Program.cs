using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    class Program
    {
        static void Main(string[] args)
        {
            var pool = new TaskPool(4);

            for (int i = 0; i < 100; i++)
            {
                var id = i;
                pool.Run(() =>
                {
                    Thread.Sleep(500);
                    Console.WriteLine($"{id}: Hello there! Delay is gone");
                });
            }
        }
    }
    public class TaskPool : TaskScheduler, IDisposable
    {
        private bool _disposed;
        private readonly object _lock = new object();
        private BlockingQueue<Task> _queue = new BlockingQueue<Task>();
        private Thread[] _threads;

        public int ThreadCount { get; }

        public TaskPool(int threadCount, bool isBackground = false)
        {
            if (threadCount < 1)
                throw new ArgumentOutOfRangeException($"{nameof(threadCount)} is {threadCount} but must be at least 1.");

            ThreadCount = threadCount;
            _threads = new Thread[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                _threads[i] = new Thread(ExecuteTasks)
                {
                    IsBackground = isBackground
                };
                _threads[i].Start();
            }
        }
        public Task Run(Action action) => 
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this);

        private void ExecuteTasks()
        {
            while(!_disposed)
            {
                var task = _queue.Dequeue();
                if (task is null)
                    return;
                TryExecuteTask(task);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            for (int i = 0; i < _threads.Length; i++)
            {
                _queue.Enqueue(null);
            }

            foreach (var thread in _threads)
                thread.Join();

            _threads = null;
            _queue.Dispose();
        }

        protected override IEnumerable<Task> GetScheduledTasks() => _queue.ToArray();

        protected override void QueueTask(Task task)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(TaskPool).FullName);
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            try
            {
                _queue.Enqueue(task);
            }
            catch(ObjectDisposedException e)
            {
                throw new ObjectDisposedException(typeof(TaskPool).FullName, e);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(TaskPool).FullName);
            return !taskWasPreviouslyQueued && TryExecuteTask(task);
        }
    }

    public sealed class BlockingQueue<T> : IDisposable
    {
        private bool _disposed;
        private readonly object _lock = new object();
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly Semaphore _pool = new Semaphore(0, int.MaxValue);

        public void Enqueue(T item)
        {
            lock(_lock)
            {
                _queue.Enqueue(item);
                _pool.Release();
            }
        }
        public T Dequeue()
        {
            _pool.WaitOne();
            lock (_lock)
            {
                return _queue.Dequeue();
            }
        }

        internal IEnumerable<T> ToArray() => _queue.ToArray();

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }
            _pool.Dispose();
            _queue.Clear();
        }
    }
}
