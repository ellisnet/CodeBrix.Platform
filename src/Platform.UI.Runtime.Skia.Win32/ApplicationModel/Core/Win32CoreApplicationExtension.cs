#nullable enable

using CodeBrix.Platform.ApplicationModel.Core;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Dispatching;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32;

internal class Win32CoreApplicationExtension : ICoreApplicationExtension
{
	public static Win32CoreApplicationExtension Instance { get; } = new();

	private Win32CoreApplicationExtension()
	{
	}

	public bool CanExit => true;

	public void Exit()
	{
		this.LogDebug()?.Debug("Application has requested an exit");

		// DestroyWindow must be called on the thread that created the window, so make sure we're
		// on the native dispatcher thread even if Exit() was invoked from somewhere else.
		Win32EventLoop.Schedule(Win32WindowWrapper.CloseAllWindows, NativeDispatcherPriority.Normal);
	}
}
