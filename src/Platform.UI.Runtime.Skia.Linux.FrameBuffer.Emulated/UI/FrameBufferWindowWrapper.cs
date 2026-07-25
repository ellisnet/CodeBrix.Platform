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

	public static void Init(DisplayOrientations orientation, bool isPreferredOrientation = false)
		=> _instance = new(orientation, isPreferredOrientation);

	public override object? NativeWindow => null;

	private readonly bool _isPreferredOrientation;
	private bool _orientationResolved;

	private FrameBufferWindowWrapper(DisplayOrientations orientation, bool isPreferredOrientation)
	{
		if (_instance != null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferWindowWrapper)} should be created once.");
		}
		_instance = this;

		Orientation = orientation;
		_isPreferredOrientation = isPreferredOrientation;
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
		if (!_isPreferredOrientation || Orientation == DisplayOrientations.None)
		{
			return;
		}
		var native = rawScreenSize.Width >= rawScreenSize.Height
			? DisplayOrientations.Landscape
			: DisplayOrientations.Portrait;
		Orientation = FromQuarterTurns(QuarterTurns(Orientation) - QuarterTurns(native));
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

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
