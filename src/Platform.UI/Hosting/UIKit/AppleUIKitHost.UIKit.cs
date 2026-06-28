#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Hosting.UIKit; //Was previously: Uno.UI.Hosting.UIKit

internal class AppleUIKitHost : CodeBrixPlatformHost
{
	private readonly Type _appType;

	/// <summary>
	/// Creates a host for an CodeBrix Skia Apple UIKit application.
	/// </summary>
	/// <param name="appBuilder">App builder.</param>
	/// <remarks>
	/// Environment.CommandLine is used to fill LaunchEventArgs.Arguments.
	/// </remarks>
	public AppleUIKitHost(Type appType)
	{
		_appType = appType ?? throw new ArgumentNullException(nameof(appType));
	}

	internal static ApplicationInitializationCallback? CreateAppAction { get; private set; }

	protected override void Initialize()
	{
	}

	protected override Task RunLoop()
	{
		try
		{
			if (_appType is null)
			{
				throw new InvalidOperationException("UIApplicationDelegate must be provided for UIKit native");
			}

			UIApplication.Main(Environment.GetCommandLineArgs(), null, _appType);

			return Task.CompletedTask;
		}
		catch (Exception e)
		{
			Console.WriteLine($"App failed to initialize: {e}");

			throw;
		}
	}
}
