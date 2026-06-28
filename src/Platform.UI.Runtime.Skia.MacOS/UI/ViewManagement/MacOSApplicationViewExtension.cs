using Windows.Foundation;
using Windows.UI.ViewManagement;

using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS; //Was previously: Uno.UI.Runtime.Skia.MacOS

internal class MacOSApplicationViewExtension : IApplicationViewExtension
{
	private static readonly MacOSApplicationViewExtension _instance = new();

	private MacOSApplicationViewExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IApplicationViewExtension), _ => _instance);

	public bool TryResizeView(Size size)
	{
		var main = NativeCodeBrix.codebrix_app_get_main_window();
		return NativeCodeBrix.codebrix_window_resize(main, size.Width, size.Height);
	}
}
