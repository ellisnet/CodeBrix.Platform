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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Server/IWlResourceImpl.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop.Server; //Was previously: namespace NWayland.Server

/// <summary>
/// Backend implementation for WlResource I/O operations.
/// Per-resource: holds object ID, version, interface, listener, and dispose state.
/// Defined in NWayland so WlResource can reference it; implemented in CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop.Server.
/// </summary>
internal interface IWlResourceImpl
{
    uint ObjectId { get; }
    int Version { get; }
    WlInterfaceDescription Interface { get; }
    IWlEventsListener? Listener { get; set; }
    bool IsDisposed { get; }

    /// <summary>
    /// Register this resource in the object map after construction.
    /// </summary>
    void Register(WlResource resource);

    /// <summary>
    /// Serialize and queue an outgoing event for the client.
    /// </summary>
    void Invoke(WlResource resource, ref WaylandCallBuilder call);

    /// <summary>
    /// Serialize an outgoing event that creates a new resource (new_id arg).
    /// Returns the newly created WlResource.
    /// </summary>
    WlResource InvokeNewId(WlResource resource, ref WaylandCallBuilder call,
        WlProxyTypeDescriptor proxyType, IWlEventsListener? listener, int version);

    /// <summary>
    /// Notify the backend that a resource is being destroyed (removed from object map, etc.).
    /// </summary>
    void Destroy(WlResource resource);

    /// <summary>
    /// Send a <c>wl_display.error</c> referencing this resource and disconnect the client.
    /// </summary>
    void PostError(WlResource resource, uint code, string message);

    /// <summary>
    /// Send a <c>wl_display.error</c> referencing the <c>wl_display</c> object (id 1) — for the
    /// wl_display "global" error codes — and disconnect the client.
    /// </summary>
    void PostGlobalError(uint code, string message);
}
