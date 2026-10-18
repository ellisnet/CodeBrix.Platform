using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using SkiaSharp;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// What the render engine tells its owner (the <see cref="WebGpuRenderView"/>, or a test double). Every callback runs
/// on the thread that caused it - the frame's thread, or the user-interface thread for <c>Detach</c> - with the
/// engine's frame gate held.
/// </summary>
internal interface IWebGpuRenderEngineHost
{
	/// <summary>A device was created: raise the renderer's and the event's DeviceReady.</summary>
	void OnDeviceReady();

	/// <summary>The device is gone (or never came): raise DeviceLost. Called BEFORE the device is released.</summary>
	/// <param name="reason">Why.</param>
	void OnDeviceLost(string reason);

	/// <summary>The render path changed.</summary>
	/// <param name="oldPath">The previous path.</param>
	/// <param name="newPath">The new path.</param>
	void OnRenderPathChanged(WebGpuRenderPath oldPath, WebGpuRenderPath newPath);

	/// <summary>A finished frame's picture: show it (ownership transfers).</summary>
	/// <param name="frame">The picture.</param>
	void Present(SKImage frame);

	/// <summary>
	/// A direct render path published a GPU-resident frame: show what <paramref name="source"/> holds from now on
	/// (until the next present or clear). Called on the frame's thread after every published frame.
	/// </summary>
	/// <param name="source">The direct path's frame source.</param>
	void PresentDirect(IWebGpuDirectFrameSource source);

	/// <summary>Drop the picture on screen - before the bitmaps behind it are released.</summary>
	void ClearPresented();
}

/// <summary>
/// The frame PROTOCOL and the lifetime of a view's WebGPU device and target, with no XAML in it: <c>BeginFrame</c> /
/// <c>EndFrame</c> on any one thread at a time, the lazy device, the render-path choice, the Readback presentation,
/// device-loss handling and the native-module hand-off. The view owns one and forwards to it; the tests drive one
/// directly.
/// </summary>
/// <remarks>
/// <para>
/// <b>The frame gate.</b> A frame holds a monitor from <c>BeginFrame</c> to <c>EndFrame</c>, both on the SAME thread.
/// Another thread's <c>BeginFrame</c> waits for it (the Manual driver and native modules); the built-in drivers only
/// TRY to take it and skip a tick when someone else holds it (latest wins). WebGPU requires one thread at a time on a
/// device, and this is that rule enforced.
/// </para>
/// <para>
/// <b>Lifetime</b> (the VideoPlayer driver's rules): the device is created LAZILY, on the first frame after
/// <see cref="Attach"/> (the view loaded with a live XamlRoot) - never in Loaded itself; it is torn down at
/// <see cref="Detach"/> (Unloaded, NOT disposal), after DeviceLost has told the renderer to release its resources; and
/// it is re-created by the first frame after the next Attach. A new pixel size is a new target at the next frame. A
/// device that cannot be created (no adapter, or a launch that says there is none) is reported once and not retried
/// until the next Attach. Errors the OWNED device reports through its uncaptured-error callback mean its state can no
/// longer be trusted: DeviceLost, one log line, and a fresh device on the next frame.
/// </para>
/// </remarks>
internal sealed unsafe class WebGpuRenderEngine : IWebGpuDeviceSource, IWebGpuTextureTarget
{
	/// <summary>How long Detach waits for a frame another thread holds before deferring the teardown to its EndFrame.</summary>
	private static readonly TimeSpan DetachWait = TimeSpan.FromSeconds(2);

	private readonly object _frameGate = new();
	private readonly Func<WebGpuSkiaDeviceOptions?, WebGpuSkiaDevice> _createDevice;
	private readonly IWebGpuRenderEngineHost _host;
	private readonly WebGpuHandoffBlock _handoff;

