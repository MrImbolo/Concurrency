using System;
using System.Threading;

namespace Concurrency
{
    public class SendOrPostCallbackItem
    {
        public EExecutionType ExecutionType { get; }
        public SendOrPostCallback Callback { get; }
        public object State { get; }
        public ManualResetEventSlim SignalComplete { get; }
        public Exception Exception { get; private set; }

        public SendOrPostCallbackItem(
            EExecutionType executionType, SendOrPostCallback callback, 
            object state, ManualResetEventSlim signalComplete)
        {
            ExecutionType = executionType;
            Callback = callback;
            State = state;
            SignalComplete = signalComplete;
        }

        public void Execute()
        {
            if (ExecutionType == EExecutionType.Post)
                Callback(State);

            else if (ExecutionType == EExecutionType.Send)
            {
                try
                {
                    Callback(State);
                }
                catch (Exception ex)
                {
                    Exception = ex;
                }
                SignalComplete.Set();
            }
            else
                throw new ArgumentException($"{nameof(ExecutionType)} is not a valid value.");
        }
    }
}
