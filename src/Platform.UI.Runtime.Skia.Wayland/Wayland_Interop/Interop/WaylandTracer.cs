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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Interop/WaylandTracer.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland

static class WaylandTracer
{

    static WlTracedArgument[] ConvertArgs(WlMessageDescription msg, WlEventArgs args)
    {
        var traced = new WlTracedArgument[msg.Arguments.Count];
        for (var c = 0; c < msg.Arguments.Count; c++)
        {
            var desc = msg.Arguments[c];

            switch (desc.Code)
            {
                case WaylandArgumentCodes.Fd:
                case WaylandArgumentCodes.Int32:
                    traced[c] = new WlTracedArgument { Int32 = args.GetInt32(c) };
                    break;
                case WaylandArgumentCodes.UInt32:
                    traced[c] = new WlTracedArgument { UInt32 = args.GetUInt32(c) };
                    break;
                case WaylandArgumentCodes.NewId:
                    // Server-created new_id args (in events) hold a wl_proxy*; resolve to id.
                    // On the request path the id isn't assigned until after marshal, so this
                    // reports 0 there, which is the honest pre-marshal value.
                    traced[c] = new WlTracedArgument { UInt32 = args.GetObjectId(c) };
                    break;
                case WaylandArgumentCodes.String:
                    traced[c] = new WlTracedArgument { Object = args.GetString(c) };
                    break;
                case WaylandArgumentCodes.Object:
                    traced[c] = new WlTracedArgument { UInt32 = args.GetObjectId(c) };
                    break;
                case WaylandArgumentCodes.Array:
                    traced[c] = new WlTracedArgument { Object = null };
                    break;
                case WaylandArgumentCodes.Fixed:
                    traced[c] = new WlTracedArgument { Fixed = args.GetWlFixed(c) };
                    break;
            }
        }

        return traced;
    }
    
    public static void TraceEvent(WlDisplay display, WlEventArgs args)
    {
        var tracer = display.Tracer;
        if (tracer == null)
            return;

        try
        {
            var sender = (WlProxy)args.Sender;
            var ev = sender.Interface.Events[(int)args.Opcode];
            tracer.Trace(sender, true, false, ev, ConvertArgs(ev, args));
        }
        catch
        {
            // Tracer is advisory — must not disrupt dispatch
        }
    }

    public static unsafe void TraceCall(WlProxy wlProxy, WaylandCallBuilder call, WlArgument* args,
        IntPtr newProxyHandle)
    {
        var tracer = wlProxy.Display.Tracer;
        if(tracer == null)
            return;

        try
        {
            var msg =  wlProxy.Interface.Methods[(int)call.OpCode];
            // Non-owning view — intentionally not disposed. The args are owned by InvokeCore's stackalloc.
            var eargs = new WlEventArgs(new WlEventArgsImpl(args, wlProxy, call.OpCode, msg));
            var traced = ConvertArgs(msg, eargs);

            // The new_id argument carries no usable id in the request args (it's a placeholder
            // until marshalling assigns one). Fill it from the proxy libwayland just created.
            if (newProxyHandle != IntPtr.Zero)
                for (var c = 0; c < msg.Arguments.Count; c++)
                    if (msg.Arguments[c].Code == WaylandArgumentCodes.NewId)
                    {
                        traced[c] = new WlTracedArgument { UInt32 = LibWayland.GetProxyId(newProxyHandle) };
                        break;
                    }

            tracer.Trace(wlProxy, false, msg.IsDestructor, msg, traced);
        }
        catch
        {
            // Tracer is advisory — must not disrupt requests
        }
    }

    public static void TraceDestroy(WlProxy wlProxy, bool isFromFinalizer, bool nativeCallSkipped)
    {
        var tracer = wlProxy.Display.Tracer;
        if (tracer == null)
            return;

        try
        {
            tracer.TraceDestroy(wlProxy, isFromFinalizer, nativeCallSkipped);
        }
        catch
        {
            // Tracer is advisory — must not disrupt dispose
        }
    }
}

public interface INWaylandTracer
{
    void Trace(WlProxy sender, bool isEvent, bool isDestructor, WlMessageDescription method,
        ReadOnlySpan<WlTracedArgument> args);
    void TraceDestroy(WlProxy proxy, bool isFromFinalizer, bool nativeCallSkipped);
}