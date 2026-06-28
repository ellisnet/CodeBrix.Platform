#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting.UIKit;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder
{
	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => DeviceTargetHelper.IsUIKit();

	public CodeBrixPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) =>
		new AppleUIKitHost(appType);
}