	private WebGpuDeviceGateDecision _gate = WebGpuDeviceGateDecision.Proceed;
	private Func<INativeGpuDeviceProvider?>? _providerProbe;
	private volatile bool _isLive;
	private long _pixelSize;        // width << 32 | height, written by the UI thread, read by the frame's thread
	private long _clearColor;       // premultiplied BGRA as SKColor bits, the same
	private WebGpuPresenter? _presenter;
	private volatile WebGpuRenderPath _path;
	private bool _unavailableLatched;
	private bool _teardownPending;
	private bool _nestedPassOpen;
	private int _frameThread;
	private long _frameIndex;
	private long _firstFrameTimestamp;
	private long _previousFrameTimestamp;
	private string? _lastTargetFailure;
	private bool _directNoticeLogged;
	private bool _directFailed;
	private long _attachTimestamp;
	private int _devicesLost;
	private bool _creationFailed;
	private bool _ownedNoticeLogged;
	private volatile WebGpuSkiaDeviceOptions? _deviceOptions;
	private volatile bool _requireOwnedDevice;
	private int _settingsChanged;   // DeviceSettingChange bits, set by the UI thread, taken by the frame's thread
	private volatile WebGpuDeviceInfo? _deviceInfo;

	[Flags]
	private enum DeviceSettingChange
	{
		None = 0,
		Options = 1,
		RequireOwnedDevice = 2,
	}

	/// <summary>Creates the engine. Nothing is created on the GPU until the first frame after <see cref="Attach"/>.</summary>
	/// <param name="host">Its owner.</param>
	/// <param name="createDevice">Creates the owned device from <see cref="DeviceOptions"/> (null:
	/// <see cref="WebGpuSkiaDevice.Create(WebGpuSkiaDeviceOptions)"/>, or <see cref="WebGpuSkiaDevice.Create()"/> for
	/// null options).</param>
	internal WebGpuRenderEngine(IWebGpuRenderEngineHost host, Func<WebGpuSkiaDeviceOptions?, WebGpuSkiaDevice>? createDevice = null)
	{
		_host = host ?? throw new ArgumentNullException(nameof(host));
		_createDevice = createDevice ?? CreateOwnedDevice;
		Frame = new WebGpuFrame(this);
		_handoff = new WebGpuHandoffBlock(this);
		ClearColor = SKColors.Black;
	}

	/// <summary>The one frame object, reused for every frame.</summary>
	internal WebGpuFrame Frame { get; }

	/// <summary>The render path running now.</summary>
	internal WebGpuRenderPath RenderPath => _path;

	/// <summary>Whether a device exists (between DeviceReady and DeviceLost).</summary>
	internal bool IsDeviceReady => Volatile.Read(ref _presenter) is not null;

	/// <summary>The running render path's presenter (null without a device). Tests.</summary>
	internal WebGpuPresenter? Presenter => Volatile.Read(ref _presenter);

	/// <summary>Whether the engine is attached (the view is loaded).</summary>
	internal bool IsLive => _isLive;

	/// <summary>The view hand-off (native memory, same address for the engine's lifetime).</summary>
	internal WebGpuRenderViewHandoff* ViewHandoff => _handoff.View;

	/// <summary>The device hand-off inside <see cref="ViewHandoff"/>.</summary>
	internal CodebrixDeviceHandoff* DeviceHandoff => _handoff.Device;

	/// <summary>The frame hand-off the current frame filled.</summary>
	internal CodebrixFrameHandoff* FrameHandoff => _handoff.Frame;

	/// <summary>The current device (null without one). Read on the frame's thread.</summary>
	internal WebGpuSkiaDevice? Device => _presenter?.Device;

	/// <summary>What the current device is (null without one). Any thread.</summary>
	internal WebGpuDeviceInfo? DeviceInfo => _deviceInfo;

	/// <summary>
	/// The options the view's OWN device is created with (null = the engine's defaults). Any thread. A change while the
	/// view owns a device replaces it at the next frame (DeviceLost "Device options changed.", then DeviceReady).
	/// </summary>
	internal WebGpuSkiaDeviceOptions? DeviceOptions
	{
		get => _deviceOptions;
		set
		{
			_deviceOptions = value;
			MarkChanged(DeviceSettingChange.Options);
		}
	}

	/// <summary>
	/// True: never adopt a head's device - always create the view's own (Readback), even with direct GPU rendering
	/// enabled. Any thread. A change while a device exists with the latch on replaces it at the next frame.
	/// </summary>
	internal bool RequireOwnedDevice
	{
		get => _requireOwnedDevice;
		set
		{
			if (_requireOwnedDevice == value)
			{
				return;
			}

			_requireOwnedDevice = value;
			MarkChanged(DeviceSettingChange.RequireOwnedDevice);
		}
	}

	private void MarkChanged(DeviceSettingChange change) => Interlocked.Or(ref _settingsChanged, (int)change);

