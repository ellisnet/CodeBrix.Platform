// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_CODEBRIX_WINUI || !IS_CODEBRIX_UI_PROJECT
#if HAS_CODEBRIX_WINUI && IS_CODEBRIX_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public enum DraggingState
	{
		Started,
		Continuing,
		Completed,
	}
}
#endif
