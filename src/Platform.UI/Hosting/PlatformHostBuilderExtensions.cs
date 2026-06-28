#nullable enable

using System;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class CodeBrixPlatformHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Microsoft.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	public static ICodeBrixPlatformHostBuilder App<TApplication>(this ICodeBrixPlatformHostBuilder builder, Func<TApplication> appBuilder)
		where TApplication : Microsoft.UI.Xaml.Application
	{
		builder.AppBuilder = appBuilder;
		builder.SetAppType<TApplication>();
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the CodeBrixPlatformHost has been initialized, and before the run loop starts.
	/// </summary>
	public static ICodeBrixPlatformHostBuilder AfterInit(this ICodeBrixPlatformHostBuilder builder, Action action)
	{
		builder.AfterInitAction = action;
		return builder;
	}
}
