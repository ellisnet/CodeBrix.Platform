#if HAS_CODEBRIX_WINUI && IS_CODEBRIX_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	public delegate void DispatcherQueueHandler();
}
