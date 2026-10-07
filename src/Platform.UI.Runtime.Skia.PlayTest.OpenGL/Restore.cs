using System;
using System.Threading;

namespace CodeBrix.Platform.PlayTest.OpenGL;

// Runs an action once, on the first Dispose: the scope a context's MakeCurrent returns.
internal sealed class Restore : IDisposable
{
    private Action? _action;

    internal Restore(Action action) => _action = action;

    public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
}