	private static WebGpuSkiaDevice CreateOwnedDevice(WebGpuSkiaDeviceOptions? options) =>
		options is null ? WebGpuSkiaDevice.Create() : WebGpuSkiaDevice.Create(options);

	/// <summary>The colour every frame's texture is cleared to before the renderer draws (unpremultiplied).</summary>
	internal SKColor ClearColor
	{
		get => (SKColor)(uint)Interlocked.Read(ref _clearColor);
		set => Interlocked.Exchange(ref _clearColor, (uint)value);
	}

	/// <summary>The pixel size the next frame renders at; (0, 0) renders no frame. Any thread.</summary>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	internal void SetPixelSize(int width, int height) =>
		Interlocked.Exchange(ref _pixelSize, ((long)Math.Max(0, width) << 32) | (uint)Math.Max(0, height));

	/// <summary>The pixel size the next frame renders at.</summary>
	internal (int Width, int Height) PixelSize
	{
		get
		{
			var packed = Interlocked.Read(ref _pixelSize);
			return ((int)(packed >> 32), (int)(packed & 0xFFFFFFFF));
		}
	}

	/// <summary>
	/// The view entered a live visual tree (Loaded with a XamlRoot): remember what the head's gate said and how to find
	/// its native GPU device provider, and allow frames again. Creates nothing.
	/// </summary>
	/// <param name="gate">The owned-device gate's decision for this XamlRoot.</param>
	/// <param name="providerProbe">Returns the head's native GPU device provider for the window, or null; consulted only
	/// when direct GPU rendering is enabled, when a device is created.</param>
	internal void Attach(WebGpuDeviceGateDecision gate, Func<INativeGpuDeviceProvider?>? providerProbe)
	{
		lock (_frameGate)
		{
			_gate = gate;
			_providerProbe = providerProbe;
			_unavailableLatched = false;
			_creationFailed = false;
			_directFailed = false;
			_attachTimestamp = Stopwatch.GetTimestamp();
			_isLive = true;
		}
	}

	/// <summary>
	/// The view left the visual tree: no more frames, and the device and target are torn down - now, or by the
	/// EndFrame of a frame another thread still holds (after <see cref="DetachWait"/>).
	/// </summary>
	internal void Detach()
	{
		_isLive = false;

		if (Volatile.Read(ref _frameThread) == Environment.CurrentManagedThreadId)
		{
			// Unloaded raised from inside this thread's own frame (a Render handler that removed the view): the
			// frame's EndFrame tears down.
			_teardownPending = true;
			return;
		}

		if (!Monitor.TryEnter(_frameGate, DetachWait))
		{
			_teardownPending = true;
			return;
		}

		try
		{
			Teardown("The view was unloaded.", WebGpuRenderPath.None);
		}
		finally
		{
			Monitor.Exit(_frameGate);
		}
	}

	/// <summary>
	/// Opens a frame: <see cref="WebGpuFrameOutcome.Rendered"/> means it is open on this thread (and the frame gate is
	/// held until <see cref="EndFrame()"/>); anything else means no frame is open.
	/// </summary>
	/// <param name="wait">True to wait while another thread holds a frame (Manual, modules); false to give up at once
	/// (the built-in drivers).</param>
	/// <returns>What happened.</returns>
	/// <exception cref="InvalidOperationException">This thread already holds an open frame.</exception>
	internal WebGpuFrameOutcome OpenFrame(bool wait)
	{
		if (Volatile.Read(ref _frameThread) == Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("BeginFrame was called twice without EndFrame.");
		}

		if (wait)
		{
			Monitor.Enter(_frameGate);
		}
		else if (!Monitor.TryEnter(_frameGate))
		{
			return WebGpuFrameOutcome.NotReady;
		}

		var opened = false;
		try
		{
			var outcome = Open();
			opened = outcome == WebGpuFrameOutcome.Rendered;
			return outcome;
		}
		finally
		{
			if (!opened)
			{
				Monitor.Exit(_frameGate);
			}
		}
	}

	/// <summary>
	/// Ends the frame this thread opened: validates and submits its commands, reads the picture back and presents it,
	/// then releases the frame gate. Always synchronous: the frame is presented before this returns (the public
	/// EndFrame, Manual frames, native modules).
	/// </summary>
	/// <exception cref="InvalidOperationException">This thread holds no open frame.</exception>
	/// <exception cref="WebGpuSkiaValidationException">The frame's commands failed validation; nothing was submitted
	/// or presented (the previous picture stays).</exception>
	internal void EndFrame() => EndFrame(allowDeferredPresent: false);

