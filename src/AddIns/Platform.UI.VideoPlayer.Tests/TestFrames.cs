// Ported from CodeBrix.VideoPlayback.Skia.Tests (commit a3f3051, MIT, same author) on 2026-08-30;
// adapted to this add-in's internal presenter and the Platform family's SkiaSharp.

using System;
using CodeBrix.VideoPlayback.Decoding;
using CodeBrix.VideoPlayback.Frames;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests; //was previously: CodeBrix.VideoPlayback.Skia.Tests;

/// <summary>
/// Builds frames in code, so a rendering test can state exactly what went in and check exactly what came out
/// without a container or a codec anywhere near it.
/// </summary>
/// <remarks>
/// Upstream this class also finds the engine's golden ".cbv" corpus on disk. That corpus lives in another
/// repository and this one adds no media of its own, so the corpus helper did not come across: every test
/// here works on frames built right here.
/// </remarks>
public static class TestFrames
{
    /// <summary>
    /// The colour description these frames carry when a test does not care: the one essentially all video in
    /// these containers uses.
    /// </summary>
    public static VideoColorInfo Bt709Limited { get; } = new VideoColorInfo(
        VideoColorPrimaries.Bt709,
        VideoTransferCharacteristics.Bt709,
        VideoMatrixCoefficients.Bt709,
        VideoColorRange.Limited,
        VideoChromaSiting.Vertical);

    /// <summary>Creates a frame filled with a pattern that varies in all three planes.</summary>
    /// <param name="pool">The pool the frame's buffer comes from.</param>
    /// <param name="width">The frame's width in luma samples.</param>
    /// <param name="height">The frame's height in luma samples.</param>
    /// <param name="layout">The plane layout.</param>
    /// <param name="color">The colour description to record on the frame.</param>
    /// <param name="seed">A number that shifts the pattern, so two frames differ.</param>
    /// <param name="timestamp">The frame's timestamp.</param>
    /// <param name="frameNumber">The frame's number.</param>
    /// <returns>A frame the caller owns and must dispose.</returns>
    public static unsafe VideoFrame CreatePattern(
        PinnedFrameBufferPool pool,
        int width,
        int height,
        VideoPixelLayout layout,
        VideoColorInfo color,
        int seed = 0,
        TimeSpan timestamp = default,
        long frameNumber = 0)
    {
        VideoFrameBufferDescriptor descriptor = new VideoFrameBufferDescriptor(width, height, layout, 8);
        VideoFrameBuffer buffer = pool.Rent(descriptor);

        Fill(buffer.Y, (x, y) => (byte)(16 + ((seed * 7) + (x * 3) + (y * 5)) % 220));

        if (!buffer.U.IsEmpty)
        {
            Fill(buffer.U, (x, y) => (byte)(16 + ((seed * 3) + (x * 11) + (y * 2)) % 224));
            Fill(buffer.V, (x, y) => (byte)(16 + ((seed * 5) + (x * 2) + (y * 13)) % 224));
        }

        return VideoFrame.Create(
            buffer,
            new VideoFrameInfo(
                width,
                height,
                width,
                height,
                layout,
                8,
                timestamp,
                timestamp.Ticks,
                frameNumber,
                true,
                color,
                null),
            pool);
    }

    /// <summary>Creates a frame in which every sample of every plane holds one constant.</summary>
    /// <param name="pool">The pool the frame's buffer comes from.</param>
    /// <param name="width">The frame's width in luma samples.</param>
    /// <param name="height">The frame's height in luma samples.</param>
    /// <param name="luma">The value every luma sample takes.</param>
    /// <param name="blueChroma">The value every first-chroma sample takes.</param>
    /// <param name="redChroma">The value every second-chroma sample takes.</param>
    /// <param name="color">The colour description to record on the frame.</param>
    /// <returns>A frame the caller owns and must dispose.</returns>
    public static VideoFrame CreateFlat(
        PinnedFrameBufferPool pool,
        int width,
        int height,
        byte luma,
        byte blueChroma,
        byte redChroma,
        VideoColorInfo color)
    {
        VideoFrameBufferDescriptor descriptor =
            new VideoFrameBufferDescriptor(width, height, VideoPixelLayout.I420, 8);

        VideoFrameBuffer buffer = pool.Rent(descriptor);

        Fill(buffer.Y, (x, y) => luma);
        Fill(buffer.U, (x, y) => blueChroma);
        Fill(buffer.V, (x, y) => redChroma);

        return VideoFrame.Create(
            buffer,
            new VideoFrameInfo(
                width,
                height,
                width,
                height,
                VideoPixelLayout.I420,
                8,
                TimeSpan.Zero,
                0,
                0,
                true,
                color,
                null),
            pool);
    }

    private static unsafe void Fill(VideoFramePlane plane, Func<int, int, byte> sample)
    {
        if (plane.IsEmpty) return;

        byte* data = (byte*)plane.Data;
        for (int y = 0; y < plane.Height; y++)
        {
            byte* row = data + ((long)y * plane.Stride);
            for (int x = 0; x < plane.Width; x++) row[x] = sample(x, y);
        }
    }
}
