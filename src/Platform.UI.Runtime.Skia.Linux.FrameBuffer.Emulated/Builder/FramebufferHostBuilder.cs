using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.Graphics.Display;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

public class FramebufferHostBuilder : IPlatformHostBuilder
{
	internal FramebufferHostBuilder()
	{
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsLinux();

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new FrameBufferHost(appBuilder, this);

	/// <summary>
	/// Shows the mouse cursor as a small circle. If this method is not called,
	/// then by default, the cursor will be shown only after the first mouse
	/// event received from libinput and will not be shown if only touch events
	/// are received. This behavior is useful if you're using a touch screen
	/// and don't need to see a cursor.
	/// </summary>
	public FramebufferHostBuilder EnableMouseCursor(float radius, Color color)
	{
		MouseCursorRadius = radius;
		MouseCursorColor = color;
		ShowMouseCursor = true;
		return this;
	}

	/// <summary>
	/// Hides the mouse cursor. If this method is not called,
	/// then by default, the cursor will be shown only after the first mouse
	/// event received from libinput and will not be shown if only touch events
	/// are received. This behavior is useful if you're using a touch screen
	/// and don't need to see a cursor.
	/// </summary>
	public FramebufferHostBuilder DisableMouseCursor()
	{
		ShowMouseCursor = false;
		return this;
	}

	/// <summary>
	/// How the application is oriented on the panel.
	/// <para>
	/// By DEFAULT (<paramref name="isPreferredOrientation"/> false — the legacy path)
	/// the value is a ROTATION APPLIED to the application's canvas relative to the
	/// panel's scanout: <see cref="DisplayOrientations.Landscape"/> is no rotation,
	/// <see cref="DisplayOrientations.Portrait"/> a quarter turn, and so on. On a
	/// landscape-native panel that reads naturally. On a PORTRAIT-native panel it does
	/// not: Landscape leaves the application portrait, because Landscape names zero
	/// rotation rather than "make me landscape".
	/// </para>
	/// <para>
	/// Pass <paramref name="isPreferredOrientation"/> true to state instead which
	/// orientation the APPLICATION WANTS TO BE, and let the rotation be worked out from
	/// the panel's native geometry. On a landscape-native panel this is identical to the
	/// default for every value; it differs only on a portrait-native panel, where
	/// Landscape then yields a landscape application rendered sideways.
	/// </para>
	/// <para>
	/// <see cref="DisplayOrientations.None"/> means "whatever the panel is natively" —
	/// no rotation — and is unaffected by the flag, so it behaves the same as not
	/// calling this method at all.
	/// </para>
	/// <para>
	/// Under the emulator the panel is whatever resolution and orientation the
	/// CodeBrix.Develop user chose, so a portrait-native panel is simply File &gt;
	/// Options &gt; Orientation &gt; Portrait.
	/// </para>
	/// </summary>
	/// <param name="orientation">The rotation to apply, or the orientation the
	/// application wants to be when <paramref name="isPreferredOrientation"/> is true.</param>
	/// <param name="isPreferredOrientation">Whether <paramref name="orientation"/> states
	/// the orientation the application wants to be, rather than a rotation to apply.</param>
	public FramebufferHostBuilder Orientation(DisplayOrientations orientation,
		bool isPreferredOrientation = false)
	{
		DisplayOrientation = orientation;
		IsPreferredOrientation = isPreferredOrientation;
		return this;
	}

	/// <summary>
	/// The DEVICE orientations the application honors while it is running: turn the
	/// device to one of these and the application re-lays-out so its UI is right-side-up
	/// for it. Turn it to any other and NOTHING changes — the application stays as it
	/// was, appearing sideways or upside-down, exactly like a locked application on a
	/// turned device.
	/// <para>
	/// Sugar over <see cref="DisplayInformation.AutoRotationPreferences"/>, which this
	/// assigns and which remains the single source of truth: an application may set that
	/// property directly instead, or change it later at run time (per page, say), and the
	/// later assignment simply wins.
	/// </para>
	/// <para>
	/// This governs RUNNING rotation only. Which orientation the application STARTS in is
	/// <see cref="Orientation"/>'s business, so an application may legitimately start
	/// outside this list — it then rotates out of that orientation and never back.
	/// </para>
	/// </summary>
	/// <param name="orientations">The device orientations to honor. Passing none is the
	/// same as <c>AutoRotationEnabled(false)</c>.</param>
	public FramebufferHostBuilder AutoRotationEnabled(params DisplayOrientations[] orientations)
	{
		AutoRotationOrientations = DisplayOrientations.None;
		foreach (var orientation in orientations ?? [])
		{
			AutoRotationOrientations |= orientation;
		}
		AutoRotationDisabled = AutoRotationOrientations == DisplayOrientations.None;
		return this;
	}

	/// <summary>
	/// Whether the application honors device rotation at all: false locks it to whatever
	/// orientation it started in, true honors all four. See the overload taking a list
	/// for the detail — this is the same setting, spelled for the two extremes.
	/// </summary>
	/// <param name="enabled">Whether to honor device rotation.</param>
	public FramebufferHostBuilder AutoRotationEnabled(bool enabled)
	{
		AutoRotationDisabled = !enabled;
		AutoRotationOrientations = enabled
			? DisplayOrientations.Landscape | DisplayOrientations.Portrait
				| DisplayOrientations.LandscapeFlipped | DisplayOrientations.PortraitFlipped
			: DisplayOrientations.None;
		return this;
	}

	/// <summary>
	/// Determines if OpenGLES+EGL initialized with DRM+GBM should be used for hardware-accelerated rendering on the
	/// Linux Framebuffer target instead of software rendering. If not called, we try to create an OpenGLES context if possible.
	/// Otherwise, software rendering will be used.
	/// </summary>
	/// <param name="cardPath">The path to the DRM device file. If null, the first device found of the form /dev/dri/cardX will be used.</param>
	/// <param name="connectorChooser">A delegate that picks which of the available connectors to use. If not supplied, the first one found will be used.</param>
	/// <param name="gbmSurfaceColorFormat">
	/// The FourCC color format used for the GBM surface created for rendering
	/// (this is passed to gbm_surface_create). For more details on the FourCC
	/// format and valid values, see https://github.com/torvalds/linux/blob/master/include/uapi/drm/drm_fourcc.h
	/// </param>
	public FramebufferHostBuilder UseKMSDRM(string? cardPath = null, DRMFourCCColorFormat? gbmSurfaceColorFormat = null, DRMConnectorChooserDelegate? connectorChooser = null)
	{
		UseDRM = true;
		DRMCardPath = cardPath;
		GBMSurfaceColorFormat = gbmSurfaceColorFormat ?? DRMFourCCColorFormat.Argb8888;
		DRMConnectorChooser = connectorChooser;
		return this;
	}

	/// <summary>
	/// Disables the usage of KMS/DRM for hardware acceleration and forces software rendering. 
	/// </summary>
	public FramebufferHostBuilder DisableKMSDRM()
	{
		UseDRM = false;
		return this;
	}

	/// <summary>
	/// Sets the RMLVO parameters to be passed to libxkbcommon's xkb_rule_names for keyboard keymap creation. If unset,
	/// the system default is used.
	/// For more details on RMLVO, see https://xkbcommon.org/doc/current/xkb-intro.html#RMLVO-intro
	/// and https://github.com/xkbcommon/libxkbcommon/blob/99e9b0fc558fb838a04c568bea033c52ffbe704b/include/xkbcommon/xkbcommon.h#L468
	/// </summary>
	public FramebufferHostBuilder XkbKeymap(XKBKeymapParams keymapParams)
	{
		KeymapParams = keymapParams;
		return this;
	}

	internal XKBKeymapParams KeymapParams { get; private set; }

	internal bool? ShowMouseCursor { get; private set; }

	internal Color MouseCursorColor { get; private set; } = Color.FromArgb(255, 0, 0, 0);

	internal float MouseCursorRadius { get; private set; } = 5;

	internal DisplayOrientations DisplayOrientation { get; private set; } = DisplayOrientations.Landscape;

	internal bool IsPreferredOrientation { get; private set; }

	// null until AutoRotationEnabled is called: the application's own
	// AutoRotationPreferences assignment, if any, is then left entirely alone.
	internal DisplayOrientations? AutoRotationOrientations { get; private set; }

	// "Never rotate" cannot be spelled as a preferences value — None means NO
	// PREFERENCE STATED there, which allows everything — so it is carried
	// separately and resolved to the startup orientation once the panel is known.
	internal bool AutoRotationDisabled { get; private set; }

	internal bool? UseDRM { get; private set; }

	internal string? DRMCardPath { get; private set; }

	internal DRMFourCCColorFormat GBMSurfaceColorFormat { get; private set; } = DRMFourCCColorFormat.Argb8888;

	internal DRMConnectorChooserDelegate? DRMConnectorChooser { get; private set; }

	public readonly record struct DRMFourCCColorFormat(char C1, char C2, char C3, char C4)
	{
		internal uint ToInt() => (uint)C1 | (uint)C2 << 8 | (uint)C3 << 16 | (uint)C4 << 24;

		internal static DRMFourCCColorFormat Argb8888 { get; } = new('A', 'R', '2', '4');
	}

	public readonly record struct DRMConnector(uint connectorType, uint connectorTypeId, uint connectorId, string connectorStringRepresentation);

	/// <returns>The index of the chosen connector or -1.</returns>
	public delegate int DRMConnectorChooserDelegate(IReadOnlyList<DRMConnector> connector);

	public readonly record struct XKBKeymapParams(string? model = null, string? rules = null, string? layout = null, string? variant = null, string? options = null);
}