	/// <summary>
	/// True when a built-in frame may leave its presentation to the next frame (<see cref="EndFrame(bool)"/>): only
	/// under the Continuous driver while it is not paused - the one driver that is sure to run another frame. The view
	/// asks this when the frame ENDS, after the Render handlers ran (a handler may have changed the driver).
	/// </summary>
	/// <param name="scheduler">The view's scheduler.</param>
	/// <returns>True when deferring is allowed.</returns>
	internal static bool MayDeferPresent(WebGpuFrameScheduler scheduler) =>
		scheduler.Driver == WebGpuFrameDriver.Continuous && !scheduler.IsPaused;

	/// <summary>
	/// Ends the frame this thread opened, as <see cref="EndFrame()"/> does - except that with
	/// <paramref name="allowDeferredPresent"/> the Readback path may finish the read-back of THIS frame during the next
	/// one and present the previous frame now (see <see cref="WebGpuReadbackPresenter"/>). Only the built-in Continuous
	/// driver passes true (<see cref="MayDeferPresent"/>); the frame left pending is completed by the next frame or by
	/// <see cref="FlushPendingFrame"/>.
	/// </summary>
	/// <param name="allowDeferredPresent">True to allow the deferred presentation.</param>
	/// <exception cref="InvalidOperationException">This thread holds no open frame.</exception>
	/// <exception cref="WebGpuSkiaValidationException">The frame's commands failed validation; nothing was submitted,
	/// begun or presented (the previous picture stays).</exception>
	internal void EndFrame(bool allowDeferredPresent)
	{
		if (!Frame.IsOpen || Volatile.Read(ref _frameThread) != Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("EndFrame was called without BeginFrame on this thread.");
		}

		ExceptionDispatchInfo? rejected = null;
		try
		{
			_nestedPassOpen = false;
			var presenter = _presenter!;
			try
			{
				presenter.EndPass(Frame.Encoder);
			}
			catch (WebGpuSkiaValidationException e)
			{
				rejected = ExceptionDispatchInfo.Capture(e);
			}

			if (rejected is null)
			{
				presenter.Show(_host, allowDeferredPresent);
			}

			if (presenter.TakeErrors() is { } errors)
			{
				LoseDevice("The WebGPU device reported an error and was replaced: " + errors);
			}
		}
		finally
		{
			CloseFrameObject();
			Volatile.Write(ref _frameThread, 0);
			if (_teardownPending)
			{
				_teardownPending = false;
				try
				{
					Teardown("The view was unloaded.", WebGpuRenderPath.None);
				}
				finally
				{
					Monitor.Exit(_frameGate);
				}
			}
			else
			{
				Monitor.Exit(_frameGate);
			}
		}

		rejected?.Throw();
	}

	/// <summary>
	/// Completes and presents the frame a deferred <see cref="EndFrame(bool)"/> left pending, if any: the view calls this
	/// when continuous ticking stops (the driver leaves Continuous, the view is paused, the view unloads), so that the
	/// last frame does not sit unpresented. Never blocks on a frame another thread holds.
	/// </summary>
	/// <returns>True when the flush ran (there may have been nothing to flush); false when a frame was open.</returns>
	internal bool FlushPendingFrame()
	{
		// Inside this thread's own open frame (a Render handler that paused the view or changed its driver): that
		// frame's EndFrame asks MayDeferPresent after the handler ran, so it presents synchronously and drops the older
		// pending frame itself.
		if (Volatile.Read(ref _frameThread) == Environment.CurrentManagedThreadId)
		{
			return false;
		}

		// Another thread holds an open frame (Manual, a native module): that frame's own EndFrame presents
		// synchronously and drops the older pending frame - so the user-interface thread never waits for it here.
		if (!Monitor.TryEnter(_frameGate))
		{
			return false;
		}

		try
		{
			if (_presenter is not { } presenter)
			{
				return true;
			}

			presenter.FlushPendingFrame(_host);
			if (presenter.TakeErrors() is { } errors)
			{
				LoseDevice("The WebGPU device reported an error and was replaced: " + errors);
			}

			return true;
		}
		finally
		{
			Monitor.Exit(_frameGate);
		}
	}

