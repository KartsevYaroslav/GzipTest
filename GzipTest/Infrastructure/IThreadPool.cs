using System;
using System.Collections.Generic;

namespace GzipTest.Infrastructure
{
    public interface IThreadPool: IDisposable
    {
        public void RunTask(ITask task);
        public void WaitAll(IEnumerable<ITask> waitedTasks);
    }
}