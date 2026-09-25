#if !__NETSTD_REFERENCE__
#nullable enable

using CodeBrix.Platform.UI.Contracts;

namespace Microsoft.UI.Xaml.Input;

public partial class FocusManager
{
	private static void FocusNative(UIElement? control) => PlatformServices.Focus.FocusNative(control);
}
#endif
