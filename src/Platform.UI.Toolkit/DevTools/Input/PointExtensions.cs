
#nullable enable

using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;

#if !HAS_CODEBRIX
using System.Runtime.InteropServices;
#endif

#if HAS_CODEBRIX_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace CodeBrix.Platform.UI.Toolkit.DevTools.Input; //Was previously: Uno.UI.Toolkit.DevTools.Input

internal static class PointExtensions
{
	public static Point OffsetLinear(this Point point, double xAndY)
		=> new(point.X + xAndY, point.Y + xAndY);

	public static Point Offset(this Point point, double x = 0, double y = 0)
		=> new(point.X + x, point.Y + y);
}
