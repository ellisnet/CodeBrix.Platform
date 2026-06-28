using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Win32;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public class Win32HostBuilder : IPlatformHostBuilder
{
	private bool _preloadMediaPlayer;

	internal Win32HostBuilder()
	{
	}

	public Win32HostBuilder PreloadMediaPlayer(bool preload)
	{
		_preloadMediaPlayer = preload;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsWindows();

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new Win32Host(appBuilder, _preloadMediaPlayer);
}
