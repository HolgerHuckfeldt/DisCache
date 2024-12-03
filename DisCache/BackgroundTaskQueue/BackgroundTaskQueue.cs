using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DisCache.BackgroundTaskQueue
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly SemaphoreSlim _concurrencySemaphore;

        public BackgroundTaskQueue(int maxConcurrency)
        {
            _concurrencySemaphore = new SemaphoreSlim(maxConcurrency);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            await _concurrencySemaphore.WaitAsync(cancellationToken);

            if (_workItems.TryDequeue(out var workItem))
            {
                return async ct =>
                {
                    try
                    {
                        await workItem(ct);
                    }
                    finally
                    {
                        _concurrencySemaphore.Release();
                    }
                };
            }
            else
            {
                _concurrencySemaphore.Release();
                return null;
            }
        }
    }
}
