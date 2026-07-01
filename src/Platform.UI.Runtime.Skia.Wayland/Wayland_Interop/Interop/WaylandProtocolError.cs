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
// https://github.com/AvaloniaUI/NWayland/blob/dd37f6d80e6e18ad6c2e868190899b8a808f851d/src/NWayland/Interop/WaylandProtocolError.cs
// (see tools/WaylandBindingsGenerator/PORTING-NOTES.txt)

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop //Was previously: namespace NWayland.Interop
{
    /// <summary>
    /// Details of a fatal Wayland protocol error reported by the compositor via <c>wl_display.error</c>.
    /// After such an error the display is unusable and must be disposed.
    /// </summary>
    /// <remarks>
    /// This is a plain data type, not an exception: the dispatching calls
    /// (<c>Dispatch</c>/<c>DispatchPending</c>/<c>Roundtrip</c>) keep returning <c>-1</c> on failure
    /// rather than throwing, so callers can handle errors with a single return-code style.
    /// Retrieve this via <see cref="CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland.WlDisplay.GetProtocolError"/> after a
    /// dispatching call returns <c>-1</c>.
    ///
    /// libwayland-client does not retain the compositor's free-form error message (it only logs it via
    /// <c>wl_log</c>), so <see cref="Message"/> is synthesized from the offending interface and its
    /// <c>error</c>-enum entry: name and spec summary when known.
    /// </remarks>
    public sealed class WaylandProtocolError
    {
        public WaylandProtocolError(uint code, string? interfaceName, uint objectId,
            string? errorName, string? errorSummary)
        {
            Code = code;
            InterfaceName = interfaceName;
            ObjectId = objectId;
            ErrorName = errorName;
            ErrorSummary = errorSummary;
        }

        /// <summary>The error code, as defined in the offending interface's <c>error</c> enum.</summary>
        public uint Code { get; }

        /// <summary>The interface of the object that triggered the error, or null if unknown (destroyed object).</summary>
        public string? InterfaceName { get; }

        /// <summary>The id of the object that triggered the error, or 0 if unknown.</summary>
        public uint ObjectId { get; }

        /// <summary>The <c>error</c>-enum entry name for <see cref="Code"/>, if the binding knows it.</summary>
        public string? ErrorName { get; }

        /// <summary>The spec summary for the error entry, if any.</summary>
        public string? ErrorSummary { get; }

        /// <summary>A human-readable description synthesized from the protocol definition.</summary>
        public string Message => Format(Code, InterfaceName, ObjectId, ErrorName, ErrorSummary);

        public override string ToString() => Message;

        private static string Format(uint code, string? interfaceName, uint objectId,
            string? errorName, string? errorSummary)
        {
            var obj = interfaceName != null ? $"{interfaceName}#{objectId}" : "[destroyed object]";
            var name = errorName != null ? $" ({errorName})" : "";
            var summary = errorSummary != null ? $": {errorSummary}" : "";
            return $"Wayland protocol error on {obj}: error {code}{name}{summary}";
        }
    }
}
