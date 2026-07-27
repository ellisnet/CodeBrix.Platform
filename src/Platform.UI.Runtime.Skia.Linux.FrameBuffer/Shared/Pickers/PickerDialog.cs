// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia.Pickers;

/// <summary>
/// The in-application modal file/folder dialog. Rendered in the XAML popup layer,
/// which on this head is composited above ALL application content (web views,
/// 3D-rendered views, game views — there is no native z-order to escape into),
/// always within the application frame, and rotated with the application. A
/// full-size smoke layer swallows pointer input behind the dialog; the software
/// keyboard is the deliberate exception to that modality, and the dialog moves
/// itself clear of the keyboard whenever one is showing. The chrome is styled by
/// ContentDialog's theme resources (see <see cref="DialogThemeResources"/>), so
/// it looks like — and restyles with — the framework's own dialogs.
/// </summary>
internal sealed class PickerDialog
{
	internal enum PickerMode
	{
		OpenFile,
		SaveFile,
		PickFolder,
	}

	// Fallbacks for the ContentDialog theme resources that drive the dialog chrome
	// (see DialogThemeResources): the standard Fluent light-theme values each key
	// carries, used only when a key cannot be resolved.
	private static readonly Color LightDismissFallback = Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
	private static readonly Color SmokeFillFallback = Color.FromArgb(0x4D, 0x00, 0x00, 0x00);
	private static readonly Color BackgroundFallback = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3);
	private static readonly Color TopOverlayFallback = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
	private static readonly Color SeparatorFallback = Color.FromArgb(0x0F, 0x00, 0x00, 0x00);
	private static readonly Color BorderFallback = Color.FromArgb(0x66, 0x75, 0x75, 0x75);
	private static readonly Color ForegroundFallback = Color.FromArgb(0xE4, 0x00, 0x00, 0x00);
	private static readonly Color SecondaryTextFallback = Color.FromArgb(0x9E, 0x00, 0x00, 0x00);
	private static readonly Color FolderGlyphColor = Color.FromArgb(0xFF, 0xE8, 0xA8, 0x33);
	private static readonly Color FileGlyphColor = Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A);

	private readonly Brush _secondaryTextBrush =
		DialogThemeResources.Brush("TextFillColorSecondaryBrush", SecondaryTextFallback);

	private readonly PickerMode _mode;
	private readonly PickerNavigator _navigator;
	private readonly bool _allowMultiple;
	private readonly bool _allowNewFolder;
	private readonly string _commitText;
	private readonly TaskCompletionSource<IReadOnlyList<string>> _completion = new();

	private readonly Popup _popup = new();
	private readonly Grid _overlay = new();
	private readonly Border _dialog = new();
	private readonly TextBlock _title = new();
	private readonly TextBlock _pathText = new();
	private readonly Button _upButton = new();
	private readonly Button _newFolderButton = new();
	private readonly ListView _list = new();
	private readonly TextBlock _emptyHint = new();
	private readonly StackPanel _saveNameRow = new();
	private readonly TextBox _saveNameBox = new();
	private readonly StackPanel _newFolderRow = new();
	private readonly TextBox _newFolderNameBox = new();
	private readonly StackPanel _overwriteRow = new();
	private readonly TextBlock _overwriteText = new();
	private readonly Button _commitButton = new();

	private XamlRoot? _xamlRoot;
	private string? _pendingOverwritePath;
	private bool _suppressSelectionChanged;

	private PickerDialog(PickerMode mode, PickerNavigator navigator, bool allowMultiple,
		bool allowNewFolder, string commitText, string? suggestedFileName)
	{
		_mode = mode;
		_navigator = navigator;
		_allowMultiple = allowMultiple;
		_allowNewFolder = allowNewFolder;
		_commitText = commitText;
		BuildVisualTree(suggestedFileName);
	}

	/// <summary>
	/// Shows the dialog and completes with the chosen full paths — empty on cancel,
	/// at most one entry except for multi-select open.
	/// </summary>
	internal static Task<IReadOnlyList<string>> ShowAsync(PickerMode mode, PickerNavigator navigator,
		bool allowMultiple, bool allowNewFolder, string commitText, string? suggestedFileName,
		CancellationToken token)
	{
		var dialog = new PickerDialog(mode, navigator, allowMultiple, allowNewFolder, commitText, suggestedFileName);
		return dialog.OpenAsync(token);
	}

	private Task<IReadOnlyList<string>> OpenAsync(CancellationToken token)
	{
		_xamlRoot = Window.InitialWindow?.Content?.XamlRoot;
		if (_xamlRoot is null)
		{
			_completion.TrySetResult([]);
			return _completion.Task;
		}

		_popup.XamlRoot = _xamlRoot;
		_popup.Child = _overlay;
		ApplyRootMetrics();
		RefreshEntries();

		_xamlRoot.Changed += OnXamlRootChanged;
		var inputPane = Windows.UI.ViewManagement.InputPane.GetForCurrentView();
		inputPane.Showing += OnInputPaneChanged;
		inputPane.Hiding += OnInputPaneChanged;

		var registration = token.CanBeCanceled
			? token.Register(() => _ = _overlay.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal, () => Complete([])))
			: default;
		_ = _completion.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);

		_popup.IsOpen = true;
		if (_mode == PickerMode.SaveFile)
		{
			// Deferred so the popup's content is loaded and focusable — this also
			// summons the software keyboard when one is enabled.
			_ = _saveNameBox.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					_saveNameBox.Focus(FocusState.Programmatic);
					_saveNameBox.SelectAll();
				});
		}
		return _completion.Task;
	}

	private void Complete(IReadOnlyList<string> result)
	{
		if (_completion.Task.IsCompleted)
		{
			return;
		}
		_popup.IsOpen = false;
		if (_xamlRoot is not null)
		{
			_xamlRoot.Changed -= OnXamlRootChanged;
		}
		var inputPane = Windows.UI.ViewManagement.InputPane.GetForCurrentView();
		inputPane.Showing -= OnInputPaneChanged;
		inputPane.Hiding -= OnInputPaneChanged;
		_completion.TrySetResult(result);
	}

	private void BuildVisualTree(string? suggestedFileName)
	{
		// The chrome resolves ContentDialog's theme resources at open, so the dialog
		// renders — and can be restyled — exactly like ContentDialog: the app's
		// active theme and any ContentDialog resource overrides apply here too.
		var background = DialogThemeResources.Brush("ContentDialogBackground", BackgroundFallback);

		// ContentDialog grays the application out with two stacked layers: its
		// popup's light-dismiss overlay under its smoke fill. Both are replicated
		// here, and the overlay eats every pointer event not meant for the dialog.
		_overlay.Background = DialogThemeResources.Brush(
			"ContentDialogLightDismissOverlayBackground", LightDismissFallback);
		_overlay.Children.Add(new Border
		{
			Background = DialogThemeResources.Brush("ContentDialogSmokeFill", SmokeFillFallback),
		});
		_overlay.PointerPressed += (_, e) => e.Handled = true;
		_overlay.PointerReleased += (_, e) => e.Handled = true;
		_overlay.Tapped += (_, e) => e.Handled = true;

		_dialog.Background = background;
		_dialog.BorderBrush = DialogThemeResources.Brush("ContentDialogBorderBrush", BorderFallback);
		_dialog.BorderThickness = new Thickness(1);
		_dialog.CornerRadius = new CornerRadius(8);
		_dialog.HorizontalAlignment = HorizontalAlignment.Center;
		_dialog.VerticalAlignment = VerticalAlignment.Center;
		_overlay.Children.Add(_dialog);

		// ContentDialog's two-surface shape: white content section over the gray
		// command strip, with the buttons living in the strip.
		var sections = new Grid();
		sections.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		sections.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		_dialog.Child = sections;

		var contentSection = new Border
		{
			Background = DialogThemeResources.Brush("ContentDialogTopOverlay", TopOverlayFallback),
			BorderBrush = DialogThemeResources.Brush("ContentDialogSeparatorBorderBrush", SeparatorFallback),
			BorderThickness = new Thickness(0, 0, 0, 1),
			CornerRadius = new CornerRadius(8, 8, 0, 0),
			Padding = new Thickness(16),
		};
		Grid.SetRow(contentSection, 0);
		sections.Children.Add(contentSection);

		var layout = new Grid { RowSpacing = 8 };
		for (var row = 0; row < 7; row++)
		{
			layout.RowDefinitions.Add(new RowDefinition
			{
				Height = row == 4 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto,
			});
		}
		contentSection.Child = layout;

		_title.Text = _mode switch
		{
			PickerMode.OpenFile => PlatformStrings.OpenFileTitle,
			PickerMode.SaveFile => PlatformStrings.SaveFileTitle,
			_ => PlatformStrings.SelectFolderTitle,
		};
		// ContentDialog's title typography (its template's hard-coded values).
		_title.FontSize = 20;
		_title.FontWeight = Windows.UI.Text.FontWeights.SemiBold;
		_title.Foreground = DialogThemeResources.Brush("ContentDialogForeground", ForegroundFallback);
		Grid.SetRow(_title, 0);
		layout.Children.Add(_title);

		_pathText.FontSize = 12;
		_pathText.Foreground = _secondaryTextBrush;
		_pathText.TextTrimming = TextTrimming.CharacterEllipsis;
		Grid.SetRow(_pathText, 1);
		layout.Children.Add(_pathText);

		var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_upButton.Content = "↑ Up";
		_upButton.Click += (_, _) =>
		{
			_navigator.NavigateUp();
			RefreshEntries();
		};
		toolbar.Children.Add(_upButton);
		if (_allowNewFolder)
		{
			_newFolderButton.Content = PlatformStrings.NewFolder;
			_newFolderButton.Click += (_, _) =>
			{
				_newFolderNameBox.Text = "";
				_newFolderRow.Visibility = Visibility.Visible;
				_newFolderNameBox.Focus(FocusState.Programmatic);
			};
			toolbar.Children.Add(_newFolderButton);
		}
		Grid.SetRow(toolbar, 2);
		layout.Children.Add(toolbar);

		BuildNewFolderRow();
		Grid.SetRow(_newFolderRow, 3);
		layout.Children.Add(_newFolderRow);

		var listHost = new Grid();
		_list.SelectionMode = _mode == PickerMode.OpenFile && _allowMultiple
			? ListViewSelectionMode.Multiple
			: ListViewSelectionMode.Single;
		_list.SelectionChanged += OnSelectionChanged;
		_list.MinHeight = 160;
		listHost.Children.Add(_list);
		_emptyHint.Text = PlatformStrings.NoItems;
		_emptyHint.Foreground = _secondaryTextBrush;
		_emptyHint.HorizontalAlignment = HorizontalAlignment.Center;
		_emptyHint.VerticalAlignment = VerticalAlignment.Center;
		listHost.Children.Add(_emptyHint);
		Grid.SetRow(listHost, 4);
		layout.Children.Add(listHost);

		if (_mode == PickerMode.SaveFile)
		{
			_saveNameRow.Orientation = Orientation.Horizontal;
			_saveNameRow.Spacing = 8;
			var nameLabel = new TextBlock { Text = PlatformStrings.NameLabel, VerticalAlignment = VerticalAlignment.Center };
			_saveNameBox.MinWidth = 220;
			_saveNameBox.Text = suggestedFileName ?? "";
			_saveNameBox.TextChanged += (_, _) => UpdateCommitEnabled();
			_saveNameRow.Children.Add(nameLabel);
			_saveNameRow.Children.Add(_saveNameBox);
			Grid.SetRow(_saveNameRow, 5);
			layout.Children.Add(_saveNameRow);
		}

		BuildOverwriteRow();
		Grid.SetRow(_overwriteRow, 6);
		layout.Children.Add(_overwriteRow);

		var commandStrip = new Border
		{
			Background = background,
			CornerRadius = new CornerRadius(0, 0, 8, 8),
			Padding = new Thickness(16),
		};
		Grid.SetRow(commandStrip, 1);
		sections.Children.Add(commandStrip);

		var buttons = new Grid();
		buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
		buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_commitButton.Content = _commitText;
		_commitButton.HorizontalAlignment = HorizontalAlignment.Stretch;
		_commitButton.Click += (_, _) => Commit();
		var cancelButton = new Button
		{
			Content = PlatformStrings.Cancel,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		cancelButton.Click += (_, _) => Complete([]);
		Grid.SetColumn(cancelButton, 2);
		buttons.Children.Add(_commitButton);
		buttons.Children.Add(cancelButton);
		commandStrip.Child = buttons;
	}

	private void BuildNewFolderRow()
	{
		_newFolderRow.Orientation = Orientation.Horizontal;
		_newFolderRow.Spacing = 8;
		_newFolderRow.Visibility = Visibility.Collapsed;
		_newFolderNameBox.MinWidth = 180;
		_newFolderNameBox.PlaceholderText = PlatformStrings.FolderNamePlaceholder;
		var createButton = new Button { Content = PlatformStrings.Create };
		createButton.Click += (_, _) =>
		{
			if (_navigator.CreateFolder(_newFolderNameBox.Text) is { } created)
			{
				_newFolderRow.Visibility = Visibility.Collapsed;
				_navigator.NavigateInto(created);
				RefreshEntries();
			}
		};
		var dismissButton = new Button { Content = PlatformStrings.Cancel };
		dismissButton.Click += (_, _) => _newFolderRow.Visibility = Visibility.Collapsed;
		_newFolderRow.Children.Add(_newFolderNameBox);
		_newFolderRow.Children.Add(createButton);
		_newFolderRow.Children.Add(dismissButton);
	}

	private void BuildOverwriteRow()
	{
		_overwriteRow.Orientation = Orientation.Horizontal;
		_overwriteRow.Spacing = 8;
		_overwriteRow.Visibility = Visibility.Collapsed;
		_overwriteText.VerticalAlignment = VerticalAlignment.Center;
		var replaceButton = new Button { Content = PlatformStrings.Replace };
		replaceButton.Click += (_, _) =>
		{
			if (_pendingOverwritePath is { } path)
			{
				Complete([path]);
			}
		};
		var keepButton = new Button { Content = PlatformStrings.KeepEditing };
		keepButton.Click += (_, _) =>
		{
			_pendingOverwritePath = null;
			_overwriteRow.Visibility = Visibility.Collapsed;
		};
		_overwriteRow.Children.Add(_overwriteText);
		_overwriteRow.Children.Add(replaceButton);
		_overwriteRow.Children.Add(keepButton);
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_suppressSelectionChanged)
		{
			return;
		}
		if (e.AddedItems.OfType<ListViewItem>().FirstOrDefault()?.Tag is PickerNavigator.Entry entry)
		{
			if (entry.IsFolder)
			{
				// Deferred: rebuilding the list from inside its own selection event
				// would mutate the collection mid-raise.
				_ = _list.Dispatcher.RunAsync(
					Windows.UI.Core.CoreDispatcherPriority.Normal,
					() =>
					{
						if (_navigator.NavigateInto(entry))
						{
							RefreshEntries();
						}
					});
				return;
			}
			if (_mode == PickerMode.SaveFile)
			{
				// Tapping an existing file adopts its name.
				_saveNameBox.Text = entry.Name;
			}
		}
		UpdateCommitEnabled();
	}

	private void RefreshEntries()
	{
		_suppressSelectionChanged = true;
		_list.Items.Clear();
		foreach (var entry in _navigator.GetEntries(foldersOnly: _mode == PickerMode.PickFolder))
		{
			_list.Items.Add(new ListViewItem
			{
				Content = BuildEntryVisual(entry),
				Tag = entry,
				MinHeight = 40,
			});
		}
		_suppressSelectionChanged = false;

		_emptyHint.Visibility = _list.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		_pathText.Text = _navigator.DisplayPath;
		_upButton.IsEnabled = _navigator.CanNavigateUp;
		_pendingOverwritePath = null;
		_overwriteRow.Visibility = Visibility.Collapsed;
		UpdateCommitEnabled();
	}

	private static UIElement BuildEntryVisual(PickerNavigator.Entry entry)
	{
		var panel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalAlignment = VerticalAlignment.Center,
		};
		panel.Children.Add(BuildEntryGlyph(entry.IsFolder));
		panel.Children.Add(new TextBlock
		{
			Text = entry.Name,
			FontSize = 14,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
		});
		return panel;
	}

	// Self-contained vector glyphs: no dependence on any icon font being present.
	private static UIElement BuildEntryGlyph(bool isFolder)
	{
		var geometry = new GeometryGroup();
		if (isFolder)
		{
			geometry.Children.Add(new RectangleGeometry { Rect = new Rect(0, 0, 7, 4) });
			geometry.Children.Add(new RectangleGeometry { Rect = new Rect(0, 2, 16, 11) });
		}
		else
		{
			geometry.Children.Add(new RectangleGeometry { Rect = new Rect(2, 0, 12, 15) });
		}
		return new Microsoft.UI.Xaml.Shapes.Path
		{
			Data = geometry,
			Fill = new SolidColorBrush(isFolder ? FolderGlyphColor : FileGlyphColor),
			Width = 16,
			Height = 16,
			VerticalAlignment = VerticalAlignment.Center,
		};
	}

	private void UpdateCommitEnabled()
		=> _commitButton.IsEnabled = _mode switch
		{
			PickerMode.OpenFile => SelectedFilePaths().Count > 0,
			PickerMode.SaveFile => PickerNavigator.IsValidEntryName(_saveNameBox.Text),
			_ => true,
		};

	private List<string> SelectedFilePaths()
		=> _list.SelectedItems
			.OfType<ListViewItem>()
			.Select(item => item.Tag)
			.OfType<PickerNavigator.Entry>()
			.Where(entry => !entry.IsFolder)
			.Select(entry => entry.FullPath)
			.ToList();

	private void Commit()
	{
		switch (_mode)
		{
			case PickerMode.OpenFile:
				var files = SelectedFilePaths();
				if (files.Count > 0)
				{
					Complete(_allowMultiple ? files : [files[0]]);
				}
				break;

			case PickerMode.SaveFile:
				if (_navigator.ResolveSaveTarget(_saveNameBox.Text) is { } target)
				{
					if (System.IO.File.Exists(target))
					{
						_pendingOverwritePath = target;
						_overwriteText.Text = PlatformStrings.ReplaceFile(System.IO.Path.GetFileName(target));
						_overwriteRow.Visibility = Visibility.Visible;
					}
					else
					{
						Complete([target]);
					}
				}
				break;

			default:
				Complete([_navigator.CurrentPath]);
				break;
		}
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => ApplyRootMetrics();

	private void OnInputPaneChanged(Windows.UI.ViewManagement.InputPane sender,
		Windows.UI.ViewManagement.InputPaneVisibilityEventArgs args)
	{
		args.EnsuredFocusedElementInView = true;
		ApplyRootMetrics();
	}

	// The dialog always fits the application frame, and — since popups are NOT
	// part of the resized content — steps clear of the software keyboard itself
	// by keeping the keyboard's occluded strip as bottom padding.
	private void ApplyRootMetrics()
	{
		if (_xamlRoot is null)
		{
			return;
		}
		var size = _xamlRoot.Size;
		var keyboardHeight = Windows.UI.ViewManagement.InputPane.GetForCurrentView().OccludedRect.Height;
		_overlay.Width = size.Width;
		_overlay.Height = size.Height;
		// Margin on the dialog (not padding on the overlay) so the gray-out layers
		// keep covering the keyboard's strip too.
		_dialog.Margin = new Thickness(0, 0, 0, Math.Min(keyboardHeight, Math.Max(0, size.Height - 120)));
		_dialog.Width = Math.Max(280, Math.Min(size.Width - 48, 560));
		_dialog.MaxHeight = Math.Max(220, size.Height - keyboardHeight - 48);
	}
}
