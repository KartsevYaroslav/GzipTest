using System.Collections.Generic;
using System.Threading;

namespace GzipTest.Infrastructure
{
    internal class BackgroundThreadPool : IThreadPool
    {
        private readonly BlockingBag<ITask> tasks;
        private readonly Dictionary<ITask, ManualResetEvent> waitHandlerByTask;

        public BackgroundThreadPool(uint workersCount)
        {
            tasks = new BlockingBag<ITask>(workersCount);
            waitHandlerByTask = new Dictionary<ITask, ManualResetEvent>();

            for (var i = 0; i < workersCount; i++)
            {
                var thread = new Thread(Execute) {IsBackground = true};
                thread.Start();
            }
        }

        public void RunTask(ITask task)
        {
            waitHandlerByTask[task] = new ManualResetEvent(false);
            tasks.Add(task);
        }

        public void WaitAll(IEnumerable<ITask> waitedTasks)
        {
            foreach (var task in waitedTasks)
            {
                waitHandlerByTask[task].WaitOne();
                waitHandlerByTask.Remove(task);
            }
        }

        public void Dispose() => tasks.Dispose();

        private void Execute()
        {
            while (tasks.TryTake(out var task))
            {
                task.Run();
                waitHandlerByTask[task].Set();
            }
        }
    }
}