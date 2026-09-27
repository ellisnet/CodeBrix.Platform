using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CodeBrix.Platform.Extensions.Specialized;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Presents a list of items the user can select: one (<see cref="SelectionMode.Single"/>), any number by toggling them
/// (<see cref="SelectionMode.Multiple"/>), or a range with the Shift and Control keys (<see cref="SelectionMode.Extended"/>).
/// </summary>
/// <remarks>
/// A <see cref="Selector"/> whose containers are <see cref="ListBoxItem"/>s. Keyboard: the arrow keys (in the direction of
/// the items panel), Home, End, PageUp and PageDown move the focused item; in Single mode the selection follows the
/// focus (unless <see cref="SingleSelectionFollowsFocus"/> is false or Control is held); in Extended mode the selection
/// follows the focus, Shift extends it from the anchor and Control moves the focus alone; Space selects (Single),
/// toggles (Multiple, Extended with Control) the focused item; Control+A selects every item (Multiple, Extended).
/// The default items panel is a <see cref="StackPanel"/>, which stands in for VirtualizingStackPanel (not implemented on
/// the Skia heads and Core).
/// </remarks>
public partial class ListBox : Selector
{
	private readonly List<object> _oldSelectedItems = new List<object>();
	private bool _modifyingSelectionInternally;

	/// <summary>The index a Shift range (Extended mode) is measured from.</summary>
	private int _anchorIndex = -1;

	/// <summary>Initializes a new instance of the <see cref="ListBox"/> class.</summary>
	public ListBox()
	{
		DefaultStyleKey = typeof(ListBox);

		var selectedItems = new ObservableCollection<object>();
		selectedItems.CollectionChanged += OnSelectedItemsCollectionChanged;
		SelectedItems = selectedItems;

		SelectionChanged += OnSelectionChanged;
		AddHandler(KeyDownEvent, new KeyEventHandler(OnListBoxKeyDown), handledEventsToo: false);
	}

