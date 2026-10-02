using CodeBrix.Platform.Foundation.Extensibility;

namespace Microsoft.UI.Xaml.Controls;

public partial class MenuBar
{
	partial void InitializeNativeMenuBar()
	{
		if (ApiExtensibility.CreateInstance<INativeMenuBarExtension>(this, out var nativeMenu))
		{
			Loaded += (_, _) => nativeMenu.Load();
			Unloaded += (_, _) => nativeMenu.Unload();
		}
	}

	internal void SetNativePresentation(bool enabled)
	{
		if (IsVisualPresentationSuppressed == enabled) { return; }
		// Preserve Visibility, sizing, bindings and the logical lifetime. Only
		// the chosen bar's visual/layout presentation moves out of the window.
		IsVisualPresentationSuppressed = enabled;
		UpdateHitTest();
		InvalidateMeasure();
		InvalidateArrange();
	}
}
