using System;

namespace GzipTest.Infrastructure
{
    internal class Task : ITask
    {
        private readonly Action task;

        public Task(Action task) => this.task = task;

        public void Run() => task();
    }
}