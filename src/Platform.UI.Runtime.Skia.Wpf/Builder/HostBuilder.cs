using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using CodeBrix.Platform.UI.Runtime.Skia;
using Windows.UI.WebUI;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseWindowsWpf(this ICodeBrixPlatformHostBuilder builder, Action<IWindowsSkiaHostBuilder> windowsBuilder = null)
	{
		builder.AddHostBuilder(() =>
		{
			var wpfBuilder = new WpfHostBuilder();
			if (wpfBuilder.IsSupported)
			{
				windowsBuilder?.Invoke(wpfBuilder);
			}
			return wpfBuilder;
		});

		return builder;
	}

	public static IWindowsSkiaHostBuilder WpfApplication(this IWindowsSkiaHostBuilder builder, Func<System.Windows.Application> action)
	{
		builder.WpfApplication = action;

		return builder;
	}
}
