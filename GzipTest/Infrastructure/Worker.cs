using System;

namespace GzipTest.Infrastructure
{
    internal class Worker
    {
        private readonly IThreadPool threadPool;
        private Task? task;

        public Worker(IThreadPool threadPool) => this.threadPool = threadPool;
        private bool IsStarted => task != null;

        public void Run(Action action)
        {
            if (IsStarted)
                throw new InvalidOperationException("Worker already started");

            task = action;
            threadPool.RunTask(task);
        }

        public void Wait()
        {
            if (!IsStarted)
                throw new InvalidOperationException("Worker not started yet");

            threadPool.WaitAll(new[] {task!});
        }
    }
}