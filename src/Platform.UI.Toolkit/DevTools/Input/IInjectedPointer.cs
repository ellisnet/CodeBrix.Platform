
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

internal interface IInjectedPointer
{
	void Press(Point position);

	void MoveTo(Point position, uint? steps = null, uint? stepOffsetInMilliseconds = null);

	void MoveBy(double deltaX = 0, double deltaY = 0);

	void Release();
}
