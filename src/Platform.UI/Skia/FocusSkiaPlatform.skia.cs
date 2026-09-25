#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Contracts;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IFocusPlatform"/>: on the browser, moves the accessibility focus to the
/// semantic DOM element of the focused element; elsewhere there is no native focus to move.
/// </summary>
internal sealed partial class FocusSkiaPlatform : IFocusPlatform
{
	/// <inheritdoc />
	public void FocusNative(UIElement? control)
	{
		if (OperatingSystem.IsBrowser() && control is not null && (control as Control)?.IsDelegatingFocusToTemplateChild() != true)
		{
			NativeMethods.FocusSemanticElement(control.Visual.Handle);
		}
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.CodeBrix.Platform.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		public static partial void FocusSemanticElement(IntPtr handle);
	}
}
