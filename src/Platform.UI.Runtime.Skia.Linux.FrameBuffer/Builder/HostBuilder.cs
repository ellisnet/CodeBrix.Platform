using System;
using CodeBrix.Platform.UI.Runtime.Skia;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new FramebufferHostBuilder());
		return builder;
	}

	public static ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer(this ICodeBrixPlatformHostBuilder builder, Action<FramebufferHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var fbBuilder = new FramebufferHostBuilder();
			if (((IPlatformHostBuilder)fbBuilder).IsSupported)
			{
				action.Invoke(fbBuilder);
			}
			return fbBuilder;
		});

		return builder;
	}
}
