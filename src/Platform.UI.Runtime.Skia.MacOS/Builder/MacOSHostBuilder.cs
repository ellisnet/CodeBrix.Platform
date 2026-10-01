using Microsoft.UI.Xaml;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.MacOS;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

public class MacOSHostBuilder : IPlatformHostBuilder
{
	private bool _useSystemMenuBar;
	private string? _systemAppName;

	/// <summary>Sets the macOS application display name at startup, independently
	/// of menu-bar projection. Does not rename the executable or assembly.</summary>
	/// <exception cref="ArgumentException">The name is empty, whitespace, or contains a null character.</exception>
	/// <exception cref="ArgumentNullException">The name is null.</exception>
	public MacOSHostBuilder UseSystemAppName(string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		if (name.Contains('\0')) { throw new ArgumentException("The application name cannot contain a null character.", nameof(name)); }
		_systemAppName = name;
		return this;
	}

	/// <summary>Projects the window's MenuBar into the macOS system menu bar.
	/// The default is an in-window menu bar. Other hosts are unaffected.</summary>
	public MacOSHostBuilder UseSystemMenuBar(bool enabled = true)
	{
		_useSystemMenuBar = enabled;
		return this;
	}
	public MacOSHostBuilder()
	{
	}

	public bool IsSupported
		=> OperatingSystem.IsMacOS();

	public SkiaHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new MacSkiaHost(appBuilder) { UseSystemMenuBar = _useSystemMenuBar, SystemAppName = _systemAppName };

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) => Create(appBuilder, appType);
}
