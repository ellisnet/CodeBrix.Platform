using System;

#nullable enable

using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Input;

#if !HAS_CODEBRIX
using System.Runtime.InteropServices;
#endif

#if HAS_CODEBRIX_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace CodeBrix.Platform.UI.Toolkit.DevTools.Input; //Was previously: Uno.UI.Toolkit.DevTools.Input

internal static class InputInjectorExtensions
{
	public static IInjectedPointer GetPointer(this InputInjector injector, PointerDeviceType pointer)
		=> pointer switch
		{
			PointerDeviceType.Touch => GetFinger(injector),
#if HAS_CODEBRIX
			PointerDeviceType.Mouse => GetMouse(injector),
#endif
			_ => throw new NotSupportedException($"Injection of {pointer} is not supported on this platform.")
		};

	public static Finger GetFinger(this InputInjector injector, uint id = 42)
		=> new(injector, id);

#if HAS_CODEBRIX
	public static Mouse GetMouse(this InputInjector injector)
		=> new(injector);
#endif
}
