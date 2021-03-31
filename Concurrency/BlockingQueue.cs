using System;
using System.Collections.Generic;
using System.Threading;

namespace Concurrency
{
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
