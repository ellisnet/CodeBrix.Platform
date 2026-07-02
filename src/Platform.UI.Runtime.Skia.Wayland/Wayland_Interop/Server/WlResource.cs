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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Server/WlResource.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop.Server; //Was previously: namespace NWayland.Server

/// <summary>
/// Opaque context passed to generated server resource constructors.
/// Holds the per-resource <see cref="IWlResourceImpl"/> which contains all state.
/// </summary>
public class WlResourceCreationContext
{
    internal WlResourceCreationContext() { }

    internal IWlResourceImpl Impl { get; init; } = null!;
}

public abstract class WlResource : IWaylandCallTarget, IDisposable
{
    private readonly IWlResourceImpl _impl;

    public uint ObjectId => _impl.ObjectId;
    public int Version => _impl.Version;
    public bool IsDisposed => _impl.IsDisposed;
    public WlInterfaceDescription Interface => _impl.Interface;

    public IWlEventsListener? Listener
    {
        get => _impl.Listener;
        internal set => _impl.Listener = value;
    }

    protected WlResource(WlResourceCreationContext context)
    {
        _impl = context.Impl;
        _impl.Register(this);
    }

    void IWaylandCallTarget.Invoke(ref WaylandCallBuilder call)
    {
        _impl.Invoke(this, ref call);
    }

    object IWaylandCallTarget.InvokeNewId(ref WaylandCallBuilder call, WlProxyTypeDescriptor proxyType,
        IWlEventsListener? listener, IWlTargetQueue? queue, uint? newIdVersion)
    {
        return _impl.InvokeNewId(this, ref call, proxyType, listener,
            (int)(newIdVersion ?? (uint)Version));
    }

    /// <summary>
    /// Raise a fatal protocol error against this resource (<c>wl_display.error</c>) and disconnect
    /// the client. When <paramref name="message"/> is null it is resolved from this interface's
    /// <c>error</c> enum entry for <paramref name="code"/> (summary, falling back to the entry name).
    /// Generated resource classes expose a strongly-typed public overload taking the interface's
    /// error enum; this raw overload is for posting codes not defined by the resource's own
    /// interface (e.g. a wl_display global error).
    /// </summary>
    protected void PostError(uint code, string? message = null)
    {
        if (message == null && Interface.TryGetError(code, out var err))
            message = string.IsNullOrEmpty(err.Summary) ? err.Name : err.Summary;
        _impl.PostError(this, code, message ?? $"protocol error {code}");
    }

    /// <summary>
    /// Raise one of the <c>wl_display</c> "global" protocol errors (<c>no_memory</c>,
    /// <c>implementation</c>, <c>invalid_object</c>, <c>invalid_method</c>) and disconnect the
    /// client. These belong to <c>wl_display</c>'s error enum and may be raised from any resource;
    /// the error references the <c>wl_display</c> object (id 1) — matching libwayland — so the
    /// client resolves the correct errno and error name rather than misreading the code against
    /// this resource's own interface. When <paramref name="message"/> is null it is resolved from
    /// <c>wl_display</c>'s error-enum entry for <paramref name="code"/>.
    /// </summary>
    public void PostError(WlDisplay.ErrorEnum code, string? message = null)
    {
        if (message == null && WlDisplay.ProxyType.Interface.TryGetError((uint)code, out var err))
            message = string.IsNullOrEmpty(err.Summary) ? err.Name : err.Summary;
        message ??= $"protocol error {(uint)code}";
        // The error references the wl_display object (id 1), so the offending object's identity
        // is lost on the wire. Embed it in the message as weston does ("…, object <iface>@<id>").
        _impl.PostGlobalError((uint)code, $"{message}, object {Interface.Name}@{ObjectId}");
    }

    public void Dispose()
    {
        _impl.Destroy(this);
    }
}