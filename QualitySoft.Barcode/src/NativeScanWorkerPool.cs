using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QualitySoft.Barcode;

internal sealed class NativeScanWorkerPool : IDisposable
{
    private readonly BlockingCollection<WorkItem> _queue = new BlockingCollection<WorkItem>();
    private readonly Thread[] _workers;
    private bool _disposed;

    public NativeScanWorkerPool(int workerCount, int stackSize)
    {
        if (workerCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), workerCount, "Worker count must be at least one.");
        }

        if (stackSize < 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(stackSize), stackSize, "Stack size must be at least 1 MB.");
        }

        _workers = new Thread[workerCount];
        for (var i = 0; i < _workers.Length; i++)
        {
            _workers[i] = new Thread(WorkerLoop, stackSize)
            {
                IsBackground = true,
                Name = "QS Barcode native scan " + i
            };
            _workers[i].Start();
        }
    }

    public T Invoke<T>(Func<T> action)
    {
        return InvokeAsync(action, CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeScanWorkerPool));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var item = new WorkItem<T>(action, cancellationToken);
        try
        {
            _queue.Add(item, cancellationToken);
        }
        catch (InvalidOperationException ex) when (_queue.IsAddingCompleted)
        {
            throw new ObjectDisposedException(nameof(NativeScanWorkerPool), ex);
        }

        return item.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();
        foreach (var worker in _workers)
        {
            worker.Join();
        }

        _queue.Dispose();
    }

    private void WorkerLoop()
    {
        foreach (var item in _queue.GetConsumingEnumerable())
        {
            item.Execute();
        }
    }

    private abstract class WorkItem
    {
        public abstract void Execute();
    }

    private sealed class WorkItem<T> : WorkItem
    {
        private readonly Func<T> _action;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _registration;
        private readonly TaskCompletionSource<T> _completion;

        public WorkItem(Func<T> action, CancellationToken cancellationToken)
        {
            _action = action;
            _cancellationToken = cancellationToken;
            _completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (cancellationToken.CanBeCanceled)
            {
                _registration = cancellationToken.Register(state =>
                {
                    var completion = (TaskCompletionSource<T>)state!;
#if NET462
                    completion.TrySetCanceled();
#else
                    completion.TrySetCanceled(cancellationToken);
#endif
                }, _completion);
            }
        }

        public Task<T> Task => _completion.Task;

        public override void Execute()
        {
            try
            {
                if (_completion.Task.IsCompleted)
                {
                    return;
                }

                _cancellationToken.ThrowIfCancellationRequested();
                _completion.TrySetResult(_action());
            }
            catch (OperationCanceledException)
            {
#if NET462
                _completion.TrySetCanceled();
#else
                _completion.TrySetCanceled(_cancellationToken);
#endif
            }
            catch (Exception ex)
            {
                _completion.TrySetException(ex);
            }
            finally
            {
                _registration.Dispose();
            }
        }
    }
}
