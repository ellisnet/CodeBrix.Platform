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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Interop/WlProxyCreationContext.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland.Interop

public class WlProxyCreationContext
{
    internal WlProxyCreationContext(WlDisplay display, WlEventQueue? queue, 
        WlInterfaceDescription @interface, WlProxyHandle proxyHandle,
        IWlEventsListener? listener, bool setDispatcher = true)
    {
        Display = display;
        Queue = queue;
        Interface = @interface;
        ProxyHandle = proxyHandle;
        Listener = listener;
        SetDispatcher = setDispatcher;
    }
    internal WlDisplay Display { get; set; }
    internal WlEventQueue? Queue { get; set; }
    internal WlInterfaceDescription Interface { get; }
    internal WlProxyHandle ProxyHandle { get; }
    internal IWlEventsListener? Listener { get; }
    internal bool SetDispatcher { get; }
}