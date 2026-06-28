#nullable enable

using System;
using CodeBrix.Platform.UI.Hosting.UIKit;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseAppleUIKit(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new AppleUIKitHostBuilder());

		return builder;
	}
}
