using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using CodeBrix.Platform.UI.CommandBar;
using Windows.System;

// ReSharper disable CheckNamespace

namespace CommandBarDemo.Views;

/// <summary>
/// The parity page: a WinUI CommandBar sample written the way a WinUI page writes it, with no
/// prefix changes, driven by the demo's self-test on the head.
/// </summary>
/// <remarks>
/// <para>
/// The XAML is the deliverable here. This file only fills in the artwork the demo generates at
/// start-up, wires the two commanded buttons, and runs the measurements the self-test reports.
/// </para>
/// <para>
/// The page is reached from <see cref="MainPage"/>: the self-test finishes its own checks, then
/// navigates the root frame here and asks this page for the parity half.
/// </para>
/// </remarks>
public sealed partial class WinUiCommandBarPage : Page
{
	/// <summary>A 24x24 SVG whose ink is <c>currentColor</c>, so a tint decides its colour.</summary>
	private const string TintableSvgMarkup =
		"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\">"
		+ "<path d=\"M12 2 L22 12 L12 22 L2 12 Z\" fill=\"currentColor\" /></svg>";

	private readonly StringBuilder _log = new();
	private readonly XamlUICommand _commandWithAccelerator = new();
	private readonly TaskCompletionSource<bool> _loaded = new();
	private int _commandInvocations;

