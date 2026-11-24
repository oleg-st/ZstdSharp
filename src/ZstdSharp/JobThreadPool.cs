using System;
using System.Collections.Generic;
using System.Threading;

namespace ZstdSharp
{
    internal unsafe class JobThreadPool : IDisposable
    {
        private readonly List<Thread> threads = new List<Thread>();
        private readonly Queue<Job> queue;
        private readonly int queueSize;
        private int threadLimit;
        private int busyThreads;
        private bool shuttingDown;

        private struct Job
        {
            public void* function;
            public void* opaque;
        }

        public JobThreadPool(int queueSize)
        {
            this.queueSize = queueSize;
            queue = new Queue<Job>(Math.Max(1, queueSize));
        }

        public bool Resize(int numThreads)
        {
            lock (queue)
            {
                if (numThreads < 1 || shuttingDown)
                    return false;

                try
                {
                    // Reserve the list entries before starting any new threads.
                    if (threads.Capacity < numThreads)
                        threads.Capacity = numThreads;

                    while (threads.Count < numThreads)
                    {
                        var thread = new Thread(Worker);
                        thread.Start();
                        threads.Add(thread);
                    }
                }
                catch (OutOfMemoryException)
                {
                    return false;
                }

                threadLimit = numThreads;
                Monitor.PulseAll(queue);
                return true;
            }
        }

        private void Worker()
        {
            while (true)
            {
                Job job;
                lock (queue)
                {
                    while (queue.Count == 0 || busyThreads >= threadLimit)
                    {
                        if (shuttingDown)
                            return;
                        Monitor.Wait(queue);
                    }

                    job = queue.Dequeue();
                    busyThreads++;
                    Monitor.PulseAll(queue);
                }

                try
                {
                    ((delegate* managed<void*, void>)job.function)(job.opaque);
                }
                finally
                {
                    lock (queue)
                    {
                        busyThreads--;
                        Monitor.PulseAll(queue);
                    }
                }
            }
        }

        // Without queueing, only accept work when a worker is available.
        private bool IsFull() => queueSize == 0
            ? busyThreads >= threadLimit || queue.Count != 0
            : queue.Count >= queueSize;

        private void Enqueue(void* function, void* opaque)
        {
            queue.Enqueue(new Job { function = function, opaque = opaque });
            Monitor.PulseAll(queue);
        }

        public void Add(void* function, void* opaque)
        {
            lock (queue)
            {
                while (!shuttingDown && IsFull())
                    Monitor.Wait(queue);
                if (!shuttingDown)
                    Enqueue(function, opaque);
            }
        }

        public bool TryAdd(void* function, void* opaque)
        {
            lock (queue)
            {
                if (shuttingDown || IsFull())
                    return false;

                Enqueue(function, opaque);
                return true;
            }
        }

        public void JoinJobs()
        {
            lock (queue)
            {
                while (queue.Count != 0 || busyThreads != 0)
                    Monitor.Wait(queue);
            }
        }

        public void Dispose()
        {
            lock (queue)
            {
                shuttingDown = true;
                Monitor.PulseAll(queue);
            }

            foreach (var thread in threads)
                thread.Join();
        }

        public int Size()
        {
            // todo not implemented
            // https://github.com/dotnet/runtime/issues/24200
            return 0;
        }
    }
}
