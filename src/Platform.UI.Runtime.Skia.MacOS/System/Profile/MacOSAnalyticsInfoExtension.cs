using Windows.System.Profile.Internal;

using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS; //Was previously: Uno.UI.Runtime.Skia.MacOS

internal class MacOSAnalyticsInfoExtension : IAnalyticsInfoExtension
{
	private static readonly MacOSAnalyticsInfoExtension _instance = new();

	private MacOSAnalyticsInfoExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), _ => _instance);

	public CodeBrixDeviceForm GetDeviceForm() => CodeBrixDeviceForm.Desktop;
}
