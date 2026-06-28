using CodeBrix.Platform.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

#if HAS_CODEBRIX_WINUI
public
#else
internal
#endif
partial class InputKeyboardSource
{
	public static CoreVirtualKeyStates GetKeyStateForCurrentThread(VirtualKey virtualKey)
		=> KeyboardStateTracker.GetKeyState(virtualKey);
}
