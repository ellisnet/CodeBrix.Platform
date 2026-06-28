using Windows.Foundation;

namespace Microsoft.UI.Input;

#if HAS_CODEBRIX_WINUI
public
#else
internal
#endif
	partial class InputFocusController
#if HAS_CODEBRIX_WINUI
	: global::Microsoft.UI.Input.InputObject
#endif
{
	internal InputFocusController()
	{

	}

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public FocusNavigationResult DepartFocus(FocusNavigationRequest request) => FocusNavigationResult.NotMoved;

#pragma warning disable CS0067 // Unused members
	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> GotFocus;

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public event TypedEventHandler<InputFocusController, FocusChangedEventArgs> LostFocus;
#pragma warning restore CS0067

	[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
	public bool HasFocus => false;
}
