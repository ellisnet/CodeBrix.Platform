// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// The on-screen keyboard's visual: a light-themed strip of pointer-driven keys.
/// Keys are plain Borders — deliberately NOT buttons — so tapping one can never
/// move focus away from the text control being typed into. Design follows the
/// established mobile conventions (AOSP LatinIME / FlorisBoard; Apache-2.0,
/// design reference only): three pages (letters, two symbol pages), a three-state
/// shift, long-press alternates for accents and AltGr characters, and a
/// layout-cycling key when more than one layout is enabled.
/// </summary>
internal sealed class SoftwareKeyboardView : Border
{
	private const string DigitsRow = "1234567890";
	private static readonly string[] Symbols1Rows = ["1234567890", "@#€$%&-_+()", "*\"':;!?"];
	private static readonly string[] Symbols2Rows = ["~`|•√π÷×¶", "£¥¢^°={}§", "\\©®™%[]"];

	private static readonly Color StripBackground = Color.FromArgb(0xFF, 0xEC, 0xEC, 0xEE);
	private static readonly Color KeyBackground = Colors.White;
	private static readonly Color SpecialKeyBackground = Color.FromArgb(0xFF, 0xD4, 0xD6, 0xDA);
	private static readonly Color PressedKeyBackground = Color.FromArgb(0xFF, 0xB8, 0xD4, 0xF0);
	private static readonly Color KeyForeground = Color.FromArgb(0xFF, 0x20, 0x20, 0x20);

	private enum KeyKind
	{
		Character,
		Shift,
		Backspace,
		Enter,
		Space,
		SymbolsPage,
		SymbolsPage2,
		LettersPage,
		Globe,
		Tab,
		ArrowLeft,
		ArrowRight,
		ArrowUp,
		ArrowDown,
	}

	private enum ShiftState
	{
		Off,
		Once,
		Locked,
	}

	private enum Page
	{
		Letters,
		Symbols1,
		Symbols2,
	}

	private sealed record KeyDef(KeyKind Kind, float Weight, string? Legend = null,
		char? Character = null, string? Alternates = null);

	private readonly ISoftwareKeyInjector _injector;
	private readonly IReadOnlyList<KeyboardLayoutDefinition> _layouts;
	private readonly Grid _rows = new();
	private readonly DispatcherTimer _longPressTimer = new();
	private readonly DispatcherTimer _repeatTimer = new();
	private Popup? _alternatesPopup;

	private int _activeLayoutIndex;
	private Page _page = Page.Letters;
	private ShiftState _shift = ShiftState.Off;
	private DateTimeOffset _lastShiftTap = DateTimeOffset.MinValue;
	private double _keyboardWidth;
	private double _keyboardHeight;
	private Border? _pressedKey;
	private KeyDef? _pressedDef;
	private bool _longPressFired;

