#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.Helpers; //Was previously: Uno.Helpers

internal static class UIElementAccessibilityHelper
{
	internal static Action<UIElement, UIElement, int?>? ExternalOnChildAdded { get; set; }
	internal static Action<UIElement, UIElement>? ExternalOnChildRemoved { get; set; }
}
