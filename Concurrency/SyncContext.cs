using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Concurrency
{
    public sealed class SyncContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingQueue<SendOrPostCallbackItem> _messageQueue = new BlockingQueue<SendOrPostCallbackItem>();

        public override SynchronizationContext CreateCopy() => this;


        public override void Post(SendOrPostCallback d, object state) => 
            _messageQueue.Enqueue(new SendOrPostCallbackItem(EExecutionType.Post, d, state, null));

        public override void Send(SendOrPostCallback d, object state)
        {
            using (var handle = new ManualResetEventSlim())
            {
                var callbackItem = new SendOrPostCallbackItem(EExecutionType.Send, d, state, handle);
                _messageQueue.Enqueue(callbackItem);
                handle.Wait();
                if (callbackItem.Exception != null)
                    throw callbackItem.Exception;
            }
        }
        public SendOrPostCallbackItem Recieve()
        {
            var message = _messageQueue.Dequeue();
            if (message is null)
                throw new ArgumentNullException("Message queue was unblocked");
            return message;
        }
        public void Unblock() => _messageQueue.Enqueue(null);
        public void Unblock(int count)
        {
            for (; count > 0; count--)
                _messageQueue.Enqueue(null);
        }
        public void Dispose() => _messageQueue.Dispose();

    }
}
