using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QualitySoft.Barcode;

internal sealed class NativeScanScheduler : IDisposable
{
    private const int NativeScanThreadStackSize = 16 * 1024 * 1024;
    private readonly BlockingCollection<WorkItem> _queue;
    private readonly Thread[] _threads;
    private bool _disposed;

    public NativeScanScheduler()
    {
        var count = Math.Max(1, Environment.ProcessorCount);
        _queue = new BlockingCollection<WorkItem>(new ConcurrentQueue<WorkItem>(), Math.Max(32, count * 8));
        _threads = new Thread[count];
        for (var i = 0; i < _threads.Length; i++)
        {
            _threads[i] = new Thread(Run, NativeScanThreadStackSize)
            {
                IsBackground = true,
                Name = "QS Barcode native scan"
            };
            _threads[i].Start();
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
            throw new ObjectDisposedException(nameof(NativeScanScheduler));
        }

        var item = new WorkItem<T>(action);
        try
        {
            _queue.Add(item, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ObjectDisposedException(nameof(NativeScanScheduler), ex);
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
        foreach (var thread in _threads)
        {
            thread.Join();
        }

        _queue.Dispose();
    }

    private void Run()
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
        private readonly TaskCompletionSource<T> _completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkItem(Func<T> action)
        {
            _action = action;
        }

        public Task<T> Task => _completion.Task;

        public override void Execute()
        {
            try
            {
                _completion.TrySetResult(_action());
            }
            catch (Exception ex)
            {
                _completion.TrySetException(ex);
            }
        }
    }
}
