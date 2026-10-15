using System;
using System.Threading;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using SkiaSharp;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// What the paint side of a direct render path reads: the most recently published frame as an image on the head's own
/// Skia GPU context. Implemented by <see cref="WebGpuDirectPresenter"/>; the surface holds one while the path runs.
/// </summary>
internal interface IWebGpuDirectFrameSource
{
	/// <summary>
	/// The latest published frame as an SKImage on the head's GPU context, or null when there is none (or the path
	/// has failed). The image is BORROWED and cached by the source - draw it, never dispose it. Called from the paint
	/// (with the surface's gate held).
	/// </summary>
	/// <returns>The image, or null.</returns>
	SKImage? AcquirePaintImage();

	/// <summary>
	/// An independent raster copy of the latest published frame (the caller owns it), or null - made by drawing the
	/// GPU image into a GPU surface and reading that back, so the frame texture itself is only ever sampled.
	/// </summary>
	/// <returns>The copy, or null.</returns>
	SKImage? CaptureFrame();
}

/// <summary>
/// Which step of a direct render path a test (or a diagnostic run) makes fail on purpose, to prove the fallback to
/// Readback at that step.
/// </summary>
internal enum WebGpuDirectFailurePoint
{
	/// <summary>Nothing is forced to fail.</summary>
	None = 0,

	/// <summary>Adopting the head's device is reported as refused.</summary>
	Adopt = 1,

	/// <summary>Creating the frame textures on the adopted device is reported as refused.</summary>
	Target = 2,

	/// <summary>Wrapping a published frame as an image on the head's GPU context returns nothing (the paint side).</summary>
	Wrap = 3,
}

/// <summary>
/// The DIRECT render paths' common part: WebGPU runs on the head's OWN GPU device (adopted through
/// <see cref="INativeGpuDeviceProvider"/>), frames are rendered into GPU textures the size of the element, and the
/// paint draws the newest finished texture through the head's own Skia GPU context - zero processor readback. The
/// Vulkan and Metal adapters supply the adoption and the texture description.
/// </summary>
/// <remarks>
/// <para>
/// <b>Frame textures.</b> THREE textures, each one a headless <see cref="WebGpuSkiaTarget"/> on the adopted device
/// (which validates the frame's commands in an error scope and submits them). A frame renders into a texture that is
/// neither the newest published one nor the one the paint last drew. Two would be a double buffer if the paint drew
/// on the head's render thread; it does not: the head's paint RECORDS each window frame into a picture on the
/// user-interface thread and the render thread replays that picture later, possibly more than once - so the texture
/// the newest picture shows, the newest published one and the one being written must all differ.
/// </para>
/// <para>
/// <b>Hand-over to Skia.</b> At the end of a frame the finished texture is transitioned to the shader-read state in
/// the SAME submission (CodebrixCommandEncoderTransitionTexture), and that submission is made inside the provider's
/// frame scope (the head's device lock on Vulkan: queue access is externally synchronized with the head's own
/// submissions). On a queue shared with Skia that is all the synchronization needed: the transition's barrier orders
/// the frame's writes before every later command on the queue, Skia's sampling included - no wait, no stall. Where
/// WebGPU has its own queue (Metal) the library waits for each submission before returning.
/// </para>
/// <para>
/// <b>Paint side.</b> Each texture is wrapped ONCE as a borrowed SKImage on the head's context (inside the provider's
/// frame scope) and the wrapper is cached; painting draws it. A wrapper holds its own reference on the texture, which
/// Skia's release callback drops when Skia - and the GPU work that sampled it - are done, so a texture a recorded
/// picture still shows outlives a resize or the view's teardown for exactly as long as needed.
/// </para>
/// <para>
/// <b>Errors.</b> The adopted device has no uncaptured-error handler of ours (the engine would end the process): every
/// frame and the DeviceReady callbacks run inside a validation error scope this class opens and closes; what a scope
/// catches is reported through <see cref="TakeErrors"/> / <see cref="EndDeviceWork"/> (log line + DeviceLost).
/// </para>
/// </remarks>
internal abstract unsafe class WebGpuDirectPresenter : WebGpuPresenter, IWebGpuDirectFrameSource
{
	/// <summary>How many frame textures the path keeps (see the remarks).</summary>
	internal const int SlotCount = 3;

	private static readonly WebGpuSkiaTargetOptions TargetOptions = new() { Format = Format };
	private static readonly SKImageTextureReleaseDelegate ReleaseWrappedTexture = OnWrappedTextureReleased;

	private readonly INativeGpuDeviceProvider _provider;
	private readonly GRContext _grContext;
	private readonly Slot[] _slots = new Slot[SlotCount];
	private readonly object _slotsGate = new();

