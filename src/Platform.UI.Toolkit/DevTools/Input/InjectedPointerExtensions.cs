
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

internal static class InjectedPointerExtensions
{
	public static void Press(this IInjectedPointer pointer, double x, double y)
		=> pointer.Press(new(x, y));

	public static void MoveTo(this IInjectedPointer pointer, double x, double y)
		=> pointer.MoveTo(new(x, y));

	public static void Drag(this IInjectedPointer pointer, Point from, Point to, uint? steps = null, uint? stepOffsetInMilliseconds = null)
	{
		pointer.Press(from);
		pointer.MoveTo(to, steps, stepOffsetInMilliseconds);
		pointer.Release();
	}

	public static void Tap(this IInjectedPointer pointer, Point location)
	{
		pointer.Press(location);
		pointer.Release();
	}
}
