using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Hosting;
using Windows.UI.WebUI;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseLinuxX11(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new X11HostBuilder());
		return builder;
	}

	public static ICodeBrixPlatformHostBuilder UseLinuxX11(this ICodeBrixPlatformHostBuilder builder, Action<X11HostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var x11Builder = new X11HostBuilder();
			if (((IPlatformHostBuilder)x11Builder).IsSupported)
			{
				action.Invoke(x11Builder);
			}
			return x11Builder;
		});

		return builder;
	}
}
