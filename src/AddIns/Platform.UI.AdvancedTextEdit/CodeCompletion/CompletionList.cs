#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/CompletionList.cs (and the
//CompletionList.xaml ControlTemplate/DataTemplate) in the AvalonEdit repo (MIT). The filtering
//and match-quality algorithm is transliterated. Re-expressions:
//- The visual tree (a CompletionListBox, plus the empty-list placeholder) is built in code;
//  the WPF DataTemplate (Image 16x16 + content) became a per-item container factory that
//  realizes ListViewItem elements up front (cached per data item, reused across filtering).
//- HandleKey takes the VirtualKey directly and returns whether it was handled; the WPF
//  double-click became DoubleTapped, and EmptyTemplate (a ControlTemplate) became the
//  EmptyContent element property.
//- SelectionChanged is a plain .NET event carrying data items (not a re-exposed routed event).

/// <summary>
/// The completion list control used inside the CompletionWindow; hosts a CompletionListBox.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class CompletionList : Control
{
	readonly CompletionListBox listBox;
	readonly ContentPresenter emptyContentHost;
	readonly Dictionary<ICompletionData, ListViewItem> containerCache = new Dictionary<ICompletionData, ListViewItem>();
	Grid? rootGrid;

	/// <summary>
	/// Creates a new CompletionList.
	/// </summary>
	public CompletionList()
	{
		listBox = new CompletionListBox();
		listBox.SelectionChanged += ListBoxSelectionChanged;
		listBox.DoubleTapped += ListBoxDoubleTapped;
		emptyContentHost = new ContentPresenter
		{
			Visibility = Visibility.Collapsed,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		completionData.CollectionChanged += (sender, e) =>
		{
			// While no filter ran yet, the list mirrors the data collection (upstream bound
			// ItemsSource to the ObservableCollection; filtering swaps in snapshot lists).
			if (currentList == null)
				RefreshListBoxItems(completionData);
		};
		//was previously: the tree came from the CompletionList.xaml ControlTemplate
		//(PART_ListBox); this port builds the equivalent tree in code.
		Template = new ControlTemplate(CreateTemplateRoot);
		RefreshListBoxItems(completionData);
	}

	UIElement CreateTemplateRoot()
	{
		rootGrid?.Children.Clear();
		rootGrid = new Grid();
		rootGrid.Children.Add(listBox);
		rootGrid.Children.Add(emptyContentHost);
		return rootGrid;
	}

	bool isFiltering = true;
	/// <summary>
	/// If true, the CompletionList is filtered to show only matching items. Also enables search by substring.
	/// If false, enables the old behavior: no filtering, search by string.StartsWith.
	/// </summary>
	public bool IsFiltering
	{
		get { return isFiltering; }
		set { isFiltering = value; }
	}

	/// <summary>
	/// The element shown when the CompletionList contains no items (e.g. a TextBlock saying
	/// "no completions"). If EmptyContent is null, nothing will be shown.
	/// </summary>
	//was previously: the EmptyTemplate dependency property (a ControlTemplate); a plain element
	//property here because the tree is built in code.
	public UIElement? EmptyContent
	{
		get { return emptyContentHost.Content as UIElement; }
		set
		{
			emptyContentHost.Content = value;
			UpdateEmptyContentVisibility();
		}
	}

	/// <summary>
	/// Is raised when the completion list indicates that the user has chosen
	/// an entry to be completed.
	/// </summary>
	public event EventHandler? InsertionRequested;

	/// <summary>
	/// Raises the InsertionRequested event.
	/// </summary>
	public void RequestInsertion(EventArgs e)
	{
		InsertionRequested?.Invoke(this, e);
	}

	/// <summary>
	/// Gets the list box.
	/// </summary>
	public CompletionListBox ListBox
	{
		get { return listBox; }
	}

	/// <summary>
	/// Gets the scroll viewer used in this list box, or null while the list box template has
	/// not been materialized yet.
	/// </summary>
	public ScrollViewer? ScrollViewer
	{
		get { return listBox.ScrollViewerInternal; }
	}

	readonly ObservableCollection<ICompletionData> completionData = new ObservableCollection<ICompletionData>();

	/// <summary>
	/// Gets the list to which completion data can be added.
	/// </summary>
	public IList<ICompletionData> CompletionData
	{
		get { return completionData; }
	}

	/// <inheritdoc/>
	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		base.OnKeyDown(e);
		if (!e.Handled)
		{
			e.Handled = HandleKey(e.Key);
		}
	}

	/// <summary>
	/// Handles a key press. Used to let the completion list handle key presses while the
	/// focus is still on the text editor.
	/// </summary>
	/// <param name="key">The key that was pressed.</param>
	/// <returns>True when the completion list consumed the key press; false otherwise.</returns>
	//was previously: HandleKey(KeyEventArgs e), setting e.Handled.
	public bool HandleKey(VirtualKey key)
	{
		// We have to do some key handling manually, because the default doesn't work with
		// our simulated events.
		// Also, the default PageUp/PageDown implementation changes the focus, so we avoid it.
		switch (key)
		{
			case VirtualKey.Down:
				listBox.SelectIndex(listBox.SelectedIndex + 1);
				return true;
			case VirtualKey.Up:
				listBox.SelectIndex(listBox.SelectedIndex - 1);
				return true;
			case VirtualKey.PageDown:
				listBox.SelectIndex(listBox.SelectedIndex + listBox.VisibleItemCount);
				return true;
			case VirtualKey.PageUp:
				listBox.SelectIndex(listBox.SelectedIndex - listBox.VisibleItemCount);
				return true;
			case VirtualKey.Home:
				listBox.SelectIndex(0);
				return true;
			case VirtualKey.End:
				listBox.SelectIndex(listBox.Items.Count - 1);
				return true;
			case VirtualKey.Tab:
			case VirtualKey.Enter:
				//was previously: RequestInsertion(e) with the WPF KeyEventArgs; the port has no
				//args object for forwarded keys, so EventArgs.Empty marks key-triggered insertion.
				RequestInsertion(EventArgs.Empty);
				return true;
			default:
				return false;
		}
	}

	void ListBoxDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
	{
		//was previously: OnMouseDoubleClick with ChangedButton == Left; only double taps on the
		//realized items count, not on the scroll bar.
		DependencyObject? source = e.OriginalSource as DependencyObject;
		while (source != null && source != this && source != listBox)
		{
			if (source is ListViewItem)
			{
				e.Handled = true;
				RequestInsertion(e);
				return;
			}
			source = VisualTreeHelper.GetParent(source);
		}
	}

	/// <summary>
	/// Gets/Sets the selected item.
	/// </summary>
	/// <remarks>
	/// The setter of this property does not scroll to the selected item.
	/// You might want to also call <see cref="ScrollIntoView"/>.
	/// </remarks>
	public ICompletionData? SelectedItem
	{
		get
		{
			int index = listBox.SelectedIndex;
			IReadOnlyList<ICompletionData> data = listBox.DataItems;
			return (index >= 0 && index < data.Count) ? data[index] : null;
		}
		set
		{
			if (value == null)
			{
				listBox.ClearSelection();
			}
			else
			{
				int index = IndexOfDataItem(value);
				listBox.SelectedIndex = index;
			}
		}
	}

	/// <summary>
	/// Scrolls the specified item into view.
	/// </summary>
	public void ScrollIntoView(ICompletionData item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));
		int index = IndexOfDataItem(item);
		if (index >= 0 && listBox.Items.Count > index)
			listBox.ScrollIntoView(listBox.Items[index]);
	}

	int IndexOfDataItem(ICompletionData item)
	{
		IReadOnlyList<ICompletionData> data = listBox.DataItems;
		for (int i = 0; i < data.Count; i++)
		{
			if (data[i] == item)
				return i;
		}
		return -1;
	}

	/// <summary>
	/// Occurs when the SelectedItem property changes. The event args carry the affected
	/// <see cref="ICompletionData"/> items.
	/// </summary>
	//was previously: re-exposed the WPF Selector.SelectionChanged routed event.
	public event SelectionChangedEventHandler? SelectionChanged;

	void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SelectionChangedEventHandler? handler = SelectionChanged;
		if (handler == null)
			return;
		// map the realized containers back to their completion data
		handler(this, new SelectionChangedEventArgs(MapToData(e.RemovedItems), MapToData(e.AddedItems)));
	}

	static IList<object> MapToData(IList<object> containers)
	{
		List<object> result = new List<object>(containers.Count);
		foreach (object container in containers)
		{
			if (container is ListViewItem { Tag: ICompletionData data })
				result.Add(data);
		}
		return result;
	}

	#region Item realization
	//was previously: the CompletionList.xaml DataTemplate (horizontal StackPanel with a 16x16
	//Image bound to ICompletionData.Image and a ContentPresenter bound to Content). This
	//framework cannot build a DataTemplate in code, so the port realizes one ListViewItem per
	//data item up front (the container cache reuses them across re-filtering).

	ListViewItem GetContainer(ICompletionData item, HashSet<ListViewItem> usedContainers)
	{
		if (!containerCache.TryGetValue(item, out ListViewItem? container))
		{
			container = CreateItemContainer(item);
			containerCache[item] = container;
		}
		if (!usedContainers.Add(container))
		{
			// the same data item appears twice in the list: a container cannot be shown twice,
			// so realize an extra one for the duplicate
			container = CreateItemContainer(item);
			usedContainers.Add(container);
		}
		return container;
	}

	ListViewItem CreateItemContainer(ICompletionData item)
	{
		StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
		Image image = new Image
		{
			Width = 16,
			Height = 16,
			Margin = new Thickness(0, 0, 2, 0)
		};
		if (item.Image != null)
			image.Source = item.Image;
		else
			image.Visibility = Visibility.Collapsed;
		panel.Children.Add(image);
		object? itemContent = item.Content;
		if (itemContent is UIElement contentElement)
			panel.Children.Add(contentElement);
		else if (itemContent != null)
			panel.Children.Add(new TextBlock { Text = itemContent as string ?? itemContent.ToString(), VerticalAlignment = VerticalAlignment.Center });
		return new ListViewItem
		{
			Content = panel,
			Tag = item,
			// editor-density metrics: the default ListViewItem chrome is touch-sized
			MinHeight = 0,
			Padding = new Thickness(4, 1, 4, 1),
			HorizontalContentAlignment = HorizontalAlignment.Left,
			VerticalContentAlignment = VerticalAlignment.Center,
			// The popup surface is always light (see CompletionWindow's chrome), while the app
			// theme may be dark - without an explicit foreground the item text inherits the
			// theme's light-on-dark brush and disappears on the light popup.
			Foreground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x00, 0x00)),
		};
	}

	void RefreshListBoxItems(IEnumerable<ICompletionData> data)
	{
		List<ICompletionData> dataList = data.ToList();
		HashSet<ListViewItem> usedContainers = new HashSet<ListViewItem>();
		List<ListViewItem> containers = new List<ListViewItem>(dataList.Count);
		foreach (ICompletionData item in dataList)
		{
			containers.Add(GetContainer(item, usedContainers));
		}
		listBox.SetItems(dataList, containers);
		UpdateEmptyContentVisibility();
	}

	void UpdateEmptyContentVisibility()
	{
		bool showEmptyContent = listBox.DataItems.Count == 0 && emptyContentHost.Content != null;
		emptyContentHost.Visibility = showEmptyContent ? Visibility.Visible : Visibility.Collapsed;
	}
	#endregion

	// SelectItem gets called twice for every typed character (once from FormatLine), this helps execute SelectItem only once
	string? currentText;
	List<ICompletionData>? currentList;

	/// <summary>
	/// Selects the best match, and filter the items if turned on using <see cref="IsFiltering" />.
	/// </summary>
	public void SelectItem(string text)
	{
		if (text == currentText)
			return;

		if (this.IsFiltering)
		{
			SelectItemFiltering(text);
		}
		else
		{
			SelectItemWithStart(text);
		}
		currentText = text;
	}

	/// <summary>
	/// Filters CompletionList items to show only those matching given query, and selects the best match.
	/// </summary>
	void SelectItemFiltering(string query)
	{
		// if the user just typed one more character, don't filter all data but just filter what we are already displaying
		IEnumerable<ICompletionData> listToFilter = (this.currentList != null && (!string.IsNullOrEmpty(this.currentText)) && (!string.IsNullOrEmpty(query)) &&
							query.StartsWith(this.currentText, StringComparison.Ordinal)) ?
			this.currentList : (IEnumerable<ICompletionData>)this.completionData;

		var matchingItems =
			from item in listToFilter
			let quality = GetMatchQuality(item.Text, query)
			where quality > 0
			select new { Item = item, Quality = quality };

		// e.g. "DateTimeKind k = (*cc here suggests DateTimeKind*)"
		ICompletionData? suggestedItem = listBox.SelectedIndex != -1 ? listBox.DataItems[listBox.SelectedIndex] : null;

		List<ICompletionData> listBoxItems = new List<ICompletionData>();
		int bestIndex = -1;
		int bestQuality = -1;
		double bestPriority = 0;
		int i = 0;
		foreach (var matchingItem in matchingItems)
		{
			double priority = matchingItem.Item == suggestedItem ? double.PositiveInfinity : matchingItem.Item.Priority;
			int quality = matchingItem.Quality;
			if (quality > bestQuality || (quality == bestQuality && (priority > bestPriority)))
			{
				bestIndex = i;
				bestPriority = priority;
				bestQuality = quality;
			}
			listBoxItems.Add(matchingItem.Item);
			i++;
		}
		this.currentList = listBoxItems;
		RefreshListBoxItems(listBoxItems);
		SelectIndexCentered(bestIndex);
	}

	/// <summary>
	/// Selects the item that starts with the specified query.
	/// </summary>
	void SelectItemWithStart(string query)
	{
		if (string.IsNullOrEmpty(query))
			return;

		int suggestedIndex = listBox.SelectedIndex;

		int bestIndex = -1;
		int bestQuality = -1;
		double bestPriority = 0;
		for (int i = 0; i < completionData.Count; ++i)
		{
			int quality = GetMatchQuality(completionData[i].Text, query);
			if (quality < 0)
				continue;

			double priority = completionData[i].Priority;
			bool useThisItem;
			if (bestQuality < quality)
			{
				useThisItem = true;
			}
			else
			{
				if (bestIndex == suggestedIndex)
				{
					useThisItem = false;
				}
				else if (i == suggestedIndex)
				{
					// prefer recommendedItem, regardless of its priority
					useThisItem = bestQuality == quality;
				}
				else
				{
					useThisItem = bestQuality == quality && bestPriority < priority;
				}
			}
			if (useThisItem)
			{
				bestIndex = i;
				bestPriority = priority;
				bestQuality = quality;
			}
		}
		SelectIndexCentered(bestIndex);
	}

	void SelectIndexCentered(int bestIndex)
	{
		if (bestIndex < 0)
		{
			listBox.ClearSelection();
		}
		else
		{
			int firstItem = listBox.FirstVisibleItem;
			if (bestIndex < firstItem || firstItem + listBox.VisibleItemCount <= bestIndex)
			{
				listBox.CenterViewOn(bestIndex);
				listBox.SelectIndex(bestIndex);
			}
			else
			{
				listBox.SelectIndex(bestIndex);
			}
		}
	}

	int GetMatchQuality(string itemText, string query)
	{
		if (itemText == null)
			throw new ArgumentNullException(nameof(itemText), "ICompletionData.Text returned null");

		// Qualities:
		//  	8 = full match case sensitive
		// 		7 = full match
		// 		6 = match start case sensitive
		//		5 = match start
		//		4 = match CamelCase when length of query is 1 or 2 characters
		// 		3 = match substring case sensitive
		//		2 = match substring
		//		1 = match CamelCase
		//		-1 = no match
		if (query == itemText)
			return 8;
		if (string.Equals(itemText, query, StringComparison.InvariantCultureIgnoreCase))
			return 7;

		if (itemText.StartsWith(query, StringComparison.InvariantCulture))
			return 6;
		if (itemText.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
			return 5;

		bool? camelCaseMatch = null;
		if (query.Length <= 2)
		{
			camelCaseMatch = CamelCaseMatch(itemText, query);
			if (camelCaseMatch == true)
				return 4;
		}

		// search by substring, if filtering (i.e. new behavior) turned on
		if (IsFiltering)
		{
			if (itemText.IndexOf(query, StringComparison.InvariantCulture) >= 0)
				return 3;
			if (itemText.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
				return 2;
		}

		if (!camelCaseMatch.HasValue)
			camelCaseMatch = CamelCaseMatch(itemText, query);
		if (camelCaseMatch == true)
			return 1;

		return -1;
	}

	static bool CamelCaseMatch(string text, string query)
	{
		// We take the first letter of the text regardless of whether or not it's upper case so we match
		// against camelCase text as well as PascalCase text ("cct" matches "camelCaseText")
		IEnumerable<char> theFirstLetterOfEachWord = text.Take(1).Concat(text.Skip(1).Where(char.IsUpper));

		int i = 0;
		foreach (char letter in theFirstLetterOfEachWord)
		{
			if (i > query.Length - 1)
				return true;    // return true here for CamelCase partial match ("CQ" matches "CodeQualityAnalysis")
			if (char.ToUpperInvariant(query[i]) != char.ToUpperInvariant(letter))
				return false;
			i++;
		}
		if (i >= query.Length)
			return true;
		return false;
	}
}
