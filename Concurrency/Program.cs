using System;
using System.Threading;

namespace Concurrency
{
    class Program
    {
        static void Main(/*string[] args*/)
        {
            var pool = new TaskPool(4);
            TaskPoolDelayedItemsExecution(pool);
        }
        internal static void TaskPoolDelayedItemsExecution(TaskPool taskPool)
        {
            var pool = taskPool;

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
}