	private WebGpuSkiaDevice? _device;
	private int _write = -1;          // frame thread only
	private volatile int _published = -1;
	private volatile int _painted = -1;
	private string? _frameScopeError;
	private bool _frameScopeOpen;
	private volatile string? _failure;
	private bool _disposed;

	/// <summary>Creates the presenter over a provider that is available; <see cref="Adopt"/> does the rest.</summary>
	/// <param name="provider">The head's provider.</param>
	/// <param name="grContext">The head's GPU context (read once: the provider's is null when it is unavailable).</param>
	private protected WebGpuDirectPresenter(INativeGpuDeviceProvider provider, GRContext grContext)
	{
		_provider = provider;
		_grContext = grContext;
		for (var i = 0; i < SlotCount; i++)
		{
			_slots[i] = new Slot();
		}
	}

	/// <summary>Forces one step of every direct path to fail (tests and diagnostic runs; None in production).</summary>
	internal static WebGpuDirectFailurePoint InjectedFailure { get; set; }

	/// <summary>
	/// Adopts the head's device behind <paramref name="provider"/> and returns the presenter for its kind, or null with
	/// the reason when the path cannot be used here.
	/// </summary>
	/// <param name="provider">An available provider.</param>
	/// <param name="reason">Why there is no presenter, or null.</param>
	/// <returns>The presenter, or null.</returns>
	internal static WebGpuDirectPresenter? TryCreate(INativeGpuDeviceProvider provider, out string? reason)
	{
		if (provider.GRContext is not { } grContext)
		{
			reason = "the head's GPU context is not available";
			return null;
		}

		WebGpuDirectPresenter presenter = provider.Kind == NativeGpuDeviceKind.Metal
			? new WebGpuMetalDirectPresenter(provider, grContext)
			: new WebGpuVulkanDirectPresenter(provider, grContext);

		try
		{
			if (presenter.Adopt(out reason))
			{
				return presenter;
			}
		}
		catch (Exception e)
		{
			reason = e.Message;
		}

		presenter.Dispose();
		return null;
	}

	/// <summary>The head's provider.</summary>
	private protected INativeGpuDeviceProvider Provider => _provider;

	/// <summary>The head's Skia GPU context.</summary>
	private protected GRContext GrContext => _grContext;

	/// <inheritdoc/>
	internal override WebGpuSkiaDevice Device => _device!;

	/// <inheritdoc/>
	internal override WebGpuSkiaTarget? Target => _write >= 0 ? _slots[_write].Target : null;

	/// <inheritdoc/>
	internal override string? FailureReason => _failure ?? (_provider.IsAvailable ? null : _provider.Reason ?? "the head's GPU device is gone");

	/// <inheritdoc/>
	internal override bool HeadDeviceGone => _failure is null && !_provider.IsAvailable;

	/// <summary>Whether WebGPU submits to the very queue Skia renders with (no waits between the two).</summary>
	internal bool SharesQueueWithSkia => _device?.SharesQueueWithSkia ?? false;

	/// <summary>The index of the newest published texture (-1: none yet). Tests.</summary>
	internal int PublishedSlot => _published;

	/// <summary>The index of the texture the paint drew last (-1: none yet). Tests.</summary>
	internal int PaintedSlot => _painted;

	/// <summary>The index of the texture the open frame renders into (-1 outside a frame). Tests.</summary>
	internal int WriteSlot => _write;

	/// <summary>
	/// How many processor readbacks the frame textures in use have performed: zero on a direct path (the paint samples
	/// them on the GPU). Tests.
	/// </summary>
	internal long ReadbackCount
	{
		get
		{
			long count = 0;
			lock (_slotsGate)
			{
				foreach (var slot in _slots)
				{
					count += slot.Target?.ReadbackCount ?? 0;
				}
			}

			return count;
		}
	}

	/// <summary>How many frames the frame textures in use have submitted. Tests.</summary>
	internal long FrameCount
	{
		get
		{
			long count = 0;
			lock (_slotsGate)
			{
				foreach (var slot in _slots)
				{
					count += slot.Target?.FrameCount ?? 0;
				}
			}

			return count;
		}
	}

	/// <summary>Creates the adopted device on the head's (inside the provider's frame scope).</summary>
	/// <param name="provider">The provider.</param>
	/// <param name="grContext">The head's GPU context.</param>
	/// <returns>The device (check IsAvailable).</returns>
	private protected abstract WebGpuSkiaDevice AdoptDevice(INativeGpuDeviceProvider provider, GRContext grContext);

	/// <summary>
	/// Describes a frame texture to Skia - its native image, format and the shader-read layout/state it is left in -
	/// or returns null with the reason.
	/// </summary>
	/// <param name="target">The frame target.</param>
	/// <param name="reason">Why it cannot be described, or null.</param>
	/// <returns>The backend texture (the caller disposes it), or null.</returns>
	private protected abstract GRBackendTexture? DescribeTexture(WebGpuSkiaTarget target, out string? reason);