	/// <summary>Creates the page.</summary>
	public WinUiCommandBarPage()
	{
		InitializeComponent();

		//The add-in's icons take their artwork here because this demo writes its icon files at
		//start-up rather than shipping them as content; an application writes the URI in the XAML.
		SvgIconButton.Icon = new SvgIcon
		{
			Markup = TintableSvgMarkup,
			Size = 20,
			Tint = new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x66, 0xDD)),
		};
		SvgIconSourceArt.Markup = TintableSvgMarkup;

		//A XamlUICommand carrying an accelerator, bound to an ordinary Button and to an
		//AppBarButton. The framework copies the command's accelerators onto whatever it is bound
		//to, and both buttons take their label from it as well.
		_commandWithAccelerator.Label = "Quit";
		_commandWithAccelerator.Description = "Leave the score";
		_commandWithAccelerator.KeyboardAccelerators.Add(
			new KeyboardAccelerator { Key = VirtualKey.Q, Modifiers = VirtualKeyModifiers.Control });
		_commandWithAccelerator.ExecuteRequested += (_, _) => _commandInvocations++;

		CommandedButton.Command = _commandWithAccelerator;
		CommandedAppBarButton.Command = _commandWithAccelerator;

		Loaded += (_, _) => _loaded.TrySetResult(true);
	}

	/// <summary>Waits until the page has been loaded into the tree.</summary>
	/// <returns>A task that completes once the page is loaded.</returns>
	public Task WaitForLoadedAsync() => _loaded.Task;

	/// <summary>
	/// Runs the parity measurements and reports each one through the self-test's recorder.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	/// <param name="windowId">The X11 window this process owns, or null when it is not known.</param>
	/// <returns>A task that completes when every measurement has been reported.</returns>
	public async Task RunParityChecksAsync(Action<string, bool, string> check, string windowId)
	{
		UpdateLayout();
		await Task.Delay(700);

		// ---- The bars themselves ---------------------------------------------------------------

		// P1. Every type the pasted sample names resolved, and every bar got a template.
		check("winui-commandbars-realized",
			MediaBar != null && EditBar != null && MinimalBar != null && DynamicBar != null
			&& IconBar != null
			&& MediaBar.Template != null && EditBar.Template != null && MinimalBar.Template != null,
			$"media={MediaBar?.GetType().FullName} templates="
			+ $"{MediaBar?.Template != null}/{EditBar?.Template != null}/{MinimalBar?.Template != null}");

		// P2. The bar's own content is its PrimaryCommands collection, and the declared elements
		//     landed in it as the types the XAML named.
		var primary = MediaBar.PrimaryCommands;
		check("winui-primarycommands-collection",
			primary.Count == 6
			&& primary[0] is AppBarToggleButton && primary[2] is AppBarSeparator
			&& primary[3] is AppBarButton,
			$"count={primary.Count} types={string.Join("/", primary.Select(p => p.GetType().Name))}");

		// P3. SecondaryCommands is a separate collection and holds what the sample put there.
		var secondary = MediaBar.SecondaryCommands;
		check("winui-secondarycommands-collection",
			secondary.Count == 4 && secondary[0] is AppBarButton && secondary[2] is AppBarSeparator,
			$"count={secondary.Count} types={string.Join("/", secondary.Select(p => p.GetType().Name))}");

		// P4. The bar paints: a real arranged size on the head, and so does its Content block.
		check("winui-commandbar-paints",
			MediaBar.ActualWidth > 0 && MediaBar.ActualHeight > 0 && NowPlaying.ActualWidth > 0,
			$"bar={MediaBar.ActualWidth:0.#}x{MediaBar.ActualHeight:0.#} "
			+ $"content={NowPlaying.ActualWidth:0.#}x{NowPlaying.ActualHeight:0.#}");

		// ---- The elements ----------------------------------------------------------------------

		// P5. An AppBarButton with a Symbol icon: the button arranges, and so does the SymbolIcon
		//     the XAML's Icon="Back" shorthand created.
		var backIcon = BackButton.Icon as SymbolIcon;
		check("winui-appbarbutton-symbolicon-paints",
			BackButton.ActualWidth > 0 && BackButton.ActualHeight > 0
			&& backIcon != null && backIcon.ActualWidth > 0 && backIcon.ActualHeight > 0,
			$"button={BackButton.ActualWidth:0.#}x{BackButton.ActualHeight:0.#} "
			+ $"icon={backIcon?.GetType().Name} {backIcon?.ActualWidth:0.#}x{backIcon?.ActualHeight:0.#} "
			+ $"symbol={backIcon?.Symbol}");

		// P6. An AppBarToggleButton checks and unchecks, and paints while it does.
		var shuffleBefore = ShuffleToggle.IsChecked == true;
		ShuffleToggle.IsChecked = true;
		UpdateLayout();
		await Task.Delay(200);
		var shuffleChecked = ShuffleToggle.IsChecked == true;
		ShuffleToggle.IsChecked = false;
		await Task.Delay(200);
		check("winui-appbartogglebutton-paints-and-toggles",
			!shuffleBefore && shuffleChecked && ShuffleToggle.IsChecked == false
			&& ShuffleToggle.ActualWidth > 0 && ShuffleToggle.ActualHeight > 0,
			$"{ShuffleToggle.ActualWidth:0.#}x{ShuffleToggle.ActualHeight:0.#} "
			+ $"before={shuffleBefore} checked={shuffleChecked} after={ShuffleToggle.IsChecked}");

		// P7. An AppBarSeparator draws a line rather than nothing: narrow, and as tall as the bar's
		//     command row.
		check("winui-appbarseparator-paints",
			MediaSeparator.ActualWidth > 0 && MediaSeparator.ActualWidth < 20
			&& MediaSeparator.ActualHeight > 10,
			$"{MediaSeparator.ActualWidth:0.#}x{MediaSeparator.ActualHeight:0.#}");

		// P8. An AppBarElementContainer hosts an ordinary control in the bar, and the control works.
		ZoomCombo.SelectedIndex = 2;
		await Task.Delay(250);
		var zoomText = (ZoomCombo.SelectedItem as ComboBoxItem)?.Content as string;
		check("winui-appbarelementcontainer-hosts-a-plain-control",
			ZoomContainer.ActualWidth > 0 && ZoomContainer.ActualHeight > 0
			&& ZoomCombo.ActualWidth > 100 && zoomText == "200%",
			$"container={ZoomContainer.ActualWidth:0.#}x{ZoomContainer.ActualHeight:0.#} "
			+ $"combo={ZoomCombo.ActualWidth:0.#}x{ZoomCombo.ActualHeight:0.#} selected='{zoomText}'");

		// P9. A FontIcon glyph paints on an AppBarButton.
		check("winui-fonticon-paints",
			FontIconButton.ActualWidth > 0 && FontIconArt.ActualWidth > 0 && FontIconArt.ActualHeight > 0,
			$"button={FontIconButton.ActualWidth:0.#}x{FontIconButton.ActualHeight:0.#} "
			+ $"glyph={FontIconArt.ActualWidth:0.#}x{FontIconArt.ActualHeight:0.#}");

		// P10. A BitmapIcon over a PNG addressed with ms-appx:///, the way a WinUI sample writes it.
		check("winui-bitmapicon-over-a-png-paints",
			BitmapIconButton.ActualWidth > 0 && BitmapIconArt.ActualWidth > 0
			&& BitmapIconArt.ActualHeight > 0 && BitmapIconArt.UriSource != null,
			$"button={BitmapIconButton.ActualWidth:0.#}x{BitmapIconButton.ActualHeight:0.#} "
			+ $"icon={BitmapIconArt.ActualWidth:0.#}x{BitmapIconArt.ActualHeight:0.#} "
			+ $"uri={BitmapIconArt.UriSource}");

		// ---- Label positions -------------------------------------------------------------------

		// P11. DefaultLabelPosition="Right" shows the label beside the icon while the bar is CLOSED:
		//      that is what tells a "Right" bar apart from a "Bottom" one at rest.
		var rightLabel = LabelWidth(SaveButton);
		var rightIconOnly = LabelWidth(CutButton);
		check("winui-defaultlabelposition-right-labels-a-closed-bar",
			EditBar.DefaultLabelPosition == CommandBarDefaultLabelPosition.Right
			&& rightLabel > 0 && SaveButton.ActualWidth > SaveButton.ActualHeight
			&& FontIconButton.ActualWidth > CutButton.ActualWidth,
			$"label='{SaveButton.Label}' {rightLabel:0.#} wide; "
			+ $"button={SaveButton.ActualWidth:0.#}x{SaveButton.ActualHeight:0.#}; "
			+ $"a labelled button {FontIconButton.ActualWidth:0.#} vs an unlabelled one "
			+ $"{CutButton.ActualWidth:0.#} (that one's label is {rightIconOnly:0.#} wide)");

		// P12. DefaultLabelPosition="Bottom" with ClosedDisplayMode="Compact" hides the labels while
		//      the bar is closed and shows them under the icons when it opens - the compact/full-size
		//      pair a WinUI bar switches between.
		var bottomLabelClosed = LabelWidth(StopButton);
		var barHeightClosed = MediaBar.ActualHeight;
		var contentRootClosed = ContentRootHeight(MediaBar);
		var clipClosed = ContentClip(MediaBar);
		MediaBar.IsOpen = true;
		UpdateLayout();
		await Task.Delay(900);
		var bottomLabelOpen = LabelWidth(StopButton);
		var barHeightOpen = MediaBar.ActualHeight;
		var contentRootOpen = ContentRootHeight(MediaBar);
		var clipOpen = ContentClip(MediaBar);
		MediaBar.IsOpen = false;
		UpdateLayout();
		await Task.Delay(600);
		//An open bar OVERLAYS rather than reflowing: AppBar.MeasureOverride returns the closed
		//display mode's height whatever the bar is showing, and the label row that sticks out below
		//it is revealed by moving the bar's clip rather than by making the bar taller.
		check("winui-defaultlabelposition-bottom-labels-an-open-bar",
			MediaBar.DefaultLabelPosition == CommandBarDefaultLabelPosition.Bottom
			&& bottomLabelClosed == 0 && bottomLabelOpen > 0
			&& contentRootOpen > barHeightOpen
			&& Math.Abs(barHeightOpen - barHeightClosed) < 0.5
			&& clipOpen != clipClosed,
			$"label='{StopButton.Label}' closed={bottomLabelClosed:0.#} open={bottomLabelOpen:0.#}; "
			+ $"bar {barHeightClosed:0.#} -> {barHeightOpen:0.#} high; "
			+ $"contentRoot={contentRootClosed:0.#} -> {contentRootOpen:0.#}; "
			+ $"button={StopButton.ActualHeight:0.#}; clip {clipClosed} -> {clipOpen}");

		// P13. DefaultLabelPosition="Collapsed" shows no label at all, open or closed.
		var collapsedLabelClosed = LabelWidth(CutButton);
		MinimalBar.IsOpen = true;
		UpdateLayout();
		await Task.Delay(900);
		var collapsedLabelOpen = LabelWidth(CutButton);
		MinimalBar.IsOpen = false;
		UpdateLayout();
		await Task.Delay(600);
		check("winui-defaultlabelposition-collapsed-never-labels",
			MinimalBar.DefaultLabelPosition == CommandBarDefaultLabelPosition.Collapsed
			&& collapsedLabelClosed == 0 && collapsedLabelOpen == 0,
			$"label='{CutButton.Label}' closed={collapsedLabelClosed:0.#} open={collapsedLabelOpen:0.#}");

		// ---- Closed display modes --------------------------------------------------------------

		// P14. ClosedDisplayMode decides how much of a closed bar is on screen: Compact keeps the
		//      command row, Minimal keeps only the overflow button's strip, and Hidden keeps
		//      nothing. The commands are still laid out in Minimal - they are simply clipped away,
		//      which is what the bar being shorter than a command shows.
		var compactHeight = MediaBar.ActualHeight;
		var minimalHeight = MinimalBar.ActualHeight;
		var commandHeight = CutButton.ActualHeight;
		MinimalBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
		UpdateLayout();
		await Task.Delay(500);
		var minimalAsCompactHeight = MinimalBar.ActualHeight;
		MinimalBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
		UpdateLayout();
		await Task.Delay(500);
		var hiddenHeight = MinimalBar.ActualHeight;
		MinimalBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
		UpdateLayout();
		await Task.Delay(500);
		check("winui-closeddisplaymode-compact-minimal-hidden",
			compactHeight > 0 && minimalHeight > 0
			&& minimalAsCompactHeight > minimalHeight && minimalHeight > hiddenHeight
			&& hiddenHeight == 0 && minimalHeight < commandHeight,
			$"the same bar: compact={minimalAsCompactHeight:0.#} minimal={minimalHeight:0.#} "
			+ $"hidden={hiddenHeight:0.#}; a command is {commandHeight:0.#} high, so minimal clips it");

		// ---- Open, sticky, overflow ------------------------------------------------------------

		// P14. IsOpen opens the overflow and the secondary commands paint in it; IsSticky keeps it
		//      open when the focus moves away.
		EditBar.IsOpen = true;
		UpdateLayout();
		await Task.Delay(900);
		var settingsPainted = SettingsButton.ActualWidth > 0 && SettingsButton.ActualHeight > 0;
		var openWhileSticky = EditBar.IsOpen;
		ZoomCombo.Focus(FocusState.Programmatic);
		await Task.Delay(500);
		var stillOpen = EditBar.IsOpen;
		EditBar.IsOpen = false;
		UpdateLayout();
		await Task.Delay(600);
		check("winui-isopen-and-issticky",
			settingsPainted && openWhileSticky && EditBar.IsSticky && stillOpen && !EditBar.IsOpen,
			$"opened={openWhileSticky} secondary painted while open={settingsPainted} "
			+ $"sticky={EditBar.IsSticky} stillOpenAfterFocusMoved={stillOpen} "
			+ $"closed={!EditBar.IsOpen}");

		// P15. A secondary command that is in the overflow shows its accelerator's TEXT, derived
		//      from the KeyboardAccelerator the sample declared on it.
		MediaBar.IsOpen = true;
		UpdateLayout();
		await Task.Delay(900);
		var shareText = ShareButton.KeyboardAcceleratorTextOverride;
		var sharePainted = ShareButton.ActualWidth > 0;
		var sharePaintedSize = $"{ShareButton.ActualWidth:0.#}x{ShareButton.ActualHeight:0.#}";
		MediaBar.IsOpen = false;
		await Task.Delay(500);
		check("winui-keyboardaccelerator-text-in-the-overflow",
			sharePainted && !string.IsNullOrEmpty(shareText) && shareText.Contains("Ctrl"),
			$"accelerator={ShareAccelerator.Modifiers}+{ShareAccelerator.Key} "
			+ $"text='{shareText}' painted while open={sharePaintedSize}");

		// P16. A Flyout on an AppBarButton opens from the button and its items paint.
		AddButton.Flyout.ShowAt(AddButton);
		await Task.Delay(800);
		var flyoutOpen = AddFlyout.Items.Count == 2 && AddSectionItem.ActualWidth > 0;
		var flyoutItemSize = $"{AddSectionItem.ActualWidth:0.#}x{AddSectionItem.ActualHeight:0.#}";
		AddFlyout.Hide();
		await Task.Delay(500);
		check("winui-flyout-on-an-appbarbutton-opens",
			flyoutOpen,
			$"items={AddFlyout.Items.Count} first painted while open={flyoutItemSize}");

		// ---- Dynamic overflow ------------------------------------------------------------------

		// P17. IsDynamicOverflowEnabled: as the bar narrows, trailing primary commands move into
		//      the overflow, and they come back when the room returns.
		var inOverflowAtFullWidth = DynamicBar.PrimaryCommands
			.OfType<AppBarButton>().Count(b => b.IsInOverflow);
		DynamicHost.Width = 260;
		UpdateLayout();
		await Task.Delay(900);
		var inOverflowNarrow = DynamicBar.PrimaryCommands
			.OfType<AppBarButton>().Count(b => b.IsInOverflow);
		DynamicHost.Width = double.NaN;
		UpdateLayout();
		await Task.Delay(900);
		var inOverflowAgain = DynamicBar.PrimaryCommands
			.OfType<AppBarButton>().Count(b => b.IsInOverflow);
		check("winui-isdynamicoverflowenabled-moves-commands-and-gives-them-back",
			DynamicBar.IsDynamicOverflowEnabled
			&& inOverflowAtFullWidth == 0 && inOverflowNarrow > 0 && inOverflowAgain == 0,
			$"enabled={DynamicBar.IsDynamicOverflowEnabled} atFullWidth={inOverflowAtFullWidth} "
			+ $"at260={inOverflowNarrow} backAgain={inOverflowAgain}");

		// P18. The bar with dynamic overflow turned OFF keeps every command where it was put.
		check("winui-isdynamicoverflowenabled-false-keeps-the-commands",
			!MinimalBar.IsDynamicOverflowEnabled
			&& MinimalBar.PrimaryCommands.OfType<AppBarButton>().All(b => !b.IsInOverflow),
			$"enabled={MinimalBar.IsDynamicOverflowEnabled} "
			+ $"inOverflow={MinimalBar.PrimaryCommands.OfType<AppBarButton>().Count(b => b.IsInOverflow)}");

		// ---- Icons that the core does not know about (D6 and P4) -------------------------------

		// P19. D6: the add-in's SvgIcon as an AppBarButton.Icon. This is the ONE prefixed element on
		//      the page, and it works because the application references the CommandBar add-in; the
		//      core package has no SVG dependency of its own.
		var svgIcon = SvgIconButton.Icon as CodeBrix.Platform.UI.CommandBar.SvgIcon;
		check("winui-appbarbutton-with-the-addins-svgicon-paints",
			svgIcon != null && svgIcon.ActualWidth > 0 && svgIcon.ActualHeight > 0
			&& SvgIconButton.ActualWidth > 0,
			$"button={SvgIconButton.ActualWidth:0.#}x{SvgIconButton.ActualHeight:0.#} "
			+ $"icon={svgIcon?.GetType().Name} {svgIcon?.ActualWidth:0.#}x{svgIcon?.ActualHeight:0.#} "
			+ $"tinted={svgIcon?.Tint != null}");

		// P20. P4: a third-party IconSource through the framework's IconSourceElement wrapper. The
		//      wrapper knew only its own four sources before this wave; each of these three would
		//      have drawn nothing at all.
		var svgSourceChild = FirstIconChild(SvgIconSourceHost);
		var rasterSourceChild = FirstIconChild(RasterIconSourceHost);
		var imageSourceChild = FirstIconChild(ImageIconSourceHost);
		check("winui-iconsourceelement-renders-a-third-party-iconsource",
			svgSourceChild != null && svgSourceChild.ActualWidth > 0
			&& rasterSourceChild != null && rasterSourceChild.ActualWidth > 0,
			$"svg={svgSourceChild?.GetType().Name} {svgSourceChild?.ActualWidth:0.#}"
			+ $"x{svgSourceChild?.ActualHeight:0.#}; "
			+ $"raster={rasterSourceChild?.GetType().Name} {rasterSourceChild?.ActualWidth:0.#}"
			+ $"x{rasterSourceChild?.ActualHeight:0.#}");

		// P21. The same wrapper and the framework's OWN ImageIconSource, which drew nothing before
		//      the fallback either.
		check("winui-iconsourceelement-renders-the-frameworks-imageiconsource",
			imageSourceChild != null && imageSourceChild.ActualWidth > 0,
			$"{imageSourceChild?.GetType().Name} {imageSourceChild?.ActualWidth:0.#}"
			+ $"x{imageSourceChild?.ActualHeight:0.#}");

		// ---- Command binding (P4b) -------------------------------------------------------------

		// P22. A XamlUICommand's keyboard accelerators reach the button the framework binds them to.
		//      They used to reach nothing: the converter that copies them returned a collection it
		//      never filled, so Ctrl+Q on this command did nothing from either button.
		var plainAccelerators = CommandedButton.KeyboardAccelerators;
		var appBarAccelerators = CommandedAppBarButton.KeyboardAccelerators;
		check("winui-xamluicommand-accelerators-reach-the-button",
			plainAccelerators.Count == 1 && plainAccelerators[0].Key == VirtualKey.Q
			&& plainAccelerators[0].Modifiers == VirtualKeyModifiers.Control
			&& appBarAccelerators.Count == 1 && appBarAccelerators[0].Key == VirtualKey.Q,
			$"button={plainAccelerators.Count} "
			+ $"({(plainAccelerators.Count > 0 ? $"{plainAccelerators[0].Modifiers}+{plainAccelerators[0].Key}" : "none")}) "
			+ $"appBarButton={appBarAccelerators.Count}");

		// P23. The command still drives its buttons the ordinary way: the label flows to both, and
		//      an invocation runs it.
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(CommandedAppBarButton)
			as AppBarButtonAutomationPeer;
		peer?.Invoke();
		await Task.Delay(300);
		check("winui-xamluicommand-drives-its-buttons",
			CommandedAppBarButton.Label == "Quit" && CommandedButton.Content as string == "Quit"
			&& _commandInvocations > 0,
			$"appBarLabel='{CommandedAppBarButton.Label}' buttonContent='{CommandedButton.Content}' "
			+ $"invocations={_commandInvocations}");

		// P24. And the window really painted it: the tray the media bar sits on is found in a
		//      capture of THIS window at the size it was arranged at.
		var counted = -1;
		var capture = Path.Combine(Path.GetTempPath(), "commandbardemo-winui-capture.png");
		if (!string.IsNullOrEmpty(windowId))
		{
			counted = CaptureAndCount(windowId, capture, "#00C8FF");
		}

		check("winui-window-capture-shows-the-commandbar",
			counted > 500,
			$"{counted} pixels of #00C8FF in {capture}; the host arranged "
			+ $"{MediaHost.ActualWidth:0}x{MediaHost.ActualHeight:0} logical at scale "
			+ $"{XamlRoot?.RasterizationScale:0.##}");
	}

	/// <summary>Writes one line to the page's own log.</summary>
	/// <param name="message">The line.</param>
	public void Log(string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
		_log.AppendLine(line);
		Console.WriteLine($"COMMANDBAR|{line}");
		if (LogText != null)
		{
			LogText.Text = _log.ToString();
		}
	}

	/// <summary>
	/// How wide the button's label is drawn. A command bar button's template carries more than one
	/// text block for the same label - one for each place the label can sit - so the widest one is
	/// the one on screen, and zero means no label is drawn at all.
	/// </summary>
	/// <param name="button">The button to look inside.</param>
	/// <returns>The widest drawn label, or 0 when none is drawn.</returns>
	private static double LabelWidth(AppBarButton button)
		=> FindDescendants<TextBlock>(button)
			.Where(t => t.Text == button.Label && t.Visibility == Visibility.Visible)
			.Select(t => t.ActualWidth)
			.DefaultIfEmpty(0d)
			.Max();

	/// <summary>The icon element an <see cref="IconSourceElement"/> built for its source.</summary>
	/// <param name="host">The wrapper.</param>
	/// <returns>The icon element it holds, or null when it built none.</returns>
	private static IconElement FirstIconChild(IconSourceElement host)
		=> FindDescendants<IconElement>(host).FirstOrDefault();

	/// <summary>How tall the bar's own content row is, whatever the bar reports for itself.</summary>
	/// <param name="bar">The command bar.</param>
	/// <returns>The content root's arranged height, or 0 when it was not found.</returns>
	private static double ContentRootHeight(CommandBar bar)
		=> FindDescendants<FrameworkElement>(bar)
			.Where(e => e.Name == "ContentRoot")
			.Select(e => e.ActualHeight)
			.DefaultIfEmpty(0d)
			.FirstOrDefault();

	/// <summary>
	/// Where the bar's content clip sits: the rectangle and how far it is shifted. Closing the bar
	/// shifts it up so the label row below the closed height is cut away; opening puts it back.
	/// </summary>
	/// <param name="bar">The command bar.</param>
	/// <returns>A short description of the clip, or "none".</returns>
	private static string ContentClip(CommandBar bar)
	{
		foreach (var element in FindDescendants<FrameworkElement>(bar))
		{
			if (element.Clip is { } clip)
			{
				var offset = (clip.Transform as TranslateTransform)?.Y ?? 0d;

				return $"{clip.Rect.Height:0.#}@{offset:0.#}";
			}
		}

		return "none";
	}

	private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : class
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				yield return match;
			}

			foreach (var deeper in FindDescendants<T>(child))
			{
				yield return deeper;
			}
		}
	}

	/// <summary>
	/// Captures one window and counts the pixels of one colour in it.
	/// </summary>
	/// <param name="windowId">The window to capture.</param>
	/// <param name="path">Where to write the capture.</param>
	/// <param name="hexColour">The colour to count, as "#RRGGBB".</param>
	/// <returns>How many pixels of that colour the capture holds, or -1 when it could not be taken.</returns>
	private static int CaptureAndCount(string windowId, string path, string hexColour)
	{
		try
		{
			var script =
				$"import -window {windowId} '{path}' && convert '{path}' -depth 8 txt:- | grep -c -- '{hexColour}'";
			var process = Process.Start(new ProcessStartInfo("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\"")
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			});

			if (process == null)
			{
				return -1;
			}

			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit(20000);

			return int.TryParse(output.Trim(), out var value) ? value : -1;
		}
		catch (Exception)
		{
			return -1;
		}
	}
}
