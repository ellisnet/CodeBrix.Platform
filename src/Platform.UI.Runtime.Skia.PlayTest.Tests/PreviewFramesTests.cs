using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;
using CodeBrix.Platform.PlayTest.Preview;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The preview pipe format: per frame, little-endian Int32 width and height, then width*height*4
// BGRA bytes. Both orientations have the same byte count, so the dimensions must travel with
// every frame.
public sealed class PreviewFramesTests
{
    [Theory]
    [InlineData(ScreenOrientation.Landscape, 1920, 1080)]
    [InlineData(ScreenOrientation.Portrait, 1080, 1920)]
    public async Task WriteAsync_then_ReadAsync_round_trips_a_frame(ScreenOrientation orientation, int width, int height)
    {
        //Arrange
        var frame = Frame(orientation, seed: 7);
        using var stream = new MemoryStream();

        //Act
        await PreviewFrames.WriteAsync(stream, frame);
        stream.Position = 0;
        var read = await PreviewFrames.ReadAsync(stream) ?? throw new InvalidDataException("No frame was read.");

        //Assert
        read.Width.Should().Be(width);
        read.Height.Should().Be(height);
        read.Screen.Orientation.Should().Be(orientation);
        read.Pixels.AsSpan().SequenceEqual(frame.Pixels).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_prefixes_little_endian_dimensions()
    {
        //Arrange
        using var stream = new MemoryStream();

        //Act
        await PreviewFrames.WriteAsync(stream, Frame(ScreenOrientation.Portrait, seed: 1));
        var bytes = stream.ToArray();

        //Assert
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4)).Should().Be(1080);
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4)).Should().Be(1920);
        bytes.Length.Should().Be(8 + 1080 * 1920 * 4);
    }

    [Fact]
    public async Task ReadAsync_reads_consecutive_frames_in_order_then_null_at_end_of_stream()
    {
        //Arrange
        using var stream = new MemoryStream();
        await PreviewFrames.WriteAsync(stream, Frame(ScreenOrientation.Landscape, seed: 1));
        await PreviewFrames.WriteAsync(stream, Frame(ScreenOrientation.Portrait, seed: 2));
        stream.Position = 0;

        //Act
        var first = await PreviewFrames.ReadAsync(stream) ?? throw new InvalidDataException("No first frame was read.");
        var second = await PreviewFrames.ReadAsync(stream) ?? throw new InvalidDataException("No second frame was read.");
        var end = await PreviewFrames.ReadAsync(stream);

        //Assert
        first.Screen.Orientation.Should().Be(ScreenOrientation.Landscape);
        first.Pixels[4099].Should().Be(unchecked((byte)4099));
        second.Screen.Orientation.Should().Be(ScreenOrientation.Portrait);
        second.Pixels[4099].Should().Be(unchecked((byte)(4099 * 2)));
        end.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_returns_null_for_an_empty_stream()
        => (await PreviewFrames.ReadAsync(new MemoryStream())).Should().BeNull();

    [Theory]
    [InlineData(800, 600)]
    [InlineData(1920, 1920)]
    [InlineData(1080, 1080)]
    [InlineData(0, 0)]
    [InlineData(-1920, -1080)]
    public async Task ReadAsync_rejects_dimensions_other_than_the_two_virtual_screens(int width, int height)
    {
        //Arrange
        var header = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), height);

        //Act
        Func<Task> read = () => PreviewFrames.ReadAsync(new MemoryStream(header));

        //Assert
        await read.Should().ThrowAsync<InvalidDataException>().WithMessage($"*{width}x{height}*");
    }

    [Fact]
    public async Task ReadAsync_rejects_a_truncated_header()
    {
        //Act
        Func<Task> read = () => PreviewFrames.ReadAsync(new MemoryStream(new byte[] { 0x80, 0x07, 0x00 }));

        //Assert
        await read.Should().ThrowAsync<EndOfStreamException>();
    }

    [Fact]
    public async Task ReadAsync_rejects_truncated_pixels()
    {
        //Arrange
        using var stream = new MemoryStream();
        await PreviewFrames.WriteAsync(stream, Frame(ScreenOrientation.Landscape, seed: 3));
        var truncated = stream.ToArray().AsSpan(0, (int)stream.Length - 1).ToArray();

        //Act
        Func<Task> read = () => PreviewFrames.ReadAsync(new MemoryStream(truncated));

        //Assert
        await read.Should().ThrowAsync<EndOfStreamException>();
    }

    private static VirtualFrame Frame(ScreenOrientation orientation, int seed)
    {
        var screen = new VirtualScreen(orientation);
        var pixels = new byte[screen.Width * screen.Height * 4];
        for (var i = 0; i < pixels.Length; i += 4099) pixels[i] = (byte)(i * seed);
        return new VirtualFrame(screen, pixels);
    }
}
