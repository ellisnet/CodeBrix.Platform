#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public partial class AnalyticsInfo
{
	private static CodeBrixDeviceForm GetDeviceForm() => CodeBrixDeviceForm.Desktop;
}
#endif
