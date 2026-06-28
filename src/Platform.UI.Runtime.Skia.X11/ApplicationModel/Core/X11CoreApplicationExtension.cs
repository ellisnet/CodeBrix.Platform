#nullable enable

using CodeBrix.Platform.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

internal class X11CoreApplicationExtension : ICoreApplicationExtension
{
	public X11CoreApplicationExtension()
	{
	}

	public bool CanExit => true;

	public void Exit()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Application has requested an exit");
		}

		X11XamlRootHost.CloseAllWindows();
	}
}
