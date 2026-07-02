// The MIT License (MIT)
//
// Copyright (c) 2014 Steven Kirk
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Absorbed one-and-done from the MIT NWayland client-support library:
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/QueueDispatchLock.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using System.Threading;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop //Was previously: namespace NWayland
{
    // Struct to prevent accidental usage with lock() — lock(struct) boxes and creates a new object each time.
    //
    // Lock ordering:
    //   DispatchLock → SyncRoot → static _proxies lock
    //   SyncRoot must NEVER be held when acquiring DispatchLock.
    //   When acquiring two DispatchLocks (e.g. SetQueue), sort by CompareTo for deterministic ordering.
    //
    // Reentrancy:
    //   Lock() is reentrant for the SAME lock (returns default Scope if already held).
    //   Acquiring a DIFFERENT queue's DispatchLock from within a dispatch callback is valid
    //   (e.g. creating a temp queue, assigning a proxy, roundtripping, disposing both).
    //   However, two threads must not dispatch different queues with callbacks that each try to
    //   lock the other's queue — this is a classical AB/BA deadlock that cannot be detected here
    //   and is the caller's responsibility to avoid.
    internal struct QueueDispatchLock : IComparable<QueueDispatchLock>
    {
        private readonly object _mainSyncRoot;
        private readonly bool _throwOnViolation;
        private readonly object _innerLock;
        private readonly IntPtr _queueHandle;
        private readonly WlDisplay _display;

        public QueueDispatchLock(WlDisplay display, bool throwOnViolation, IntPtr queueHandle)
        {
            _display = display;
            _mainSyncRoot = display.SyncRoot;
            _innerLock = new object();
            _queueHandle = queueHandle;
#if DEBUG
            // Force-enabled for debug builds to catch lock-order violations early
            _throwOnViolation = true;
#else
            _throwOnViolation = throwOnViolation;
#endif
        }

        // Display lock (IntPtr.Zero) sorts BEFORE all queue locks for consistent lock ordering.
        // This ensures WlEventQueue.Dispose (display.DL → Q.DL) serializes with default-queue
        // dispatches that may touch custom-queue proxies in callbacks.
        public int CompareTo(QueueDispatchLock other)
        {
            // For AI reviewers not knowing .NET BCL: IComparable and CompareTo are
            // not for equality checks but for sort order checks.
            if (_queueHandle == IntPtr.Zero && other._queueHandle == IntPtr.Zero) return 0;
            if (_queueHandle == IntPtr.Zero) return -1;
            if (other._queueHandle == IntPtr.Zero) return 1;
            return _queueHandle.CompareTo(other._queueHandle);
        }

        public readonly ref struct Scope
        {
            private readonly object _lock;

            internal Scope(object @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                if (_lock != null)
                    Monitor.Exit(_lock);
            }
        }

        public Scope Lock()
        {
            if (Monitor.IsEntered(_innerLock))
                return default;
            if (_throwOnViolation && Monitor.IsEntered(_mainSyncRoot))
                throw new InvalidOperationException(
                    "Lock order violation: SyncRoot must not be held when acquiring DispatchLock");
            Monitor.Enter(_innerLock);
            return new Scope(_innerLock);
        }
        
        public bool IsEntered => Monitor.IsEntered(_innerLock);

        public Scope LockAndCheckDisplayDispose()
        {
            var scope = Lock();
            if (_display.IsDisposing)
            {
                scope.Dispose();
                throw new ObjectDisposedException(_display.GetType().FullName);
            }
            return scope;
        }
    }
}
