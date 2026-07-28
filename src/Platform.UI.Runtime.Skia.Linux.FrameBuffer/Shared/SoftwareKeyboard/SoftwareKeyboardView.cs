// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.Localization;
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
/// The on-screen keyboard's visual: a strip of pointer-driven keys, styled by the
/// same theme resources as ContentDialog (see <see cref="DialogThemeResources"/>)
/// so it matches — and restyles with — this head's dialog chrome.
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

	// The fixed spacing chrome: each key's margin (so adjacent keys sit two margins
	// apart) and the strip's outer padding. ComputeHeight's HalfHeight math relies
	// on these staying the spacing used by Rebuild/BuildKeyVisual.
	private const double KeyMargin = 2.5;
	private const double StripPadding = 3;

	// Fallbacks for the theme resources that drive the keyboard chrome (see
	// DialogThemeResources): the standard Fluent light-theme values each key
	// carries, used only when a key cannot be resolved.
	private static readonly Color StripBackgroundFallback = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3);
	private static readonly Color StripBorderFallback = Color.FromArgb(0x66, 0x75, 0x75, 0x75);
	private static readonly Color KeyFillFallback = Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF);
	private static readonly Color KeyForegroundFallback = Color.FromArgb(0xE4, 0x00, 0x00, 0x00);
	private static readonly Color SpaceLegendFallback = Color.FromArgb(0x9E, 0x00, 0x00, 0x00);
	private static readonly Color AccentFallback = Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);

	// The key surfaces, resolved (or derived) once per keyboard from the active
	// theme: normal keys use the same fill as the dialogs' buttons; special keys
	// are the foreground composited faintly onto the strip so they read slightly
	// darker in a light theme and slightly lighter in a dark one; a pressed key
	// flashes with an accent tint.
	private readonly Brush _stripBrush;
	private readonly Brush _chromeBorderBrush;
	private readonly Brush _keyBrush;
	private readonly Brush _specialKeyBrush;
	private readonly Brush _pressedKeyBrush;
	private readonly Brush _keyForegroundBrush;
	private readonly Brush _spaceLegendBrush;

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
		Dismiss,
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
	private readonly SoftwareKeyboardOptions _options;
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

	internal SoftwareKeyboardView(ISoftwareKeyInjector injector, IReadOnlyList<KeyboardLayoutDefinition> layouts,
		SoftwareKeyboardOptions options)
	{
		_injector = injector;
		_layouts = layouts;
		_options = options;

		// Resolved against the active theme when the keyboard is first created
		// (the controller keeps the one view for the application's lifetime).
		var strip = DialogThemeResources.ColorOf("ContentDialogBackground", StripBackgroundFallback);
		var foreground = DialogThemeResources.ColorOf("ContentDialogForeground", KeyForegroundFallback);
		var keyFace = DialogThemeResources.Composite(
			DialogThemeResources.ColorOf("ControlFillColorDefaultBrush", KeyFillFallback), 1.0, strip);
		var accent = DialogThemeResources.ColorOf("SystemAccentColor", AccentFallback);
		_stripBrush = new SolidColorBrush(strip);
		_chromeBorderBrush = DialogThemeResources.Brush("ContentDialogBorderBrush", StripBorderFallback);
		_keyBrush = new SolidColorBrush(keyFace);
		_specialKeyBrush = new SolidColorBrush(DialogThemeResources.Composite(foreground, 0.15, strip));
		_pressedKeyBrush = new SolidColorBrush(DialogThemeResources.Composite(accent, 0.30, keyFace));
		_keyForegroundBrush = DialogThemeResources.Brush("ContentDialogForeground", KeyForegroundFallback);
		_spaceLegendBrush = DialogThemeResources.Brush("TextFillColorSecondaryBrush", SpaceLegendFallback);

		Background = _stripBrush;
		BorderBrush = _chromeBorderBrush;
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
	/// Raised when the user taps the dismiss key. The keys are non-focusable, so
	/// the text control being typed into still has focus when this fires — the
	/// controller records it as the user's explicit "keyboard off" intent.
	/// </summary>
	internal event Action? DismissRequested;

	/// <summary>
	/// The keyboard strip height for a given logical root size: a per-orientation
	/// fraction bounded so keys stay comfortably tappable on the smallest panel
	/// and the strip never dominates the largest. Under
	/// <see cref="SoftwareKeyHeight.HalfHeight"/> the key faces halve while the
	/// spacing chrome (key gaps and strip padding) keeps its full-height size.
	/// </summary>
	internal double ComputeHeight(Size rootSize)
	{
		var fullHeight = rootSize.Height > rootSize.Width
			? Math.Clamp(rootSize.Height * 0.40, 200, 400)
			: Math.Clamp(rootSize.Height * 0.42, 190, 340);
		if (_options.KeyHeight != SoftwareKeyHeight.HalfHeight)
		{
			return fullHeight;
		}

		// The letters page's row count (digits row + the layout's letter rows +
		// the bottom row) drives the split of the strip into key faces vs. fixed
		// spacing; the symbols pages have one row fewer and simply get slightly
		// taller keys within the same strip, exactly as at full height.
		var rowCount = ActiveLayout.Rows.Length + 2;
		var spacing = rowCount * KeyMargin * 2 + StripPadding * 2;
		return spacing + (fullHeight - spacing) / 2;
	}

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
		_rows.Padding = new Thickness(StripPadding);

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
			WithDismissKey(
				DigitsRow.Select(digit => new KeyDef(KeyKind.Character, 1f, digit.ToString(), digit)).ToList()),
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
		rows[0] = WithDismissKey(rows[0]);

		// The third symbols row is flanked by the page toggle and backspace.
		rows[2].Insert(0, new KeyDef(secondPage ? KeyKind.SymbolsPage : KeyKind.SymbolsPage2, 1.5f,
			secondPage ? "?123" : "=\\<"));
		rows[2].Add(new KeyDef(KeyKind.Backspace, 1.5f));

		rows.Add(BuildBottomRow(lettersPage: false, withArrows: secondPage));
		return rows;
	}

	// The dismiss key is always the top-right key, on every page of every layout,
	// at the same width as the rest of its row - unless the host opted out.
	private List<KeyDef> WithDismissKey(List<KeyDef> topRow)
	{
		if (_options.ShowDismissKey)
		{
			topRow.Add(new KeyDef(KeyKind.Dismiss, 1f));
		}
		return topRow;
	}

	private List<KeyDef> BuildBottomRow(bool lettersPage, bool withArrows = false)
	{
		var row = new List<KeyDef>
		{
			lettersPage
				? new KeyDef(KeyKind.SymbolsPage, 1.5f, "?123")
				: new KeyDef(KeyKind.LettersPage, 1.5f, PlatformStrings.KeyAbc),
		};
		if (_layouts.Count > 1)
		{
			row.Add(new KeyDef(KeyKind.Globe, 1f, ActiveLayout.Id.ToUpperInvariant()));
		}
		if (withArrows)
		{
			row.Add(new KeyDef(KeyKind.Tab, 1f, PlatformStrings.KeyTab));
			row.Add(new KeyDef(KeyKind.ArrowLeft, 1f, SymbolGlyphs.ArrowLeft));
			row.Add(new KeyDef(KeyKind.ArrowUp, 1f, SymbolGlyphs.ArrowUp));
			row.Add(new KeyDef(KeyKind.ArrowDown, 1f, SymbolGlyphs.ArrowDown));
			row.Add(new KeyDef(KeyKind.ArrowRight, 1f, SymbolGlyphs.ArrowRight));
		}
		else
		{
			row.Add(new KeyDef(KeyKind.Character, 1f, ",", ','));
			row.Add(new KeyDef(KeyKind.Space, 4f));
			row.Add(new KeyDef(KeyKind.Character, 1f, ".", '.'));
		}
		row.Add(new KeyDef(KeyKind.Enter, 1.5f, PlatformStrings.KeyEnter));
		return row;
	}

	// The key's painted legend. An arrow legend is a Fluent Private Use Area
	// codepoint, so that one gets the symbols font: a plain Unicode arrow is
	// missing from Open Sans and Roboto and would draw as a box on a device with
	// no host fonts to fall back on. See SymbolGlyphs.
	private TextBlock CreateLegend(KeyDef key, string legend)
	{
		var text = new TextBlock
		{
			Text = legend,
			Foreground = key.Kind == KeyKind.Space ? _spaceLegendBrush : _keyForegroundBrush,
			FontSize = key.Kind == KeyKind.Character ? 18 : 13,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		if (SymbolGlyphs.IsSymbolGlyph(legend))
		{
			text.FontFamily = SymbolGlyphs.SymbolFontFamily;
		}
		return text;
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
				ShiftState.Off => PlatformStrings.KeyShift,
				// The dot marks "shifted for the next key only".
				// U+2022 BULLET, not U+25CF BLACK CIRCLE: the latter is absent
				// from Open Sans, so it drew as a missing-glyph box in any
				// application using that font.
				ShiftState.Once => PlatformStrings.KeyShift + " •",
				_ => PlatformStrings.KeyShiftUpper,
			},
			// Word legends stay within the glyph coverage of every bundled
			// application font; symbol glyphs for these can come once a dedicated
			// keyboard font is settled.
			KeyKind.Backspace => PlatformStrings.KeyBackspace,
			KeyKind.Space => ActiveLayout.DisplayName,
			_ => key.Legend ?? "",
		};

		// The dismiss key's downward triangle is drawn, not text: a shaped glyph
		// renders identically on every layout regardless of any font's coverage.
		// (The arrow keys take the other route — a Fluent symbol glyph — because
		// four arrowheads as polygons would be four more shapes to keep aligned.)
		UIElement face = key.Kind == KeyKind.Dismiss
			? new Microsoft.UI.Xaml.Shapes.Polygon
			{
				Points = [new Point(0, 0), new Point(12, 0), new Point(6, 7)],
				Fill = _keyForegroundBrush,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			}
			: CreateLegend(key, legend);

		var visual = new Border
		{
			Background = special ? _specialKeyBrush : _keyBrush,
			CornerRadius = new CornerRadius(5),
			Margin = new Thickness(KeyMargin),
			Child = face,
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
		visual.Background = _pressedKeyBrush;

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
			visual.Background = special ? _specialKeyBrush : _keyBrush;
		}
		_pressedKey = null;
		_pressedDef = null;

		// The capture taken on press MUST be handed back explicitly, exactly as
		// ButtonBase does: these heads only ever produce TOUCH pointers, and for
		// touch the managed input manager deliberately does not release captures
		// on pointer-up - it defers that to the source-level PointerExited of the
		// documented "Up / Exited / Lost" finger sequence, which a frame-buffer
		// panel never sends. A leaked capture re-routes the NEXT key's release to
		// this now-stale key, so that key lights up on press and then never
		// commits. Released last, and only after the pressed-key state is already
		// cleared, so the PointerCaptureLost this raises synchronously finds
		// nothing left to undo.
		visual?.ReleasePointerCaptures();
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
			case KeyKind.Dismiss:
				DismissRequested?.Invoke();
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
			Background = _stripBrush,
			BorderBrush = _chromeBorderBrush,
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(4),
			Child = panel,
		};
		foreach (var alternate in alternates)
		{
			var alternateKey = new Border
			{
				Background = _keyBrush,
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(10, 6, 10, 6),
				Child = new TextBlock
				{
					Text = alternate.ToString(),
					FontSize = 18,
					Foreground = _keyForegroundBrush,
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