	internal SoftwareKeyboardView(ISoftwareKeyInjector injector, IReadOnlyList<KeyboardLayoutDefinition> layouts)
	{
		_injector = injector;
		_layouts = layouts;

		RequestedTheme = ElementTheme.Light;
		Background = new SolidColorBrush(StripBackground);
		BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0));
		BorderThickness = new Thickness(0, 1, 0, 0);
		Child = _rows;

		// The strip itself swallows pointer input so a stray tap between keys can
		// neither reach the application nor steal focus from the text control.
		PointerPressed += (_, e) => e.Handled = true;
		PointerReleased += (_, e) => e.Handled = true;

		_longPressTimer.Interval = TimeSpan.FromMilliseconds(450);
		_longPressTimer.Tick += (_, _) => OnLongPress();
		_repeatTimer.Interval = TimeSpan.FromMilliseconds(400);
		_repeatTimer.Tick += (_, _) => OnBackspaceRepeat();
	}

	private KeyboardLayoutDefinition ActiveLayout => _layouts[_activeLayoutIndex];

	/// <summary>
	/// The keyboard strip height for a given logical root size: a per-orientation
	/// fraction bounded so keys stay comfortably tappable on the smallest panel
	/// and the strip never dominates the largest.
	/// </summary>
	internal static double ComputeHeight(Size rootSize)
		=> rootSize.Height > rootSize.Width
			? Math.Clamp(rootSize.Height * 0.40, 200, 400)
			: Math.Clamp(rootSize.Height * 0.42, 190, 340);

	/// <summary>Sizes the strip and (re)builds its keys for the current state.</summary>
	internal void ApplyMetrics(double width, double height)
	{
		_keyboardWidth = width;
		_keyboardHeight = height;
		Width = width;
		Height = height;
		Rebuild();
	}

	private void Rebuild()
	{
		CloseAlternates();
		_rows.Children.Clear();
		_rows.RowDefinitions.Clear();
		_rows.Padding = new Thickness(3);

		var rows = BuildPageRows();
		foreach (var _ in rows)
		{
			_rows.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		}
		for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
		{
			var rowGrid = new Grid();
			foreach (var key in rows[rowIndex])
			{
				rowGrid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(key.Weight, GridUnitType.Star),
				});
			}
			for (var keyIndex = 0; keyIndex < rows[rowIndex].Count; keyIndex++)
			{
				var visual = BuildKeyVisual(rows[rowIndex][keyIndex]);
				Grid.SetColumn(visual, keyIndex);
				rowGrid.Children.Add(visual);
			}
			Grid.SetRow(rowGrid, rowIndex);
			_rows.Children.Add(rowGrid);
		}
	}

	private List<List<KeyDef>> BuildPageRows()
		=> _page switch
		{
			Page.Letters => BuildLettersRows(),
			Page.Symbols1 => BuildSymbolsRows(Symbols1Rows, secondPage: false),
			_ => BuildSymbolsRows(Symbols2Rows, secondPage: true),
		};

	private List<List<KeyDef>> BuildLettersRows()
	{
		var layout = ActiveLayout;
		var shifted = _shift != ShiftState.Off;
		var rows = new List<List<KeyDef>>
		{
			DigitsRow.Select(digit => new KeyDef(KeyKind.Character, 1f, digit.ToString(), digit)).ToList(),
		};

		for (var rowIndex = 0; rowIndex < layout.Rows.Length; rowIndex++)
		{
			var row = new List<KeyDef>();
			var baseRow = layout.Rows[rowIndex];
			var isLastRow = rowIndex == layout.Rows.Length - 1;
			if (isLastRow)
			{
				row.Add(new KeyDef(KeyKind.Shift, 1.5f));
			}
			for (var keyIndex = 0; keyIndex < baseRow.Length; keyIndex++)
			{
				var baseChar = baseRow[keyIndex];
				var character = shifted ? ShiftOf(layout, rowIndex, keyIndex, baseChar) : baseChar;
				row.Add(new KeyDef(KeyKind.Character, 1f, character.ToString(), character,
					AlternatesOf(layout, rowIndex, keyIndex, baseChar, shifted)));
			}
			if (isLastRow)
			{
				row.Add(new KeyDef(KeyKind.Backspace, 1.5f));
			}
			rows.Add(row);
		}

		rows.Add(BuildBottomRow(lettersPage: true));
		return rows;
	}

	private List<List<KeyDef>> BuildSymbolsRows(string[] symbolRows, bool secondPage)
	{
		var rows = symbolRows
			.Select(symbolRow => symbolRow
				.Select(symbol => new KeyDef(KeyKind.Character, 1f, symbol.ToString(), symbol))
				.ToList())
			.ToList();

		// The third symbols row is flanked by the page toggle and backspace.
		rows[2].Insert(0, new KeyDef(secondPage ? KeyKind.SymbolsPage : KeyKind.SymbolsPage2, 1.5f,
			secondPage ? "?123" : "=\\<"));
		rows[2].Add(new KeyDef(KeyKind.Backspace, 1.5f));

		rows.Add(BuildBottomRow(lettersPage: false, withArrows: secondPage));
		return rows;
	}

	private List<KeyDef> BuildBottomRow(bool lettersPage, bool withArrows = false)
	{
		var row = new List<KeyDef>
		{
			lettersPage
				? new KeyDef(KeyKind.SymbolsPage, 1.5f, "?123")
				: new KeyDef(KeyKind.LettersPage, 1.5f, "ABC"),
		};
		if (_layouts.Count > 1)
		{
			row.Add(new KeyDef(KeyKind.Globe, 1f, ActiveLayout.Id.ToUpperInvariant()));
		}
		if (withArrows)
		{
			row.Add(new KeyDef(KeyKind.Tab, 1f, "Tab"));
			row.Add(new KeyDef(KeyKind.ArrowLeft, 1f, "←"));
			row.Add(new KeyDef(KeyKind.ArrowUp, 1f, "↑"));
			row.Add(new KeyDef(KeyKind.ArrowDown, 1f, "↓"));
			row.Add(new KeyDef(KeyKind.ArrowRight, 1f, "→"));
		}
		else
		{
			row.Add(new KeyDef(KeyKind.Character, 1f, ",", ','));
			row.Add(new KeyDef(KeyKind.Space, 4f));
			row.Add(new KeyDef(KeyKind.Character, 1f, ".", '.'));
		}
		row.Add(new KeyDef(KeyKind.Enter, 1.5f, "Enter"));
		return row;
	}

	private static char ShiftOf(KeyboardLayoutDefinition layout, int rowIndex, int keyIndex, char baseChar)
		=> layout.ShiftRows is { } shiftRows && rowIndex < shiftRows.Length && keyIndex < shiftRows[rowIndex].Length
			? shiftRows[rowIndex][keyIndex]
			: char.ToUpperInvariant(baseChar);

	private static string? AlternatesOf(KeyboardLayoutDefinition layout, int rowIndex, int keyIndex,
		char baseChar, bool shifted)
	{
		var alternates = "";
		// The AltGr level is presented as long-press alternates: no hardware AltGr
		// exists on a touch panel.
		if (layout.AltGrRows is { } altGrRows && rowIndex < altGrRows.Length && keyIndex < altGrRows[rowIndex].Length
			&& altGrRows[rowIndex][keyIndex] is var altGr && altGr != ' ')
		{
			alternates += altGr;
		}
		if (layout.LongPress is { } longPress && longPress.TryGetValue(baseChar, out var extra))
		{
			foreach (var alternate in extra)
			{
				if (!alternates.Contains(alternate))
				{
					alternates += alternate;
				}
			}
		}
		if (alternates.Length == 0)
		{
			return null;
		}
		return shifted ? string.Concat(alternates.Select(char.ToUpperInvariant)) : alternates;
	}

	private Border BuildKeyVisual(KeyDef key)
	{
		var special = key.Kind is not (KeyKind.Character or KeyKind.Space);
		var legend = key.Kind switch
		{
			KeyKind.Shift => _shift switch
			{
				ShiftState.Off => "Shift",
				ShiftState.Once => "Shift ●",
				_ => "SHIFT",
			},
			// Word legends stay within the glyph coverage of every bundled
			// application font; symbol glyphs for these can come once a dedicated
			// keyboard font is settled.
			KeyKind.Backspace => "Bksp",
			KeyKind.Space => ActiveLayout.DisplayName,
			_ => key.Legend ?? "",
		};

		var text = new TextBlock
		{
			Text = legend,
			Foreground = new SolidColorBrush(key.Kind == KeyKind.Space
				? Color.FromArgb(0xFF, 0x90, 0x90, 0x90)
				: KeyForeground),
			FontSize = key.Kind == KeyKind.Character ? 18 : 13,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};

		var visual = new Border
		{
			Background = new SolidColorBrush(special ? SpecialKeyBackground : KeyBackground),
			CornerRadius = new CornerRadius(5),
			Margin = new Thickness(2.5),
			Child = text,
			Tag = key,
		};

		visual.PointerPressed += OnKeyPressed;
		visual.PointerReleased += OnKeyReleased;
		visual.PointerExited += OnKeyAborted;
		visual.PointerCaptureLost += OnKeyAborted;
		return visual;
	}

	private void OnKeyPressed(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		if (sender is not Border { Tag: KeyDef key } visual)
		{
			return;
		}
		CloseAlternates();
		visual.CapturePointer(e.Pointer);
		_pressedKey = visual;
		_pressedDef = key;
		_longPressFired = false;
		visual.Background = new SolidColorBrush(PressedKeyBackground);

		if (key.Kind == KeyKind.Character && key.Alternates is not null)
		{
			_longPressTimer.Start();
		}
		else if (key.Kind == KeyKind.Backspace)
		{
			InjectSpecial(VirtualKey.Back);
			_repeatTimer.Interval = TimeSpan.FromMilliseconds(400);
			_repeatTimer.Start();
		}
	}

	private void OnKeyReleased(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;
		if (sender is not Border visual || !ReferenceEquals(visual, _pressedKey) || _pressedDef is not { } key)
		{
			ReleaseVisual(sender as Border);
			return;
		}
		var longPressFired = _longPressFired;
		ReleaseVisual(visual);
		if (!longPressFired)
		{
			CommitKey(key);
		}
	}

	private void OnKeyAborted(object sender, PointerRoutedEventArgs e)
	{
		if (ReferenceEquals(sender, _pressedKey))
		{
			ReleaseVisual(_pressedKey);
		}
	}

	private void ReleaseVisual(Border? visual)
	{
		_longPressTimer.Stop();
		_repeatTimer.Stop();
		if (visual is { Tag: KeyDef key })
		{
			var special = key.Kind is not (KeyKind.Character or KeyKind.Space);
			visual.Background = new SolidColorBrush(special ? SpecialKeyBackground : KeyBackground);
		}
		_pressedKey = null;
		_pressedDef = null;
	}

	private void CommitKey(KeyDef key)
	{
		switch (key.Kind)
		{
			case KeyKind.Character when key.Character is { } character:
				InjectCharacter(character);
				break;
			case KeyKind.Space:
				InjectCharacter(' ');
				break;
			case KeyKind.Enter:
				InjectSpecial(VirtualKey.Enter);
				break;
			case KeyKind.Tab:
				InjectSpecial(VirtualKey.Tab);
				break;
			case KeyKind.ArrowLeft:
				InjectSpecial(VirtualKey.Left);
				break;
			case KeyKind.ArrowRight:
				InjectSpecial(VirtualKey.Right);
				break;
			case KeyKind.ArrowUp:
				InjectSpecial(VirtualKey.Up);
				break;
			case KeyKind.ArrowDown:
				InjectSpecial(VirtualKey.Down);
				break;
			case KeyKind.Backspace:
				// Injected on press (with auto-repeat); nothing more on release.
				break;
			case KeyKind.Shift:
				var now = DateTimeOffset.UtcNow;
				_shift = _shift switch
				{
					ShiftState.Off when (now - _lastShiftTap).TotalMilliseconds < 350 => ShiftState.Locked,
					ShiftState.Off => ShiftState.Once,
					ShiftState.Once when (now - _lastShiftTap).TotalMilliseconds < 350 => ShiftState.Locked,
					_ => ShiftState.Off,
				};
				_lastShiftTap = now;
				Rebuild();
				break;
			case KeyKind.SymbolsPage:
				_page = Page.Symbols1;
				Rebuild();
				break;
			case KeyKind.SymbolsPage2:
				_page = Page.Symbols2;
				Rebuild();
				break;
			case KeyKind.LettersPage:
				_page = Page.Letters;
				Rebuild();
				break;
			case KeyKind.Globe:
				_activeLayoutIndex = (_activeLayoutIndex + 1) % _layouts.Count;
				_shift = ShiftState.Off;
				_page = Page.Letters;
				Rebuild();
				break;
		}
	}

	private void InjectCharacter(char character)
	{
		var key = character switch
		{
			>= 'a' and <= 'z' => VirtualKey.A + (character - 'a'),
			>= 'A' and <= 'Z' => VirtualKey.A + (character - 'A'),
			>= '0' and <= '9' => VirtualKey.Number0 + (character - '0'),
			' ' => VirtualKey.Space,
			_ => VirtualKey.None,
		};
		_injector.InjectSoftwareKey(pressed: true, key, character);
		_injector.InjectSoftwareKey(pressed: false, key, null);
		if (_shift == ShiftState.Once)
		{
			_shift = ShiftState.Off;
			Rebuild();
		}
	}

	private void InjectSpecial(VirtualKey key)
	{
		_injector.InjectSoftwareKey(pressed: true, key, null);
		_injector.InjectSoftwareKey(pressed: false, key, null);
	}

	private void OnBackspaceRepeat()
	{
		if (_pressedDef?.Kind == KeyKind.Backspace)
		{
			_repeatTimer.Interval = TimeSpan.FromMilliseconds(60);
			InjectSpecial(VirtualKey.Back);
		}
		else
		{
			_repeatTimer.Stop();
		}
	}

	private void OnLongPress()
	{
		_longPressTimer.Stop();
		if (_pressedKey is not { Tag: KeyDef { Alternates: { } alternates } } key)
		{
			return;
		}
		_longPressFired = true;
		ShowAlternates(key, alternates);
	}

	private void ShowAlternates(Border key, string alternates)
	{
		CloseAlternates();
		var panel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 2,
		};
		var container = new Border
		{
			RequestedTheme = ElementTheme.Light,
			Background = new SolidColorBrush(Colors.White),
			BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(4),
			Child = panel,
		};
		foreach (var alternate in alternates)
		{
			var alternateKey = new Border
			{
				Background = new SolidColorBrush(KeyBackground),
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(10, 6, 10, 6),
				Child = new TextBlock
				{
					Text = alternate.ToString(),
					FontSize = 18,
					Foreground = new SolidColorBrush(KeyForeground),
				},
			};
			var captured = alternate;
			alternateKey.PointerReleased += (_, e) =>
			{
				e.Handled = true;
				InjectCharacter(captured);
				CloseAlternates();
			};
			alternateKey.PointerPressed += (_, e) => e.Handled = true;
			panel.Children.Add(alternateKey);
		}

		var position = key.TransformToVisual(this).TransformPoint(new Point(0, 0));
		_alternatesPopup = new Popup
		{
			XamlRoot = XamlRoot,
			Child = container,
		};
		var origin = TransformToVisual(null).TransformPoint(new Point(0, 0));
		_alternatesPopup.HorizontalOffset = Math.Max(4, origin.X + position.X - 8);
		_alternatesPopup.VerticalOffset = Math.Max(4, origin.Y + position.Y - 54);
		_alternatesPopup.IsOpen = true;
	}

	private void CloseAlternates()
	{
		if (_alternatesPopup is not null)
		{
			_alternatesPopup.IsOpen = false;
			_alternatesPopup = null;
		}
	}
}
