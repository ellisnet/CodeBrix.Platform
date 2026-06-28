using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Windows.Devices.Input;

namespace CodeBrix.Platform.UI.Extensions; //Was previously: Uno.UI.Extensions

internal static class UITouchExtensions
{
	public static PointerDeviceType ToPointerDeviceType(this UITouchType touchType) =>
		touchType switch
		{
			UITouchType.Stylus => PointerDeviceType.Pen,
			UITouchType.IndirectPointer => PointerDeviceType.Mouse,
			UITouchType.Indirect => PointerDeviceType.Mouse,
			_ => PointerDeviceType.Touch // Use touch as default fallback.
		};
}
