using CodeBrix.Platform.Graphics;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using SkiaSharp;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// The DirectMetal render path (the macOS head in Metal mode): the window's <c>MTLDevice</c> adopted as a WebGPU
/// device, frame textures described to Skia as <c>id&lt;MTLTexture&gt;</c>s. Selected only on macOS (the rules'
/// <c>DirectAdapterExists</c>); it compiles everywhere and runs nowhere else.
/// </summary>
/// <remarks>
/// WebGPU gets its OWN command queue on the head's device (the engine cannot adopt a queue), so the library waits for
/// every WebGPU submission before returning (WebGpuSkiaDevice.SharesQueueWithSkia is false): a published texture is
/// complete before the paint can see it. BGRA8Unorm is <c>MTLPixelFormatBGRA8Unorm</c>, which Skia's Metal backend
/// wraps directly as <see cref="SKColorType.Bgra8888"/>. Metal has no image layouts; the shader-read transition the
/// common part records is a no-op there.
/// </remarks>
internal sealed unsafe class WebGpuMetalDirectPresenter : WebGpuDirectPresenter
{
	private GRMtlBackendContext? _backendContext;

	internal WebGpuMetalDirectPresenter(INativeGpuDeviceProvider provider, GRContext grContext)
		: base(provider, grContext)
	{
	}

	/// <inheritdoc/>
	internal override WebGpuRenderPath Path => WebGpuRenderPath.DirectMetal;

	/// <summary>Adopts the window's <c>MTLDevice</c> (the queue is the one the head's GPU context submits on).</summary>
	/// <param name="provider">The provider.</param>
	/// <param name="grContext">The head's GPU context.</param>
	/// <returns>The adopted device.</returns>
	private protected override WebGpuSkiaDevice AdoptDevice(INativeGpuDeviceProvider provider, GRContext grContext)
	{
		_backendContext = new GRMtlBackendContext
		{
			DeviceHandle = provider.MetalDevice,
			QueueHandle = provider.MetalCommandQueue,
		};

		return WebGpuSkiaDevice.AdoptMetal(grContext, _backendContext);
	}

	/// <summary>The frame texture's <c>id&lt;MTLTexture&gt;</c>.</summary>
	/// <param name="target">The frame target.</param>
	/// <param name="reason">Why not, or null.</param>
	/// <returns>The backend texture, or null.</returns>
	private protected override GRBackendTexture? DescribeTexture(WebGpuSkiaTarget target, out string? reason)
	{
		var metalTexture = Wgpu.TextureGetNativeMetalTexture(target.Texture);
		if (metalTexture == null)
		{
			reason = "wgpuTextureGetNativeMetalTexture returned null for the frame texture";
			return null;
		}

		var backend = new GRBackendTexture(target.Width, target.Height, false, new GRMtlTextureInfo((nint)metalTexture));
		if (!backend.IsValid)
		{
			backend.Dispose();
			reason = "SkiaSharp rejected the frame texture's Metal description";
			return null;
		}

		reason = null;
		return backend;
	}

	/// <inheritdoc/>
	private protected override void DisposeAdapter()
	{
		_backendContext?.Dispose();
		_backendContext = null;
	}
}
