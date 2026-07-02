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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/NewId.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland

/// <summary>
/// This struct represents a NEW object reference that has arrived as a part of an event.
/// Since user code is supposed to actually consume the reference and add a listener,
/// the reference is wrapped in this struct. If GetAndConsume is not called before this struct goes out of scope,
/// the object reference is automatically destroyed.
/// </summary>
public ref struct NewId<TProxy, TListener>
    where TListener : class, IWlEventsListener
{
    private readonly INewIdImpl<TProxy> _impl;

    internal NewId(INewIdImpl<TProxy> impl)
    {
        _impl = impl;
    }

    public TProxy GetAndConsume(TListener? listener = null) => _impl.GetAndConsume(listener);
}

interface INewIdImpl<TProxy>
{
    TProxy GetAndConsume(IWlEventsListener? listener);
}

class NewIdImpl<T>(IWlEventArgsImpl args, int index) : INewIdImpl<T> where T : WlProxy
{
    public T GetAndConsume(IWlEventsListener? listener)
    {
        return args.GetNewIdProxy<T>(index, listener);
    }
}

class ServerNewIdImpl<T>(IWlEventArgsImpl args, int index) : INewIdImpl<T> where T : CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop.Server.WlResource
{
    public T GetAndConsume(IWlEventsListener? listener)
    {
        var resource = (T)args.GetServerNewId(index);
        resource.Listener = listener;
        return resource;
    }
}