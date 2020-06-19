using System;

namespace GzipTest.Infrastructure
{
    internal class Worker
    {
        private readonly IThreadPool threadPool;
        private Task? task;
        private bool isStarted;

        public Worker(IThreadPool threadPool) => this.threadPool = threadPool;

        public void Run(Action action)
        {
            if (isStarted)
                throw new InvalidOperationException();

            isStarted = true;
            task = new Task(action);
            threadPool.RunTask(task);
        }

        public void Wait()
        {
            if (!isStarted)
                throw new InvalidOperationException();

            threadPool.WaitAll(new[] {task!});
        }
    }
}