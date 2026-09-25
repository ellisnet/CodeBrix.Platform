#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.Helpers; //Was previously: Uno.Helpers

internal static class VisualAccessibilityHelper
{
	internal static Action<Visual>? ExternalOnVisualOffsetOrSizeChanged { get; set; }
}
#endif
