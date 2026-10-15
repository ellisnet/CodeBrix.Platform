using System;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.PlayTest.WebGpu;

// Proves a new device is one the WebGpuRenderView can use on its Readback path: a small off-screen target of the
// view's own format (BGRA8Unorm) is cleared by one render pass and read back, and the pixel must be the clear colour -
// "a device exists" then really means "it renders and reads back".
internal static class DeviceCheck
{
    private const int Size = 4;

    internal static unsafe void Verify(WebGpuSkiaDevice device)
    {
        using var target = WebGpuSkiaTarget.CreateHeadless(device, Size, Size, new WebGpuSkiaTargetOptions { Format = TextureFormat.BGRA8Unorm });
        if (!target.IsAvailable)
            throw new InvalidOperationException($"A {Size}x{Size} off-screen target could not be created on the device: {target.TierReason}");

        var encoder = target.BeginWebGpuPass();
        if (encoder.IsNull)
            throw new InvalidOperationException("The off-screen target handed out no command encoder.");
        var attachment = new RenderPassColorAttachment
        {
            View = target.TextureView,
            DepthSlice = Constants.DepthSliceUndefined,
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
            ClearValue = new Color { R = 0.25, G = 0.5, B = 0.75, A = 1 },
        };
        var descriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
        var pass = Wgpu.CommandEncoderBeginRenderPass(encoder, &descriptor);
        Wgpu.RenderPassEncoderEnd(pass);
        Wgpu.RenderPassEncoderRelease(pass);
        target.EndWebGpuPass();

        Span<byte> pixels = stackalloc byte[Size * Size * 4];
        if (!target.ReadPixels(pixels))
            throw new InvalidOperationException($"Reading a cleared {Size}x{Size} target back failed: {target.LastError}");

        // BGRA: (191, 128, 64, 255) is the clear colour (0.25, 0.5, 0.75, 1) in that byte order.
        var b = pixels[0]; var g = pixels[1]; var r = pixels[2]; var a = pixels[3];
        if (Math.Abs(r - 64) > 2 || Math.Abs(g - 128) > 2 || Math.Abs(b - 191) > 2 || a != 255)
            throw new InvalidOperationException(
                $"Clearing an off-screen target to (64, 128, 191, 255) read back ({r}, {g}, {b}, {a}): the device does not render.");
    }
}