	private bool Adopt(out string? reason)
	{
		if (InjectedFailure == WebGpuDirectFailurePoint.Adopt)
		{
			reason = "adoption refused (injected failure)";
			return false;
		}

		WebGpuSkiaDevice device;
		using (_provider.BeginFrame())
		{
			device = AdoptDevice(_provider, _grContext);
		}

		if (!device.IsAvailable)
		{
			reason = device.Reason ?? "the head's device could not be adopted";
			device.Dispose();
			return false;
		}

		_device = device;
		reason = null;
		return true;
	}

	// ------------------------------------------------------------------ frame side (frame gate held)

	/// <summary>
	/// Picks the texture this frame renders into - never the newest published one nor the one the paint drew last -
	/// and makes sure it is <paramref name="width"/> x <paramref name="height"/> (a new size is a new texture; the old
	/// one's wrapper is let go, and Skia's release keeps the texture alive while a picture still shows it).
	/// </summary>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <param name="reason">Why there is no usable target, or null.</param>
	/// <returns>True when a usable target exists.</returns>
	internal override bool EnsureTarget(int width, int height, out string? reason)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		reason = null;
		var published = _published;
		var painted = _painted;
		var index = 0;
		while (index == published || index == painted)
		{
			index++;
		}

		if (InjectedFailure == WebGpuDirectFailurePoint.Target)
		{
			reason = "the frame texture was refused (injected failure)";
			_failure = reason;
			return false;
		}

		var slot = _slots[index];
		if (slot.Target is { } current && current.Width == width && current.Height == height && current.IsAvailable)
		{
			_write = index;
			return true;
		}

		RetireSlot(slot);

		var target = WebGpuSkiaTarget.CreateHeadless(Device, width, height, TargetOptions);
		if (!target.IsAvailable)
		{
			reason = target.TierReason;
			target.Dispose();
			_failure = "the frame texture could not be created on the adopted device: " + reason;
			return false;
		}

		lock (_slotsGate)
		{
			slot.Target = target;
		}

