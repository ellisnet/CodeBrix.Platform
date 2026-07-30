#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/CompletionListBox.cs in the AvalonEdit
//repo (MIT). Re-expressed on ListView instead of the WPF ListBox: the items shown are
//pre-realized ListViewItem containers built by CompletionList (no WPF DataTemplate), so this
//class carries the parallel ICompletionData list (see DataItems) that maps indices back to
//completion data. The scroll math is unchanged - WPF's item-based scroll units and this
//framework's pixel-based units cancel out of the count*offset/extent formulas alike.

/// <summary>
/// The list box used inside the CompletionList.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class CompletionListBox : ListView
{
	internal ScrollViewer? scrollViewer;
	List<ICompletionData> dataItems = new List<ICompletionData>();

	/// <summary>
	/// Creates a new CompletionListBox.
	/// </summary>
	public CompletionListBox()
	{
		SelectionMode = ListViewSelectionMode.Single;
	}

	/// <inheritdoc/>
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// Find the scroll viewer:
		scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
		if (scrollViewer == null)
			scrollViewer = FindScrollViewer(this);
	}

	internal ScrollViewer? ScrollViewerInternal
	{
		get
		{
			if (scrollViewer == null)
				scrollViewer = FindScrollViewer(this);
			return scrollViewer;
		}
	}

	static ScrollViewer? FindScrollViewer(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is ScrollViewer result)
				return result;
			ScrollViewer? nested = FindScrollViewer(child);
			if (nested != null)
				return nested;
		}
		return null;
	}

	/// <summary>
	/// Gets the completion data items currently shown, parallel to the realized item containers
	/// in the list (same order, same count).
	/// </summary>
	public IReadOnlyList<ICompletionData> DataItems
	{
		get { return dataItems; }
	}

	/// <summary>
	/// Replaces the shown items. <paramref name="data"/> and <paramref name="containers"/> must
	/// be parallel lists.
	/// </summary>
	internal void SetItems(List<ICompletionData> data, List<ListViewItem> containers)
	{
		dataItems = data;
		ItemsSource = containers;
	}

	/// <summary>
	/// Gets the number of the first visible item.
	/// </summary>
	public int FirstVisibleItem
	{
		get
		{
			ScrollViewer? sv = ScrollViewerInternal;
			if (sv == null || sv.ExtentHeight == 0)
			{
				return 0;
			}
			else
			{
				return (int)(this.Items.Count * sv.VerticalOffset / sv.ExtentHeight);
			}
		}
		set
		{
			value = value.CoerceValue(0, this.Items.Count - this.VisibleItemCount);
			ScrollViewer? sv = ScrollViewerInternal;
			if (sv != null && this.Items.Count > 0)
			{
				//was previously: scrollViewer.ScrollToVerticalOffset(...); ChangeView is the
				//equivalent in this framework (animation disabled).
				sv.ChangeView(null, (double)value / this.Items.Count * sv.ExtentHeight, null, true);
			}
		}
	}

	/// <summary>
	/// Gets the number of visible items.
	/// </summary>
	public int VisibleItemCount
	{
		get
		{
			ScrollViewer? sv = ScrollViewerInternal;
			if (sv == null || sv.ExtentHeight == 0)
			{
				return 10;
			}
			else
			{
				return Math.Max(
					3,
					(int)Math.Ceiling(this.Items.Count * sv.ViewportHeight
									  / sv.ExtentHeight));
			}
		}
	}

	/// <summary>
	/// Removes the selection.
	/// </summary>
	public void ClearSelection()
	{
		this.SelectedIndex = -1;
	}

	/// <summary>
	/// Selects the item with the specified index and scrolls it into view.
	/// </summary>
	public void SelectIndex(int index)
	{
		if (index >= this.Items.Count)
			index = this.Items.Count - 1;
		if (index < 0)
			index = 0;
		this.SelectedIndex = index;
		if (this.SelectedItem != null)
			this.ScrollIntoView(this.SelectedItem);
	}

	/// <summary>
	/// Centers the view on the item with the specified index.
	/// </summary>
	public void CenterViewOn(int index)
	{
		this.FirstVisibleItem = index - VisibleItemCount / 2;
	}
}
