using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using CodeBrix.Platform.UI.Runtime.Skia;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf;
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

	/// <summary>
	/// Selects the WPF dispatcher priority tier the CodeBrix dispatcher pump runs at.
	/// </summary>
	/// <remarks>
	/// Opt into <see cref="WpfDispatcherScheduling.InputFair"/> for apps that schedule UI work
	/// continuously (games presenting a frame every tic, perpetually animating canvases). Left
	/// unset, the historical <see cref="WpfDispatcherScheduling.RenderFirst"/> behavior applies, so
	/// existing apps are unaffected.
	/// </remarks>
	/// <param name="builder">The Windows Skia host builder.</param>
	/// <param name="scheduling">The scheduling mode to use.</param>
	/// <returns>The same host builder, for chaining.</returns>
	public static IWindowsSkiaHostBuilder DispatcherScheduling(this IWindowsSkiaHostBuilder builder, WpfDispatcherScheduling scheduling)
	{
		builder.DispatcherScheduling = scheduling;

		return builder;
	}
}
