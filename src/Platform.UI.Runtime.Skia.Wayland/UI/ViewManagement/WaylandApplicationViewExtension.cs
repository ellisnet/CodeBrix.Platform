using Windows.Foundation;
using Windows.UI.ViewManagement;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandApplicationViewExtension(object owner) : IApplicationViewExtension
{
	private readonly ApplicationView _owner = (ApplicationView)owner;

	public bool TryResizeView(Size size)
	{
		// A Wayland client cannot force its outer window size; the compositor decides.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"TryResizeView({size.Width}x{size.Height}) is not available on Wayland.");
		}
		return false;
	}
}
