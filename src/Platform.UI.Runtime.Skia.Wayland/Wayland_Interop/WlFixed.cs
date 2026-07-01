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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/WlFixed.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop //Was previously: namespace NWayland
{
    public readonly struct WlFixed : IComparable<WlFixed>, IEquatable<WlFixed>
    {
        private readonly int _value;

        public WlFixed(int value)
        {
            _value = value * 256;
        }

        public WlFixed(double value)
        {
            Union u = new() { d = value + (3L << (51 - 8)) };
            _value = (int)u.i;
        }

        public static explicit operator int(WlFixed wlFixed) => wlFixed._value / 256;

        public static explicit operator double(WlFixed wlFixed)
        {
            Union u = new() { i = ((1023L + 44L) << 52) + (1L << 51) + wlFixed._value };
            return u.d - (3L << 43);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Union
        {
            [FieldOffset(0)]
            public double d;

            [FieldOffset(0)]
            public long i;
        }

        public int CompareTo(WlFixed other) => _value.CompareTo(other._value);

        public bool Equals(WlFixed other) => _value == other._value;

        public override bool Equals(object? obj) => obj is WlFixed other && Equals(other);

        public override int GetHashCode() => _value;

        public static bool operator ==(WlFixed left, WlFixed right) => left._value == right._value;

        public static bool operator !=(WlFixed left, WlFixed right) => left._value != right._value;

        public static bool operator <(WlFixed left, WlFixed right) => left._value < right._value;

        public static bool operator <=(WlFixed left, WlFixed right) => left._value <= right._value;

        public static bool operator >(WlFixed left, WlFixed right) => left._value > right._value;

        public static bool operator >=(WlFixed left, WlFixed right) => left._value >= right._value;

        public override string ToString() => ((double)this).ToString("G");
    }
}
