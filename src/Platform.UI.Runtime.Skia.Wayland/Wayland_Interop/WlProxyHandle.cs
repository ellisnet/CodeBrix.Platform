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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/WlProxyHandle.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using System.Threading;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland

public enum WlProxyDestroyMethod
{
    ProxyDestroy,
    DisplayDisconnect
}

/// <summary>
/// Wraps a native wl_proxy* (or wl_display*) pointer with safe disposal semantics.
/// Prevents double-free via atomic TakeHandle and provides an ODE-checked Handle accessor.
/// Owns the handle and calls the appropriate native destroy function on Dispose
/// (wl_proxy_destroy or wl_display_disconnect) if <see cref="OwnsHandle"/> is true.
/// </summary>
public class WlProxyHandle : IDisposable
{
    private IntPtr _handle;
    private readonly bool _ownsHandle;
    private readonly WlProxyDestroyMethod _destroyMethod;

    public WlProxyHandle(IntPtr handle, bool ownsHandle,
        WlProxyDestroyMethod destroyMethod = WlProxyDestroyMethod.ProxyDestroy)
    {
        _handle = handle;
        _ownsHandle = ownsHandle;
        _destroyMethod = destroyMethod;
    }

    public IntPtr Handle => _handle == IntPtr.Zero
        ? throw new ObjectDisposedException(nameof(WlProxyHandle))
        : _handle;

    public bool IsDisposed => _handle == IntPtr.Zero;

    public bool OwnsHandle => _ownsHandle;

    /// <summary>
    /// Atomically zeros the handle and returns the previous value.
    /// Returns IntPtr.Zero if already taken (preventing double-free).
    /// Does NOT call any native destroy function — use this when you need
    /// to invalidate the handle without destroying (e.g., when the display
    /// is already disconnected, or when the native side already destroyed
    /// the proxy via a destructor request).
    /// </summary>
    public IntPtr TakeHandle() => Interlocked.Exchange(ref _handle, IntPtr.Zero);

    /// <summary>
    /// Atomically takes the handle and, if this instance owns it, calls the
    /// appropriate native destroy function (wl_proxy_destroy or wl_display_disconnect).
    /// Safe to call multiple times — only the first call performs destruction.
    /// </summary>
    public void Dispose()
    {
        var handle = TakeHandle();
        if (handle != IntPtr.Zero && _ownsHandle)
        {
            switch (_destroyMethod)
            {
                case WlProxyDestroyMethod.ProxyDestroy:
                    LibWayland.wl_proxy_destroy(handle);
                    break;
                case WlProxyDestroyMethod.DisplayDisconnect:
                    LibWayland.wl_display_disconnect(handle);
                    break;
            }
        }
    }
}
