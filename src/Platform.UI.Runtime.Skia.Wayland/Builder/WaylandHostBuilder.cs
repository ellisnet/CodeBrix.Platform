using System;
using System.IO;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public partial class WaylandHostBuilder : IPlatformHostBuilder
{
	private int _renderFrameRate = 60;

	internal WaylandHostBuilder()
	{
	}

	/// <summary>
	/// Sets the FPS that the application should try to achieve.
	/// </summary>
	public WaylandHostBuilder RenderFrameRate(int renderFrameRate)
	{
		_renderFrameRate = renderFrameRate;
		return this;
	}

	// Deliberately NOT an environment sniff: the AUTHORITATIVE Wayland check is the
	// wl_display_connect result at startup (WaylandApplicationHost.RunLoop). Env-based
	// gating here would make CodeBrixPlatformHostBuilder.Build() die with an opaque
	// "No platform host could be selected" instead of the clean, on-brand
	// "This application requires a Wayland compositor." fail-fast (plan decision 2.(6)).
	bool IPlatformHostBuilder.IsSupported => OperatingSystem.IsLinux();

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new WaylandApplicationHost(appBuilder, _renderFrameRate);
}
