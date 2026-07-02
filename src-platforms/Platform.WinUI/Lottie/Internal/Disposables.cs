#nullable enable

using System;
using System.Threading;

namespace CodeBrix.Platform.WinUI.Lottie;

// Minimal internal equivalents of the CodeBrix.Platform disposable helpers used by
// the Lottie source machinery (CodeBrix.Platform.Extensions.Disposables).

internal static class Disposable
{
    private sealed class AnonymousDisposable : IDisposable
    {
        private Action? _dispose;

        public AnonymousDisposable(Action dispose) => _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }

    /// <summary>Creates a disposable that invokes <paramref name="dispose"/> once when disposed.</summary>
    public static IDisposable Create(Action dispose) => new AnonymousDisposable(dispose);
}

/// <summary>
/// Holds a single disposable; assigning a new value disposes the previous one.
/// Disposing the holder disposes the current value and all future assignments.
/// </summary>
internal sealed class SerialDisposable : IDisposable
{
    private readonly object _gate = new();
    private IDisposable? _current;
    private bool _isDisposed;

    public IDisposable? Disposable
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
        set
        {
            IDisposable? previous;
            bool disposeValue;
            lock (_gate)
            {
                previous = _current;
                disposeValue = _isDisposed;
                if (!_isDisposed)
                {
                    _current = value;
                }
            }

            previous?.Dispose();
            if (disposeValue)
            {
                value?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        IDisposable? current;
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            current = _current;
            _current = null;
        }

        current?.Dispose();
    }
}

/// <summary>
/// A disposable that cancels (and disposes) an owned <see cref="CancellationTokenSource"/>.
/// </summary>
internal sealed class CancellationDisposable : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
        }
        finally
        {
            _cts.Dispose();
        }
    }
}