	// Frame gate held. Everything that has to happen before a frame can be recorded.
	private WebGpuFrameOutcome Open()
	{
		if (_teardownPending)
		{
			_teardownPending = false;
			Teardown("The view was unloaded.", WebGpuRenderPath.None);
		}

		if (!_isLive)
		{
			return WebGpuFrameOutcome.NotReady;
		}

		ApplyDeviceSettingChanges();

		if (_presenter?.FailureReason is { } failure)
		{
			// A direct path stopped working (its paint side could not wrap a frame, the head's device went away):
			// one line, DeviceLost, and the view continues on Readback from this very frame.
			FallBackToReadback(_presenter.Path, failure, _presenter.HeadDeviceGone);
		}

		if (_presenter is null)
		{
			var created = CreateDevice();
			if (created != WebGpuFrameOutcome.Rendered)
			{
				return created;
			}
		}

		var (width, height) = PixelSize;
		if (width == 0 || height == 0)
		{
			return WebGpuFrameOutcome.NotReady;
		}

		var presenter = _presenter!;
		if (!presenter.EnsureTarget(width, height, out var reason))
		{
			if (presenter.FailureReason is { } directFailure)
			{
				FallBackToReadback(presenter.Path, directFailure);
				return WebGpuFrameOutcome.NotReady;
			}

			// Logged once per distinct reason: a size the device refuses would otherwise log every frame.
			if (reason != _lastTargetFailure)
			{
				_lastTargetFailure = reason;
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"The WebGPU render target ({width}x{height}) could not be created: {reason}");
				}
			}

