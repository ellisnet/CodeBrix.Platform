using Windows.System.Profile.Internal;

namespace CodeBrix.Platform.Extensions.System.Profile
{
	internal class AnalyticsInfoExtension : IAnalyticsInfoExtension
	{
		public CodeBrixDeviceForm GetDeviceForm() => CodeBrixDeviceForm.Desktop;
	}
}
