using System;

namespace GzipTest.Infrastructure
{
    internal class Worker
    {
        private readonly IThreadPool threadPool;
        private Task? task;
        private bool IsStarted => task != null;

        public Worker(IThreadPool threadPool) => this.threadPool = threadPool;

        public void Run(Action action)
        {
            if (IsStarted)
                throw new InvalidOperationException("Worker already started");

            task = new Task(action);
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