			return WebGpuFrameOutcome.NotReady;
		}

		_lastTargetFailure = null;
		var target = presenter.Target!;
		var encoder = target.BeginWebGpuPass();
		if (encoder.IsNull)
		{
			return WebGpuFrameOutcome.NotReady;
		}

		presenter.OnFrameOpened();
		RecordClear(encoder, target.TextureView, ClearColor);

		var now = Stopwatch.GetTimestamp();
		if (_frameIndex == 0)
		{
			_firstFrameTimestamp = now;
			_previousFrameTimestamp = now;
		}

		var frame = Frame;
		frame.Encoder = encoder;
		frame.Texture = target.Texture;
		frame.TextureView = target.TextureView;
		frame.Format = target.Format;
		frame.Width = target.Width;
		frame.Height = target.Height;
		frame.FrameIndex = _frameIndex++;
		frame.ElapsedSeconds = (now - _firstFrameTimestamp) / (double)Stopwatch.Frequency;
		frame.DeltaSeconds = (now - _previousFrameTimestamp) / (double)Stopwatch.Frequency;
		_previousFrameTimestamp = now;
		CodebrixFrameHandoff.Fill(target, encoder, _handoff.Frame);
		frame.IsOpen = true;
		Volatile.Write(ref _frameThread, Environment.CurrentManagedThreadId);
		return WebGpuFrameOutcome.Rendered;
	}

	// Frame gate held. DeviceOptions / RequireOwnedDevice changed since the last frame: replace the device when the
	// change can make a difference (the rules), and let a device creation that failed (possibly because of the old
	// options) be tried again. A launch that said "no device" stays latched.
	private void ApplyDeviceSettingChanges()
	{
		var change = (DeviceSettingChange)Interlocked.Exchange(ref _settingsChanged, 0);
		if (change == DeviceSettingChange.None)
		{
			return;
		}

		if (_unavailableLatched && _creationFailed)
		{
			_unavailableLatched = false;
			_creationFailed = false;
		}

		if (_presenter is null)
		{
			return;
		}

		var recreate =
			((change & DeviceSettingChange.Options) != 0 && WebGpuRenderViewRules.DeviceOptionsChangeRecreatesDevice(_path))
			|| ((change & DeviceSettingChange.RequireOwnedDevice) != 0
				&& WebGpuRenderViewRules.RequireOwnedDeviceChangeRecreatesDevice(DirectGpuRenderingMode.IsEnabled, _path));
		if (!recreate)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info("The WebGPU device is replaced: " + WebGpuRenderViewRules.DeviceOptionsChangedReason);
		}

		// RequireOwnedDevice turned off asks for the direct path again; a direct path that FAILED stays failed until the
		// next Loaded otherwise.
		if ((change & DeviceSettingChange.RequireOwnedDevice) != 0)
		{
			_directFailed = false;
		}

		Teardown(WebGpuRenderViewRules.DeviceOptionsChangedReason, WebGpuRenderPath.None);
	}

	// Frame gate held. The lazy device: the gate first, then the device, then the path.
	private WebGpuFrameOutcome CreateDevice()
	{
		if (_unavailableLatched)
		{
			return WebGpuFrameOutcome.Unavailable;
		}

		if (!_gate.MayCreate)
		{
			_unavailableLatched = true;
			var reason = _gate.Reason ?? "The head reported that no WebGPU device is available.";
			if (this.Log().IsEnabled(_gate.Failed ? LogLevel.Warning : LogLevel.Information))
			{
				var line = "No WebGPU device for this view: " + reason;
				if (_gate.Failed)
				{
					this.Log().Warn(line);
				}
				else
				{
					this.Log().Info(line);
				}
			}

			_host.OnDeviceLost(reason);
			return WebGpuFrameOutcome.Unavailable;
		}

		var direct = TryCreateDirect(out var waitForProvider);
		if (waitForProvider)
		{
			return WebGpuFrameOutcome.NotReady;
		}

		if (direct is not null)
		{
			return Start(direct);
		}

		WebGpuSkiaDevice device;
		try
		{
			device = _createDevice(_deviceOptions);
		}
		catch (Exception e)
		{
			_unavailableLatched = true;
			_creationFailed = true;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("The WebGPU device could not be created: " + e.Message, e);
			}

			_host.OnDeviceLost("The WebGPU device could not be created: " + e.Message);
			return WebGpuFrameOutcome.Unavailable;
		}

		if (!device.IsAvailable)
		{
			var reason = device.Reason ?? "No WebGPU device could be created.";
			device.Dispose();
			_unavailableLatched = true;
			_creationFailed = true;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("No WebGPU device for this view: " + reason);
			}

			_host.OnDeviceLost(reason);
			return WebGpuFrameOutcome.Unavailable;
		}

		return Start(new WebGpuReadbackPresenter(device));
	}

	// Frame gate held. A device exists: install its presenter, then DeviceReady (inside the presenter's error capture:
	// an adopted device has no handler of ours, so what a renderer creates there must not end the process).
	private WebGpuFrameOutcome Start(WebGpuPresenter presenter)
	{
		var device = presenter.Device;
		var path = presenter.Path;
		Volatile.Write(ref _presenter, presenter);
		_frameIndex = 0;
		CodebrixDeviceHandoff.Fill(this, _handoff.Device);
		_deviceInfo = WebGpuDeviceInfo.Query(device.Adapter, device.Device, ((IWebGpuDeviceSource)device).Backend);

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"WebGPU device ready: {device.AdapterName} ({device.Backend}), render path {path}.");
		}

		SetPath(path);
		string? deviceWorkError;
		presenter.BeginDeviceWork();
		try
		{
			_host.OnDeviceReady();
		}
		finally
		{
			deviceWorkError = presenter.EndDeviceWork();
		}

		if (deviceWorkError is not null)
		{
			LoseDevice("The WebGPU device reported an error and was replaced: " + deviceWorkError);
			return WebGpuFrameOutcome.NotReady;
		}

		return WebGpuFrameOutcome.Rendered;
	}

	// Plan 2.5/2.6: the latch is read here. With it on, the head's native GPU device provider is resolved (afresh for
	// every device: the Wayland head's appears only with its first frame, so an announced-but-not-created device is
	// waited for, briefly) and, when it is eligible, adopted by the direct adapter for its kind. Every way this can
	// fail ends in ONE log line and null - the caller then creates the view's own device: Readback.
	private WebGpuDirectPresenter? TryCreateDirect(out bool waitForProvider)
	{
		waitForProvider = false;
		if (!DirectGpuRenderingMode.IsEnabled || _providerProbe is null || _directFailed)
		{
			return null;
		}

		if (!WebGpuRenderViewRules.MayAdoptHeadDevice(DirectGpuRenderingMode.IsEnabled, _requireOwnedDevice))
		{
			if (!_ownedNoticeLogged && this.Log().IsEnabled(LogLevel.Information))
			{
				_ownedNoticeLogged = true;
				this.Log().Info(WebGpuRenderViewRules.RequireOwnedDeviceMessage);
			}

			return null;
		}

		INativeGpuDeviceProvider? provider;
		try
		{
			provider = _providerProbe();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("The head's native GPU device provider could not be resolved: " + e.Message);
			}

			return null;
		}

		if (provider is null)
		{
			return null;
		}

		var kind = provider.Kind;
		var available = provider.IsAvailable;
		var waited = Stopwatch.GetElapsedTime(_attachTimestamp).TotalMilliseconds;
		if (WebGpuRenderViewRules.ShouldWaitForProvider(true, kind, available, waited))
		{
			waitForProvider = true;
			return null;
		}

		var eligible = WebGpuRenderViewRules.IsDirectPathEligible(true, kind, available);
		var adapterExists = WebGpuRenderViewRules.DirectAdapterExists(kind, OperatingSystem.IsMacOS());
		var path = WebGpuRenderViewRules.ChooseRenderPath(eligible, kind, adapterExists);
		if (path == WebGpuRenderPath.Readback)
		{
			if (!_directNoticeLogged && this.Log().IsEnabled(LogLevel.Information))
			{
				_directNoticeLogged = true;
				this.Log().Info(!eligible
					? WebGpuRenderViewRules.DirectPathFallbackMessage(WebGpuRenderViewRules.DirectPathFor(kind),
						provider.Reason ?? "the head's GPU device is not available")
					: WebGpuRenderViewRules.DirectPathNotImplementedMessage(kind));
			}

			_directFailed = true;
			return null;
		}

		var presenter = WebGpuDirectPresenter.TryCreate(provider, out var reason);
		if (presenter is null)
		{
			_directFailed = true;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn(WebGpuRenderViewRules.DirectPathFallbackMessage(path, reason ?? "the head's device could not be adopted"));
			}
		}

		return presenter;
	}

	// Frame gate held. The direct path that was running cannot go on: the one line, DeviceLost (the renderer lets go of
	// what it made on the adopted device), and the next device is the view's own.
	private void FallBackToReadback(WebGpuRenderPath path, string reason, bool headDeviceGone = false)
	{
		_directFailed = true;

		// The head's device going away (its window closing) is not a fault: Information, not Warning.
		var level = headDeviceGone ? LogLevel.Information : LogLevel.Warning;
		if (this.Log().IsEnabled(level))
		{
			if (headDeviceGone)
			{
				this.Log().Info(WebGpuRenderViewRules.DirectPathFallbackMessage(path, reason));
			}
			else
			{
				this.Log().Warn(WebGpuRenderViewRules.DirectPathFallbackMessage(path, reason));
			}
		}

		Teardown(WebGpuRenderViewRules.DirectPathFallbackMessage(path, reason), WebGpuRenderPath.Readback);
	}

	// Frame gate held.
	private void LoseDevice(string reason)
	{
		_devicesLost++;
		if (this.Log().IsEnabled(_devicesLost == 1 ? LogLevel.Warning : LogLevel.Debug))
		{
			if (_devicesLost == 1)
			{
				this.Log().Warn(reason);
			}
			else
			{
				this.Log().Debug(reason);
			}
		}

		Teardown(reason, WebGpuRenderPath.None, raiseLost: true);
	}

	// Frame gate held. DeviceLost first (the renderer releases its resources while the device still exists), then the
	// picture (no wrapper may outlive the bitmaps), then the target and the device.
	private void Teardown(string reason, WebGpuRenderPath newPath, bool raiseLost = true)
	{
		var presenter = _presenter;
		if (presenter is null)
		{
			SetPath(newPath);
			return;
		}

		if (raiseLost)
		{
			try
			{
				_host.OnDeviceLost(reason);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("A DeviceLost handler threw: " + e.Message, e);
				}
			}
		}

		_host.ClearPresented();
		Volatile.Write(ref _presenter, null);
		_deviceInfo = null;
		presenter.Dispose();
		CodebrixDeviceHandoff.Fill(this, _handoff.Device);
		SetPath(newPath);
	}

	private void SetPath(WebGpuRenderPath path)
	{
		var old = _path;
		if (old == path)
		{
			return;
		}

		_path = path;
		_host.OnRenderPathChanged(old, path);
	}

	private void CloseFrameObject()
	{
		var frame = Frame;
		frame.IsOpen = false;
		frame.Encoder = CommandEncoder.Null;
		frame.Texture = Texture.Null;
		frame.TextureView = TextureView.Null;
	}

	// Every frame starts cleared: one render pass with LoadOp.Clear, ended at once. Premultiplied, like the texture.
	private static void RecordClear(CommandEncoder encoder, TextureView view, SKColor color)
	{
		var alpha = color.Alpha / 255.0;
		var attachment = new RenderPassColorAttachment
		{
			View = view,
			DepthSlice = Constants.DepthSliceUndefined,
			LoadOp = LoadOp.Clear,
			StoreOp = StoreOp.Store,
			ClearValue = new Color
			{
				R = color.Red / 255.0 * alpha,
				G = color.Green / 255.0 * alpha,
				B = color.Blue / 255.0 * alpha,
				A = alpha,
			},
		};
		var descriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
		var pass = Wgpu.CommandEncoderBeginRenderPass(encoder, &descriptor);
		Wgpu.RenderPassEncoderEnd(pass);
		Wgpu.RenderPassEncoderRelease(pass);
	}

	// ------------------------------------------------------------------ IWebGpuDeviceSource

	/// <inheritdoc/>
	public Instance Instance => _presenter?.Device.Instance ?? Instance.Null;

	/// <inheritdoc/>
	public Adapter Adapter => _presenter?.Device.Adapter ?? Adapter.Null;

	/// <inheritdoc/>
	Device IWebGpuDeviceSource.Device => _presenter?.Device.Device ?? CodeBrix.WebGpu.Native.Device.Null;

	/// <inheritdoc/>
	public Queue Queue => _presenter?.Device.Queue ?? Queue.Null;

	/// <inheritdoc/>
	public BackendType Backend => _presenter?.Device is { } device ? ((IWebGpuDeviceSource)device).Backend : BackendType.Undefined;

	/// <inheritdoc/>
	public bool HasUncapturedErrorHandler => _presenter?.Device is { } device && ((IWebGpuDeviceSource)device).HasUncapturedErrorHandler;

	// ------------------------------------------------------------------ IWebGpuTextureTarget

	private WebGpuSkiaTarget? OpenTarget => Frame.IsOpen ? _presenter?.Target : null;

	/// <inheritdoc/>
	public Texture Texture => OpenTarget?.Texture ?? Texture.Null;

	/// <inheritdoc/>
	public TextureView TextureView => OpenTarget?.TextureView ?? TextureView.Null;

	/// <inheritdoc/>
	public TextureFormat Format => WebGpuPresenter.Format;

	/// <inheritdoc/>
	public int Width => OpenTarget?.Width ?? 0;

	/// <inheritdoc/>
	public int Height => OpenTarget?.Height ?? 0;

	/// <inheritdoc/>
	public int SampleCount => 1;

	/// <inheritdoc/>
	public TextureUsage Usage => OpenTarget?.Usage ?? TextureUsage.None;

	/// <inheritdoc/>
	public bool IsInWebGpuPass => _nestedPassOpen;

	/// <summary>
	/// Hands out the OPEN frame's encoder to a library that drives the pass protocol itself (a graphics layer's
	/// "frame on a shared target"). A null encoder outside a frame: open one first (or do the work in Render).
	/// </summary>
	/// <returns>The frame's encoder, or a null encoder when this thread has no open frame.</returns>
	/// <exception cref="InvalidOperationException">The pass was already handed out in this frame.</exception>
	public CommandEncoder BeginWebGpuPass()
	{
		if (!Frame.IsOpen || Volatile.Read(ref _frameThread) != Environment.CurrentManagedThreadId)
		{
			return CommandEncoder.Null;
		}

		if (_nestedPassOpen)
		{
			throw new InvalidOperationException("BeginWebGpuPass was called twice without EndWebGpuPass.");
		}

		_nestedPassOpen = true;
		return Frame.Encoder;
	}

	/// <summary>
	/// Closes the pass <see cref="BeginWebGpuPass"/> handed out. The commands are validated and submitted when the
	/// FRAME ends (<c>EndFrame</c>), not here.
	/// </summary>
	/// <exception cref="InvalidOperationException">No pass was handed out.</exception>
	public void EndWebGpuPass()
	{
		if (!_nestedPassOpen)
		{
			throw new InvalidOperationException("EndWebGpuPass was called without BeginWebGpuPass.");
		}

		_nestedPassOpen = false;
	}
}
