#if false
using Microsoft.UI.Xaml.Media;
#if false
using UIKit;
#else
using CodeBrix.Platform.UI;
#endif

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Controls.Legacy; //Was previously: Uno.UI.Controls.Legacy

partial class ProgressRing
{
	private NativeProgressRing _native;

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_native = this.FindFirstChild<NativeProgressRing>();

		if (this.IsDependencyPropertySet(ForegroundProperty))
		{
			ApplyForeground();
		}

		TrySetNativeAnimating();
	}

	partial void TrySetNativeAnimating();

	protected override void OnForegroundColorChanged(Brush oldValue, Brush newValue)
	{
		base.OnForegroundColorChanged(oldValue, newValue);

		ApplyForeground();
	}
}

#endif
