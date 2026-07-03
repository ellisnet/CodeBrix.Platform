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
		WaylandNotSupported.WarnOnce(typeof(WaylandApplicationViewExtension),
			"ApplicationView.TryResizeView",
			"a client cannot force its outer window size; the compositor decides.");
		return false;
	}
}
