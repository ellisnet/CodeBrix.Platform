using UIKit;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public static partial class AnalyticsInfo
{
	private static CodeBrixDeviceForm GetDeviceForm()
	{
		return UIDevice.CurrentDevice.UserInterfaceIdiom switch
		{
			UIUserInterfaceIdiom.Phone => CodeBrixDeviceForm.Mobile,
			UIUserInterfaceIdiom.Pad => CodeBrixDeviceForm.Tablet,
			UIUserInterfaceIdiom.TV => CodeBrixDeviceForm.Television,
			UIUserInterfaceIdiom.CarPlay => CodeBrixDeviceForm.Car,
			_ => CodeBrixDeviceForm.Unknown,
		};
	}
}
