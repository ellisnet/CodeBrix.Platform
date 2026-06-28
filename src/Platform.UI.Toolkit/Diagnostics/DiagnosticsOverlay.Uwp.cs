#nullable enable
#if !WINUI && !HAS_CODEBRIX_WINUI
using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.Diagnostics.UI; //Was previously: Uno.Diagnostics.UI

public sealed partial class DiagnosticsOverlay : Control
{
	// Note: This file is only to let the DiagnosticsOverlay.xaml (ref unconditionally in Generic.xaml) to compile properly.
}
#endif
