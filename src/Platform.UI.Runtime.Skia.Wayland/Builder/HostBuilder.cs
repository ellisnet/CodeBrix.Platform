using System;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class WaylandHostBuilderExtensions
{
	public static ICodeBrixPlatformHostBuilder UseLinuxWayland(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new WaylandHostBuilder());
		return builder;
	}

	public static ICodeBrixPlatformHostBuilder UseLinuxWayland(this ICodeBrixPlatformHostBuilder builder, Action<WaylandHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var waylandBuilder = new WaylandHostBuilder();
			if (((IPlatformHostBuilder)waylandBuilder).IsSupported)
			{
				action.Invoke(waylandBuilder);
			}
			return waylandBuilder;
		});

		return builder;
	}
}
