using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;

namespace CodeBrix.Platform.PlayTest.Preview;

internal static class PreviewFrames
{
    // Each frame carries its own dimensions: both orientations have the same byte count,
    // so dimensions cannot be inferred from the payload or from the initial window size.
    internal static async Task WriteAsync(Stream output, VirtualFrame frame)
    {
        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), frame.Width);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), frame.Height);
        await output.WriteAsync(header).ConfigureAwait(false);
        await output.WriteAsync(frame.Pixels).ConfigureAwait(false);
        await output.FlushAsync().ConfigureAwait(false);
    }

    internal static async Task<VirtualFrame> ReadAsync(Stream input)
    {
        var header = new byte[8];
        if (await input.ReadAsync(header.AsMemory(0, 1)).ConfigureAwait(false) == 0) return null;
        await input.ReadExactlyAsync(header.AsMemory(1)).ConfigureAwait(false);
        var width = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        if (!((width == 1920 && height == 1080) || (width == 1080 && height == 1920)))
            throw new InvalidDataException($"Invalid PlayTest preview frame dimensions: {width}x{height}.");
        var pixels = new byte[checked(width * height * 4)];
        await input.ReadExactlyAsync(pixels).ConfigureAwait(false);
        return new VirtualFrame(new VirtualScreen(width < height ? ScreenOrientation.Portrait : ScreenOrientation.Landscape), pixels);
    }
}
