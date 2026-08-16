using System;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics;
using Windows.UI.Core;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Runtime.Skia;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI; //Was previously: Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static FrameBufferWindowWrapper? _instance;
	internal static FrameBufferWindowWrapper Instance => _instance!;

	public static void Init(DisplayOrientations orientation, bool isPreferredOrientation = false,
		DisplayOrientations? autoRotationOrientations = null, bool autoRotationDisabled = false)
		=> _instance = new(orientation, isPreferredOrientation, autoRotationOrientations, autoRotationDisabled);

	public override object? NativeWindow => null;

	private readonly bool _isPreferredOrientation;
	private readonly DisplayOrientations? _autoRotationOrientations;
	private readonly bool _autoRotationDisabled;
	private bool _orientationResolved;
	private Size _panelSize;

	/// <summary>
	/// Raised when the device has been turned to an orientation the application
	/// honors, after the application's bounds have been updated for it.
	/// </summary>
	internal event Action? DeviceOrientationChanged;

	/// <summary>
	/// Raised BEFORE a portrait &lt;-&gt; landscape turn the application honors is
	/// applied, while the old bounds are still in place. The software keyboard
	/// subscribes to get out of the way: a keyboard strip — locked or not —
	/// cannot survive the aspect swap (its occlusion inset and popup metrics
	/// would re-derive against a mid-change size and corrupt the screen), so it
	/// unlocks, hides and releases the focused text control first. 180° turns
	/// keep the panel's proportions and are not announced.
	/// </summary>
	internal event Action? AspectSwappingOrientationChange;

	private FrameBufferWindowWrapper(DisplayOrientations orientation, bool isPreferredOrientation,
		DisplayOrientations? autoRotationOrientations, bool autoRotationDisabled)
	{
		if (_instance != null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferWindowWrapper)} should be created once.");
		}
		_instance = this;

		Orientation = orientation;
		_isPreferredOrientation = isPreferredOrientation;
		_autoRotationOrientations = autoRotationOrientations;
		_autoRotationDisabled = autoRotationDisabled;
	}

	/// <summary>
	/// The panel's own orientation — Landscape when its scanout is at least as wide
	/// as it is tall, Portrait otherwise. Fixed for the life of the process, and
	/// known only once <see cref="SetSize"/> has supplied the geometry.
	/// </summary>
	internal DisplayOrientations NativeOrientation { get; private set; } = DisplayOrientations.Landscape;

	/// <summary>
	/// The orientation the application currently IS: the device orientation its UI
	/// is right-side-up for. Distinct from <see cref="Orientation"/>, which is the
	/// rotation applied to the canvas to get it there.
	/// </summary>
	internal DisplayOrientations CurrentOrientation
		=> FromQuarterTurns(QuarterTurns(Orientation) + QuarterTurns(NativeOrientation));

	/// <summary>
	/// The device has been turned to <paramref name="deviceOrientation"/>. Honored
	/// only when the application accepts that orientation
	/// (<see cref="DisplayInformation.AutoRotationPreferences"/>); otherwise NOTHING
	/// changes and the UI stays as it was, appearing sideways or upside-down exactly
	/// like a locked application on a turned device. Returns whether it was honored.
	/// </summary>
	internal bool SetDeviceOrientation(DisplayOrientations deviceOrientation)
	{
		// Defensive only: the renderer hands over the panel size before the transport's
		// input loop starts, so an orientation can never arrive before this is resolved.
		if (!_orientationResolved)
		{
			return false;
		}
		// None is "no preference stated", which allows everything — NOT "nothing".
		var preferences = DisplayInformation.AutoRotationPreferences;
		if (preferences != DisplayOrientations.None && !preferences.HasFlag(deviceOrientation))
		{
			return false;
		}
		var rotation = FromQuarterTurns(
			QuarterTurns(deviceOrientation) - QuarterTurns(NativeOrientation));
		if (rotation == Orientation)
		{
			return false;
		}
		if (IsPortraitOrientation(CurrentOrientation) != IsPortraitOrientation(deviceOrientation))
		{
			AspectSwappingOrientationChange?.Invoke();
		}
		Orientation = rotation;
		SetSize(_panelSize);
		DeviceOrientationChanged?.Invoke();
		return true;
	}

	/// <summary>
	/// The rotation applied to the application's canvas relative to the panel's
	/// scanout. When the host builder stated a PREFERRED orientation instead, this is
	/// the rotation that gets the application there on this particular panel, worked
	/// out from the panel's native geometry the first time <see cref="SetSize"/>
	/// supplies it — under the emulator, the resolution the IDE was configured with.
	/// </summary>
	public DisplayOrientations Orientation { get; private set; }

	internal void SetSize(Size rawScreenSize)
	{
		// Before anything can read Orientation: the preferred-orientation path needs
		// the panel's native geometry, and this is the first moment it is known — the
		// host builder's value is read long before the emulator's frame buffer size.
		ResolveOrientation(rawScreenSize);

		if (XamlRoot is { })
		{
			var scale = RasterizationScale = (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel;
			if (Orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				(rawScreenSize.Height, rawScreenSize.Width) = (rawScreenSize.Width, rawScreenSize.Height);
			}
			var bounds = new Rect(0, 0, rawScreenSize.Width / scale, rawScreenSize.Height / scale);
			SetBoundsAndVisibleBounds(bounds, bounds);
			var fullSize = new SizeInt32((int)rawScreenSize.Width, (int)rawScreenSize.Height);
			SetSizes(fullSize, fullSize);
			// No mouse position to seed: the emulated device is touch-only.
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => SetSize(rawScreenSize));
		}
	}

	// Turns "the orientation the application wants to be" into the rotation that gets
	// it there on THIS panel. The four orientations are a quarter-turn cycle, so the
	// rotation is simply the distance around that cycle from the panel's native
	// orientation to the requested one. That distance is zero whenever the panel is
	// landscape-native, which is what makes this path identical to the legacy one on
	// every landscape-native panel — it can only differ on a portrait-native one.
	// Resolved once: the panel never changes shape, and a square panel counts as
	// landscape, so the answer could never be re-derived later anyway.
	private void ResolveOrientation(Size rawScreenSize)
	{
		if (_orientationResolved)
		{
			return;
		}
		_orientationResolved = true;
		_panelSize = rawScreenSize;
		NativeOrientation = rawScreenSize.Width >= rawScreenSize.Height
			? DisplayOrientations.Landscape
			: DisplayOrientations.Portrait;
		if (_isPreferredOrientation && Orientation != DisplayOrientations.None)
		{
			Orientation = FromQuarterTurns(
				QuarterTurns(Orientation) - QuarterTurns(NativeOrientation));
		}
		SeedAutoRotationPreferences();
	}

	// Hands the host builder's AutoRotationEnabled setting to the property that is
	// the single source of truth for it. Done here rather than at Init because
	// "never rotate" can only be expressed as the orientation the application ends
	// up in, which is not knowable until the panel is. An application that assigns
	// AutoRotationPreferences itself at run time simply overwrites this.
	private void SeedAutoRotationPreferences()
	{
		if (_autoRotationDisabled)
		{
			DisplayInformation.AutoRotationPreferences = CurrentOrientation;
		}
		else if (_autoRotationOrientations is { } orientations)
		{
			DisplayInformation.AutoRotationPreferences = orientations;
		}
	}

	// Position in the quarter-turn cycle: Landscape, Portrait, LandscapeFlipped and
	// PortraitFlipped are 0, +90, 180 and -90 degrees of canvas rotation.
	private static int QuarterTurns(DisplayOrientations orientation) => orientation switch
	{
		DisplayOrientations.Portrait => 1,
		DisplayOrientations.LandscapeFlipped => 2,
		DisplayOrientations.PortraitFlipped => 3,
		_ => 0,
	};

	private static DisplayOrientations FromQuarterTurns(int turns) => (((turns % 4) + 4) % 4) switch
	{
		1 => DisplayOrientations.Portrait,
		2 => DisplayOrientations.LandscapeFlipped,
		3 => DisplayOrientations.PortraitFlipped,
		_ => DisplayOrientations.Landscape,
	};

	private static bool IsPortraitOrientation(DisplayOrientations orientation)
		=> orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped;

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
