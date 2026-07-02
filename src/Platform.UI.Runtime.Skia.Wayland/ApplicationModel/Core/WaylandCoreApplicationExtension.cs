#nullable enable

using CodeBrix.Platform.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandCoreApplicationExtension : ICoreApplicationExtension
{
	public WaylandCoreApplicationExtension()
	{
	}

	public bool CanExit => true;

	public void Exit()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Application has requested an exit");
		}

		WaylandXamlRootHost.CloseAllWindows();
	}
}