	/// <summary>Identifies the <see cref="SelectionMode"/> dependency property.</summary>
	public static DependencyProperty SelectionModeProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionMode),
			typeof(SelectionMode),
			typeof(ListBox),
			new FrameworkPropertyMetadata(SelectionMode.Single, propertyChangedCallback: (s, e) => ((ListBox)s).OnSelectionModeChanged()));

	/// <summary>Gets or sets the selection behavior of the ListBox.</summary>
	public SelectionMode SelectionMode
	{
		get => (SelectionMode)GetValue(SelectionModeProperty);
		set => SetValue(SelectionModeProperty, value);
	}

	/// <summary>Identifies the <see cref="SingleSelectionFollowsFocus"/> dependency property.</summary>
	public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(
			nameof(SingleSelectionFollowsFocus),
			typeof(bool),
			typeof(ListBox),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets a value that indicates whether the selection changes when keyboard focus moves between the items, in
	/// <see cref="SelectionMode.Single"/> mode. The default is true.
	/// </summary>
	public bool SingleSelectionFollowsFocus
	{
		get => (bool)GetValue(SingleSelectionFollowsFocusProperty);
		set => SetValue(SingleSelectionFollowsFocusProperty, value);
	}

	/// <summary>Gets the currently selected items.</summary>
	public IList<object> SelectedItems { get; }

	internal override bool IsSingleSelection => SelectionMode == SelectionMode.Single;

	private bool IsSelectionMultiple => SelectionMode != SelectionMode.Single;

	/// <summary>Causes the ListBox to scroll so that an item is in view.</summary>
	/// <param name="item">The item to show.</param>
	public void ScrollIntoView(object item)
	{
		if (ContainerFromItem(item) is UIElement container)
		{
			container.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
		}
	}

	/// <summary>Selects all the items (Multiple and Extended modes; in Single mode it does nothing).</summary>
	public void SelectAll()
	{
		if (!IsSelectionMultiple)
		{
			return;
		}

		for (var i = 0; i < NumberOfItems; i++)
		{
			SelectInMultipleSelection(ItemFromIndex(i), ContainerFromIndex(i) as SelectorItem);
		}
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new ListBoxAutomationPeer(this);

	/// <inheritdoc />
	protected override bool IsItemItsOwnContainerOverride(object item) => item is ListBoxItem;

	/// <inheritdoc />
	protected override DependencyObject GetContainerForItemOverride() => new ListBoxItem { IsGeneratedContainer = true };

	internal override bool IsSelected(int index)
	{
		if (IsSelectionMultiple)
		{
			var item = ItemFromIndex(index);
			return index >= 0 && SelectedItems.Contains(item);
		}

		return index == SelectedIndex;
	}

	// ---------------------------------------------------------------- pointer

	internal override void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers)
	{
		// Note: the base (single selection only) is not called; each mode handles the click itself.
		var clickedItem = ItemFromIndex(clickedIndex);
		var clickedContainer = ContainerFromIndex(clickedIndex) as SelectorItem;

		switch (SelectionMode)
		{
			case SelectionMode.Single:
				if (ItemsSource is ICollectionView collectionView)
				{
					collectionView.MoveCurrentToPosition(clickedIndex);
					clickedIndex = collectionView.CurrentPosition;
				}

				SelectedIndex = modifiers.HasFlag(VirtualKeyModifiers.Control) && clickedIndex == SelectedIndex ? -1 : clickedIndex;
				_anchorIndex = clickedIndex;
				break;

			case SelectionMode.Multiple:
				FlipSelectionInMultipleSelection(clickedItem, clickedContainer);
				_anchorIndex = clickedIndex;
				break;

			case SelectionMode.Extended:
				if (modifiers.HasFlag(VirtualKeyModifiers.Shift) && _anchorIndex >= 0)
				{
					SelectRange(_anchorIndex, clickedIndex);
				}
				else if (modifiers.HasFlag(VirtualKeyModifiers.Control))
				{
					FlipSelectionInMultipleSelection(clickedItem, clickedContainer);
					_anchorIndex = clickedIndex;
				}
				else
				{
					SelectOnly(clickedIndex);
					_anchorIndex = clickedIndex;
				}

				break;
		}
	}

	// ---------------------------------------------------------------- keyboard

	private void OnListBoxKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (!args.Handled && IsEnabled)
		{
			args.Handled = TryHandleKeyDown(args.Key, args.KeyboardModifiers);
		}
	}

	/// <summary>Handles a key the ListBox navigates or selects with; returns whether it did.</summary>
	/// <param name="key">The key.</param>
	/// <param name="modifiers">The modifier keys held.</param>
	/// <returns>True when the key was handled.</returns>
	internal bool TryHandleKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		var count = NumberOfItems;
		if (count == 0)
		{
			return false;
		}

		var focused = GetFocusedItemIndex();
		var vertical = (ItemsPanelRoot as StackPanel)?.Orientation != Orientation.Horizontal;
		var control = modifiers.HasFlag(VirtualKeyModifiers.Control);

		int target;
		switch (key)
		{
			case VirtualKey.Down when vertical:
			case VirtualKey.Right when !vertical:
				target = focused < 0 ? 0 : focused + 1;
				break;
			case VirtualKey.Up when vertical:
			case VirtualKey.Left when !vertical:
				target = focused < 0 ? 0 : focused - 1;
				break;
			case VirtualKey.Home:
				target = 0;
				break;
			case VirtualKey.End:
				target = count - 1;
				break;
			case VirtualKey.PageDown:
				target = (focused < 0 ? 0 : focused) + GetItemsPerPage();
				break;
			case VirtualKey.PageUp:
				target = (focused < 0 ? 0 : focused) - GetItemsPerPage();
				break;
			case VirtualKey.Space:
				return SelectFocused(focused, control);
			case VirtualKey.A when control && IsSelectionMultiple:
				SelectAll();
				return true;
			default:
				return false;
		}

		target = Math.Clamp(target, 0, count - 1);
		if (target == focused && key is not (VirtualKey.Home or VirtualKey.End))
		{
			// Already at the edge: nothing moves, and the key is left to the page (as WinUI).
			return false;
		}

		MoveFocusAndSelection(target, modifiers);
		return true;
	}

	private bool SelectFocused(int focused, bool control)
	{
		if (focused < 0)
		{
			return false;
		}

		switch (SelectionMode)
		{
			case SelectionMode.Single:
				SelectedIndex = control && focused == SelectedIndex ? -1 : focused;
				break;
			case SelectionMode.Multiple:
				FlipSelectionInMultipleSelection(ItemFromIndex(focused), ContainerFromIndex(focused) as SelectorItem);
				break;
			case SelectionMode.Extended:
				if (control)
				{
					FlipSelectionInMultipleSelection(ItemFromIndex(focused), ContainerFromIndex(focused) as SelectorItem);
				}
				else
				{
					SelectOnly(focused);
				}

				break;
		}

		_anchorIndex = focused;
		return true;
	}

	private void MoveFocusAndSelection(int target, VirtualKeyModifiers modifiers)
	{
		var shift = modifiers.HasFlag(VirtualKeyModifiers.Shift);
		var control = modifiers.HasFlag(VirtualKeyModifiers.Control);

		switch (SelectionMode)
		{
			case SelectionMode.Single:
				if (SingleSelectionFollowsFocus && !control)
				{
					SelectedIndex = target;
				}

				break;

			case SelectionMode.Multiple:
				// The focus moves; Space toggles.
				break;

			case SelectionMode.Extended:
				if (shift)
				{
					SelectRange(_anchorIndex >= 0 ? _anchorIndex : target, target);
				}
				else if (!control)
				{
					SelectOnly(target);
					_anchorIndex = target;
				}

				break;
		}

		if (ContainerFromIndex(target) is SelectorItem container)
		{
			container.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
			container.Focus(FocusState.Keyboard);
		}
	}

	/// <summary>The index of the item whose container has the focus; else the selected one; else -1.</summary>
	private int GetFocusedItemIndex()
	{
		if (XamlRoot is { } root && FocusManager.GetFocusedElement(root) is DependencyObject focused)
		{
			for (var current = focused; current is not null && current != this; current = VisualTreeHelper.GetParent(current))
			{
				if (current is ListBoxItem item && ItemsControlFromItemContainer(item) == this)
				{
					return IndexFromContainer(item);
				}
			}
		}

		return SelectedIndex;
	}

	/// <summary>How many items one page (the ListBox's viewport) holds, at least one.</summary>
	private int GetItemsPerPage()
	{
		if (ContainerFromIndex(0) is FrameworkElement first && first.ActualHeight > 0 && ActualHeight > 0)
		{
			return Math.Max(1, (int)(ActualHeight / first.ActualHeight) - 1);
		}

		return 1;
	}

	// ---------------------------------------------------------------- selection bookkeeping (as ListViewBase)

	private void SelectOnly(int index)
	{
		for (var i = 0; i < NumberOfItems; i++)
		{
			if (i != index)
			{
				UnselectInMultipleSelection(ItemFromIndex(i), ContainerFromIndex(i) as SelectorItem);
			}
		}

		SelectInMultipleSelection(ItemFromIndex(index), ContainerFromIndex(index) as SelectorItem);
	}

	private void SelectRange(int from, int to)
	{
		var lower = Math.Min(from, to);
		var upper = Math.Max(from, to);
		for (var i = 0; i < NumberOfItems; i++)
		{
			var item = ItemFromIndex(i);
			var container = ContainerFromIndex(i) as SelectorItem;
			if (i < lower || i > upper)
			{
				UnselectInMultipleSelection(item, container);
			}
			else
			{
				SelectInMultipleSelection(item, container);
			}
		}
	}

	private void FlipSelectionInMultipleSelection(object item, SelectorItem container)
	{
		var wasSelected = SelectedItems.Remove(item);
		if (!wasSelected)
		{
			SelectedItems.Add(item);
		}

		if (container is not null)
		{
			container.IsSelected = !wasSelected;
		}
	}

	private void SelectInMultipleSelection(object item, SelectorItem container)
	{
		if (!SelectedItems.Contains(item))
		{
			SelectedItems.Add(item);
			if (container is not null)
			{
				container.IsSelected = true;
			}
		}
	}

	private void UnselectInMultipleSelection(object item, SelectorItem container)
	{
		if (SelectedItems.Remove(item) && container is not null)
		{
			container.IsSelected = false;
		}
	}

	private void SetSelectedState(int index, bool selected)
	{
		if (ContainerFromIndex(index) is SelectorItem selectorItem)
		{
			selectorItem.IsSelected = selected;
		}
	}

	private void OnSelectionModeChanged()
	{
		_anchorIndex = -1;
		SelectedIndex = -1;

		foreach (var item in SelectedItems.ToList())
		{
			SetSelectedState(IndexFromItem(item), false);
		}

		SelectedItems.Clear();
	}

	private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (_modifyingSelectionInternally)
		{
			_oldSelectedItems.Clear();
			_oldSelectedItems.AddRange(SelectedItems);
			return;
		}

		var items = GetItems();
		if (items == null)
		{
			_oldSelectedItems.Clear();
			return;
		}

		object[] additions;
		object[] removals;
		if (e.Action != NotifyCollectionChangedAction.Reset)
		{
			additions = (e.NewItems?.Cast<object>() ?? Array.Empty<object>()).Where(item => items.Contains(item)).ToArray();
			removals = (e.OldItems?.Cast<object>() ?? Array.Empty<object>()).Where(item => items.Contains(item)).ToArray();
		}
		else
		{
			additions = Array.Empty<object>();
			removals = _oldSelectedItems.Where(item => items.Contains(item)).ToArray();
		}

		try
		{
			_modifyingSelectionInternally = true;
			_isUpdatingSelection = true;

			var first = SelectedItems.Select(item => items.IndexOf(item)).FirstOrDefault(index => index > -1, -1);
			if (first >= 0)
			{
				SelectedItem = items.ElementAt(first);
				SelectedIndex = first;
			}
			else
			{
				SelectedItem = null;
				SelectedIndex = -1;
			}

			foreach (var removed in removals)
			{
				TryUpdateSelectorItemIsSelected(removed, false);
			}

			foreach (var added in additions)
			{
				TryUpdateSelectorItemIsSelected(added, true);
			}
		}
		finally
		{
			_modifyingSelectionInternally = false;
			_isUpdatingSelection = false;
		}

		if (additions.Length != 0 || removals.Length != 0)
		{
			InvokeSelectionChanged(removals, additions);
		}

		_oldSelectedItems.Clear();
		_oldSelectedItems.AddRange(SelectedItems);
	}

	internal override void ChangeSelectedItem(object item, bool oldIsSelected, bool newIsSelected)
	{
		if (_modifyingSelectionInternally)
		{
			return;
		}

		if (!IsSelectionMultiple)
		{
			if (_changingSelectedIndex)
			{
				return;
			}

			var index = IndexFromItem(item);
			if (!newIsSelected)
			{
				if (SelectedIndex == index)
				{
					SelectedIndex = -1;
				}
			}
			else
			{
				SelectedIndex = index;
			}
		}
		else if (!newIsSelected)
		{
			SelectedItems.Remove(item);
		}
		else if (!SelectedItems.Contains(item))
		{
			SelectedItems.Add(item);
		}
	}

	internal override void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState)
	{
		if (_modifyingSelectionInternally)
		{
			return;
		}

		if (IsSelectionMultiple)
		{
			// Setting SelectedItem in a multiple mode makes it the one selected item.
			var items = GetItems();
			if (selectedItem == null || items.Contains(selectedItem))
			{
				object[] removed;
				object[] added;
				try
				{
					_modifyingSelectionInternally = true;
					removed = SelectedItems.Where(item => !Equals(item, selectedItem)).ToArray();
					var isRealSelection = selectedItem != null || items.Contains(null);
					added = SelectedItems.Contains(selectedItem) || !isRealSelection ? Array.Empty<object>() : new[] { selectedItem };
					SelectedItems.Clear();
					if (isRealSelection)
					{
						SelectedItems.Add(selectedItem);
					}
				}
				finally
				{
					_modifyingSelectionInternally = false;
				}

				foreach (var item in removed)
				{
					SetSelectedState(IndexFromItem(item), false);
				}

				foreach (var item in added)
				{
					SetSelectedState(IndexFromItem(item), true);
				}

				if (added.Length > 0 || removed.Length > 0)
				{
					InvokeSelectionChanged(removed, added);
				}
			}
			else
			{
				SelectedItem = oldSelectedItem;
			}

			return;
		}

		try
		{
			_modifyingSelectionInternally = true;
			SelectedItems.Clear();
			if (selectedItem != null)
			{
				SelectedItems.Add(selectedItem);
			}
		}
		finally
		{
			_modifyingSelectionInternally = false;
		}

		base.OnSelectedItemChanged(oldSelectedItem, selectedItem, updateItemSelectedState: true);
	}

	internal override void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
	{
		base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);

		try
		{
			_changingSelectedIndex = true;
			SetSelectedState(oldSelectedIndex, false);
			SetSelectedState(newSelectedIndex, true);
		}
		finally
		{
			_changingSelectedIndex = false;
		}
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		// In Single mode the containers follow SelectedIndex (more reliable with duplicate items).
		if (IsSelectionMultiple)
		{
			if (e.AddedItems is not null)
			{
				foreach (var item in e.AddedItems)
				{
					SetSelectedState(IndexFromItem(item), true);
				}
			}

			if (e.RemovedItems is not null)
			{
				foreach (var item in e.RemovedItems)
				{
					SetSelectedState(IndexFromItem(item), false);
				}
			}
		}
	}
}
