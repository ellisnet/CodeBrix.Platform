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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Interop/WaylandCallBuilder.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland.Interop

public interface IWaylandCallTarget
{
    internal void Invoke(ref WaylandCallBuilder call);

    internal object InvokeNewId(ref WaylandCallBuilder call, WlProxyTypeDescriptor proxyType,
        IWlEventsListener? listener, IWlTargetQueue? queue, uint? newIdVersion);
}

public ref struct WaylandCallBuilder : IDisposable
{
    [ThreadStatic] private static Stack<List<WlArgument>>? _normalArgsPool;
    [ThreadStatic] private static Stack<List<object?>>? _objectArgsPool;
    private static T GetPooled<T>(ref Stack<T>? pool) where T : new()
    {
        pool ??= new Stack<T>();
        if (pool.TryPop(out var result))
            return result;
        return new T();
    }

    private static void ReturnToPool<T>(ref Stack<List<T>>? pool, ref List<T>? value)
    {
        if (value is null)
            return;
        pool ??= new();
        value.Clear();
        pool.Push(value);
        value = null;
    }

    internal List<WlArgument>? NormalArgs;
    internal List<object?>? ObjectArgs;
    private IWaylandCallTarget _target;
    internal uint OpCode;

    public static WaylandCallBuilder Create(IWaylandCallTarget target, uint opcode)
    {
        return new WaylandCallBuilder()
        {
            _target = target,
            OpCode = opcode
        };
    }
    
    private void ObjectArg(object? arg)
    {
        (ObjectArgs ??= GetPooled(ref _objectArgsPool)).Add(arg);
    }

    public void Arg(WlProxy? arg) => ObjectArg(arg);

    public void Arg(CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop.Server.WlResource? arg) => ObjectArg(arg);
    
    public void Arg(string? arg) => ObjectArg(arg);

    private void Add(WlArgument arg) => (NormalArgs ??= GetPooled(ref _normalArgsPool)).Add(arg);
    
    public void ArgNewId() => Add(WlArgument.NewId);

    public void Arg(int i) => Add(new WlArgument
    {
        Int32 = i
    });

    public void Arg(uint i) => Add(new WlArgument()
    {
        UInt32 = i
    });

    public void Arg(WlFixed wlFixed) => Add(new WlArgument()
    {
        WlFixed = wlFixed
    });

    public void Arg<T>(ReadOnlySpan<T> array) where T : unmanaged
    {
        ObjectArg(MemoryMarshal.Cast<T, byte>(array).ToArray());
    }

    public void Invoke()
    {
        _target.Invoke(ref this);
    }

    public T InvokeNewId<T>(IWlEventsListener? listener, IWlTargetQueue? queue, uint? version = null) where T : IWlProxyTypeDescriptorProvider
    {
        return (T)_target.InvokeNewId(ref this, T.ProxyType, listener, queue, version);
    }
    
    public void Dispose()
    {
        ReturnToPool(ref _normalArgsPool, ref NormalArgs);
        ReturnToPool(ref _objectArgsPool, ref ObjectArgs);
    }
}