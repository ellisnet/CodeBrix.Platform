#if IS_UNIT_TESTS || __NETSTD_REFERENCE__
#nullable enable

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private bool TryShowPlatform() => false;

	private bool TryHidePlatform() => false;
}
#endif
