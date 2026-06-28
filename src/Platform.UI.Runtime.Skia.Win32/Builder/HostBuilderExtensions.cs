using System;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilderExtensions
{
	public static ICodeBrixPlatformHostBuilder UseWindowsWin32(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new Win32HostBuilder());
		return builder;
	}

	public static ICodeBrixPlatformHostBuilder UseWindowsWin32(this ICodeBrixPlatformHostBuilder builder, Action<Win32HostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var win32Builder = new Win32HostBuilder();
			if (((IPlatformHostBuilder)win32Builder).IsSupported)
			{
				action.Invoke(win32Builder);
			}
			return win32Builder;
		});

		return builder;
	}
}
