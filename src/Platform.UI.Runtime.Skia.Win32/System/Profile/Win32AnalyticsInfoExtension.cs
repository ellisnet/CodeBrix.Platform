using Windows.System.Profile.Internal;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32; //Was previously: Uno.UI.Runtime.Skia.Win32

internal class Win32AnalyticsInfoExtension : IAnalyticsInfoExtension
{
	public static Win32AnalyticsInfoExtension Instance { get; } = new();

	private Win32AnalyticsInfoExtension()
	{
	}

	public CodeBrixDeviceForm GetDeviceForm() => CodeBrixDeviceForm.Desktop;
}