		_write = index;
		return true;
	}

	/// <summary>Opens the frame's validation error scope (the adopted device has no handler of ours).</summary>
	internal override void OnFrameOpened()
	{
		_frameScopeError = null;
		WebGpuErrorScope.Push(Device.Device);
		_frameScopeOpen = true;
	}

	/// <summary>
	/// Closes the frame's error scope, records the transition of the finished texture to the shader-read state in the
	/// frame's own encoder, and submits it inside the provider's frame scope.
	/// </summary>
	/// <param name="encoder">The frame's encoder.</param>
	/// <exception cref="WebGpuSkiaValidationException">The frame's commands failed validation; nothing was submitted.</exception>
	internal override void EndPass(CommandEncoder encoder)
	{
		var device = Device;
		if (_frameScopeOpen)
		{
			_frameScopeOpen = false;
			_frameScopeError = WebGpuErrorScope.Pop(device.Instance, device.Device);
		}

		var target = _slots[_write].Target!;
		if (!encoder.IsNull && Wgpu.CodebrixCommandEncoderTransitionTexture(encoder, target.Texture, CodebrixTextureState.ShaderResource) != Status.Success)
		{
			_failure = "the frame texture's transition to the shader-read state was refused";
		}

		using (_provider.BeginFrame())
		{
			target.EndWebGpuPass();
		}
	}

	/// <summary>Publishes the frame <see cref="EndPass"/> submitted and asks the surface to show it.</summary>
	/// <param name="host">The engine's owner.</param>
	internal override void Show(IWebGpuRenderEngineHost host)
	{
		var index = _write;
		_write = -1;
		if (index < 0)
		{
			return;
		}

		lock (_slotsGate)
		{
			_published = index;
		}

		host.PresentDirect(this);
	}

	/// <summary>What the frame's error scope caught (device-level calls the renderer made during the frame), or null.</summary>
	/// <returns>The message, or null.</returns>
	internal override string? TakeErrors()
	{
		var error = _frameScopeError;
		_frameScopeError = null;
		_write = -1;
		return error;
	}

	/// <inheritdoc/>
	internal override void BeginDeviceWork() => WebGpuErrorScope.Push(Device.Device);

	/// <inheritdoc/>
	internal override string? EndDeviceWork() => WebGpuErrorScope.Pop(Device.Instance, Device.Device);

	// ------------------------------------------------------------------ paint side (surface gate held)

	/// <inheritdoc/>
	public SKImage? AcquirePaintImage()
	{
		lock (_slotsGate)
		{
			var index = _published;
			if (index < 0 || _disposed || _failure is not null)
			{
				return null;
			}

			var image = EnsureWrapped(_slots[index]);
			if (image is not null)
			{
				_painted = index;
			}

			return image;
		}
	}

	/// <inheritdoc/>
	public SKImage? CaptureFrame()
	{
		lock (_slotsGate)
		{
			var index = _published;
			if (index < 0 || _disposed || _failure is not null)
			{
				return null;
			}

			using (_provider.BeginFrame())
			{
				var image = EnsureWrapped(_slots[index]);
				if (image is null)
				{
					return null;
				}

				var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
				using var surface = SKSurface.Create(_grContext, false, info);
				if (surface is null)
				{
					return null;
				}

				// Drawn, not read: reading the frame texture itself would leave it in a transfer layout WebGPU does
				// not know about. Sampling it keeps it in the shader-read state both sides agree on.
				surface.Canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), null);
				var copy = new SKBitmap(info);
				if (!surface.ReadPixels(info, copy.GetPixels(), copy.RowBytes, 0, 0))
				{
					copy.Dispose();
					return null;
				}

				copy.SetImmutable();
				return SKImage.FromBitmap(copy);
			}
		}
	}

	// _slotsGate held. The slot's cached wrapper, made (once per texture) inside the provider's frame scope.
	private SKImage? EnsureWrapped(Slot slot)
	{
		// An injected failure applies to every paint, cached wrapper or not, so a running path can be made to fail.
		string? reason;
		SKImage? image = null;
		if (InjectedFailure == WebGpuDirectFailurePoint.Wrap)
		{
			reason = "the frame texture could not be wrapped as an image on the head's GPU context (injected failure)";
		}
		else
		{
			if (slot.Image is { } cached)
			{
				return cached;
			}

			if (slot.Target is not { } target)
			{
				return null;
			}

			using (_provider.BeginFrame())
			{
				image = Wrap(target, out reason);
			}
		}

		if (image is null)
		{
			_failure = reason ?? "the frame texture could not be wrapped as an image on the head's GPU context";
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("Direct frame wrap failed: " + _failure);
			}

			return null;
		}

		slot.Image = image;
		return image;
	}

	// Provider frame scope held. Borrow (never adopt) the texture: Skia must not destroy it; the extra reference taken
	// here is the wrapper's, dropped by Skia's release callback.
	private SKImage? Wrap(WebGpuSkiaTarget target, out string? reason)
	{
		using var backend = DescribeTexture(target, out reason);
		if (backend is null)
		{
			return null;
		}

		var texture = target.Texture;
		Wgpu.TextureAddRef(texture);
		var reference = new WrappedTextureReference(texture);
		var image = SKImage.FromTexture(_grContext, backend, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888, SKAlphaType.Premul, null, ReleaseWrappedTexture, reference);
		if (image is null)
		{
			// Skia calls the release callback itself when it refuses a texture; the reference makes the release
			// happen exactly once either way.
			reference.Release();
			reason = "SKImage.FromTexture returned null for the frame texture on the head's GPU context";
		}

		return image;
	}

	private static void OnWrappedTextureReleased(object context) => (context as WrappedTextureReference)?.Release();

	// Drops a slot's wrapper and target. The wrapper is OUR reference only (a recorded picture may hold more); the
	// texture lives on until Skia's release callback.
	private void RetireSlot(Slot slot)
	{
		SKImage? image;
		WebGpuSkiaTarget? target;
		lock (_slotsGate)
		{
			image = slot.Image;
			target = slot.Target;
			slot.Image = null;
			slot.Target = null;
		}

		if (image is not null)
		{
			using (_provider.BeginFrame())
			{
				image.Dispose();
			}
		}

		target?.Dispose();
	}

	/// <summary>
	/// Releases the textures' wrappers and targets, then the adopted device - inside the provider's frame scope (the
	/// engine may touch the queue while it lets go). When the head's device is already gone (its window closed)
	/// nothing is touched: the WebGPU objects of a destroyed device can only be leaked.
	/// </summary>
	public override void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		lock (_slotsGate)
		{
			_disposed = true;
		}

		if (!_provider.IsAvailable)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("The head's GPU device is gone; the adopted WebGPU objects are left as they are.");
			}

			return;
		}

		using (_provider.BeginFrame())
		{
			for (var i = 0; i < SlotCount; i++)
			{
				RetireSlot(_slots[i]);
			}

			_device?.Dispose();
			_device = null;
		}

		DisposeAdapter();
	}

	/// <summary>Releases what the adapter itself made for the adoption (backend context objects).</summary>
	private protected virtual void DisposeAdapter()
	{
	}

	private sealed class Slot
	{
		public WebGpuSkiaTarget? Target;
		public SKImage? Image;
	}

	// The wrapper's own reference on a frame texture, released exactly once.
	private sealed class WrappedTextureReference(Texture texture)
	{
		private int _released;

		public void Release()
		{
			if (Interlocked.Exchange(ref _released, 1) == 0)
			{
				Wgpu.TextureRelease(texture);
			}
		}
	}
}
