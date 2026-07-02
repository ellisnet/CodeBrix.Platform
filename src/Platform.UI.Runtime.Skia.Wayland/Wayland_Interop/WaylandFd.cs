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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/WaylandFd.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop; //Was previously: namespace NWayland

/// <summary>
/// A ref struct wrapping a file descriptor received from a Wayland event.
/// The caller must call <see cref="Consume"/> to take ownership of the FD.
/// If not consumed before going out of scope, the FD is automatically closed.
/// </summary>
public ref struct WaylandFd
{
    private readonly IWlEventArgsImpl _impl;
    private readonly int _index;
    private bool _consumed;

    internal WaylandFd(IWlEventArgsImpl impl, int index)
    {
        _impl = impl;
        _index = index;
        _consumed = false;
    }

    /// <summary>
    /// Take ownership of the file descriptor. Returns the raw FD value.
    /// Can only be called once.
    /// </summary>
    public int Consume()
    {
        if (_consumed)
            throw new System.InvalidOperationException("FD already consumed");
        _consumed = true;
        return _impl.GetFd(_index);
    }

    /// <summary>
    /// Closes the file descriptor if it was not consumed.
    /// </summary>
    public void Dispose()
    {
        if (!_consumed)
        {
            _consumed = true;
            _impl.CloseFd(_index);
        }
    }
}
