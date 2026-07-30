#nullable enable

using Microsoft.UI.Xaml;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/OverloadInsightWindow.cs in the
//AvalonEdit repo (MIT). Key handling arrives through the base class's OnKeyDown(VirtualKey,
//VirtualKeyModifiers) seam instead of a WPF KeyEventArgs override; otherwise unchanged.

/// <summary>
/// Insight window that shows an OverloadViewer.
/// </summary>
public class OverloadInsightWindow : InsightWindow
{
	readonly OverloadViewer overloadViewer = new OverloadViewer();

	/// <summary>
	/// Creates a new OverloadInsightWindow.
	/// </summary>
	public OverloadInsightWindow(TextArea textArea) : base(textArea)
	{
		overloadViewer.Margin = new Thickness(2, 0, 0, 0);
		this.Content = overloadViewer;
	}

	/// <summary>
	/// Gets/Sets the item provider.
	/// </summary>
	public IOverloadProvider? Provider
	{
		get { return overloadViewer.Provider; }
		set { overloadViewer.Provider = value; }
	}

	/// <summary>
	/// Handles Up/Down cycling through the overloads (in addition to the base class's Escape
	/// handling) while more than one overload is available.
	/// </summary>
	protected override bool OnKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		bool handled = base.OnKeyDown(key, modifiers);
		if (!handled && this.Provider != null && this.Provider.Count > 1)
		{
			switch (key)
			{
				case VirtualKey.Up:
					handled = true;
					overloadViewer.ChangeIndex(-1);
					break;
				case VirtualKey.Down:
					handled = true;
					overloadViewer.ChangeIndex(+1);
					break;
			}
			if (handled)
			{
				overloadViewer.UpdateLayout();
				UpdatePosition();
			}
		}
		return handled;
	}
}
