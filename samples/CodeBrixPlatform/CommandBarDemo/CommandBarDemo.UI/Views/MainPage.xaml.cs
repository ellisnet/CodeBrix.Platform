using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using CodeBrix.Platform.UI.CommandBar;
using CodeBrix.Platform.UI.Svg;
using CommandBarDemo.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.System;

// ReSharper disable CheckNamespace

namespace CommandBarDemo.Views;

/// <summary>
/// The CommandBar add-in's demo page. Set COMMANDBARDEMO_SELFTEST=1 to run the scripted checks on
/// screen and exit with the number of failures (results also written to the COMMANDBARDEMO_RESULTS
/// file path, one PASS/FAIL line per check), mirroring the ParityDemo self-test hook.
/// </summary>
/// <remarks>
/// The page is in two halves. The top half is the REFERENCE DEMO: a score editor's main and music
/// tool bars, everything on them bound to <see cref="MainViewModel"/>. The lower half keeps the
/// per-control proof rows the scaffold and the three build streams wrote, so every self-test step
/// any of them contributed still runs against the element it was written for.
/// </remarks>
public sealed partial class MainPage : Page
{
	// ---- ICONS stream, on-screen proof -------------------------------------------------------
	// An SVG written inline rather than shipped as an asset, so the demo proves the icon route
	// without any asset plumbing: a 24x24 document whose left 11 columns are painted in whatever
	// "currentColor" resolves to. SvgIcon.Tint resolves it, through a stylesheet handed to the
	// parser, so this square comes out in the tint rather than black.
	private const string ProofSvgMarkup =
		"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\">"
		+ "<rect x=\"0\" y=\"0\" width=\"11\" height=\"24\" fill=\"currentColor\" /></svg>";

	// A real 24x24 PNG with an alpha channel - left half opaque #FF8800, right half fully
	// transparent - written to a temporary file so the platform's own image decoder reads it from
	// disk exactly as it would read an application's asset.
	private const string ProofPngBase64 =
		"iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAJ0lEQVR42mP438HwnxTMQCoYtWDUglELRi0YtWDUglELRi0YGhYA"
		+ "AK471t/WCORYAAAAAElFTkSuQmCC";

	/// <summary>The colour the reference demo's tray sits on, so a capture can find it.</summary>
	private const string TrayColourInCapture = "#00C8FF";

	private readonly StringBuilder _log = new();
	private readonly MainViewModel _model = new();
	private bool _hooked;

	//The BUTTONS stream's own subjects. A command built from an action alone is the shape an
	//application reaches for most often, and the shape the add-in's suite fences host-free.
	private readonly SimpleCommand _saveCommand;
	private readonly SwitchableDemoCommand _printCommand = new();
	private int _saveCount;
	private VirtualKeyModifiers _lastClickModifiers = VirtualKeyModifiers.None;

	//The reference demo's own recording of what a real click and a real key press carried.
	private VirtualKeyModifiers _lastPrintClickModifiers = VirtualKeyModifiers.None;
	private VirtualKey _lastKey = VirtualKey.None;
	private int _accessKeyInvocations;
	private int _printClicks;

	/// <summary>This process's own X11 window, once a probe has proven which one it is.</summary>
	private string _ownWindow;

	public MainPage()
	{
		InitializeComponent();

		_saveCommand = new SimpleCommand(() => _saveCount++);

		SaveButton.Icon = new DemoSquareIconSource();
		IconOnlyButton.Icon = new DemoSquareIconSource();
		MagnifierButton.Icon = new DemoSquareIconSource();
		MenuDropDown.Icon = new DemoSquareIconSource();
		InstantDropDown.Icon = new DemoSquareIconSource();
		DelayedDropDown.Icon = new DemoSquareIconSource();

		SaveButton.Command = _saveCommand;
		IconOnlyButton.Command = _printCommand;
		SaveButton.ClickWithModifiers += (_, args) => _lastClickModifiers = args.Modifiers;

		BuildReferenceDemo();

		KeyDown += OnPageKeyDown;
		ActualThemeChanged += OnPageActualThemeChanged;
		Loaded += OnPageLoaded;
	}

	#region The reference demo

	/// <summary>
	/// Wires the reference tool bars to the view model: commands, icons, menus and the switches.
	/// </summary>
	/// <remarks>
	/// Everything here could be written in XAML with bindings instead; it is done in code so the
	/// page shows both vocabularies at once - the XAML above declares the SHAPE of the bars, and
	/// this fills in the ACTIONS - and so the demo needs no converters or static resources to read.
	/// </remarks>
	private void BuildReferenceDemo()
	{
		//New / Open / Save are XamlUICommands: the button states neither text nor icon, and takes
		//both from the command, along with its description and its keyboard accelerator.
		NewToolButton.Command = _model.NewCommand;
		OpenToolButton.Command = _model.OpenCommand;
		SaveToolButton.Command = _model.SaveCommand;

		//The rest are plain commands, so the button says what it shows.
		EngraveToolButton.Command = _model.EngraveCommand;
		EngraveToolButton.Icon = SvgIcon("engrave");
		PrintToolButton.Command = _model.PrintCommand;
		PrintToolButton.Icon = SvgIcon("print");
		PrintToolButton.ClickWithModifiers += (_, args) =>
		{
			_lastPrintClickModifiers = args.Modifiers;
			_printClicks++;
		};

		//An access key: Alt+G reaches this button from anywhere in the page. The framework resolves
		//it from the modifiers the head reports for the key press, which is why it is also the
		//public proof that X11's Mod1 arrives as the Menu modifier.
		PrintToolButton.AccessKey = "G";
		PrintToolButton.AccessKeyInvoked += (_, args) =>
		{
			_accessKeyInvocations++;
			args.Handled = true;
		};

		//A PNG with an alpha channel, tinted: the image's alpha becomes the mask and the tint
		//paints it, which is how a monochrome bitmap icon follows a theme.
		MagnifierToolButton.Icon = new RasterIconSource
		{
			Source = DemoIcons.Png,
			Tint = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue),
		};
		MagnifierToolButton.SetBinding(
			ToolToggleButton.IsCheckedProperty,
			new Microsoft.UI.Xaml.Data.Binding
			{
				Source = _model,
				Path = new PropertyPath(nameof(MainViewModel.IsMagnifierOn)),
				Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
			});

		//A JPEG, drawn exactly as it was written: no alpha, so no tint.
		ScoreThumbnailButton.Icon = new RasterIconSource { Source = DemoIcons.Jpeg };

		//Artwork that states no colour of its own: each chevron is drawn in currentColor and the
		//button's tint decides what that is, so one icon set serves any theme or accent.
		PreviousPageButton.Command = _model.PreviousPageCommand;
		PreviousPageButton.Icon = TintedIcon(DemoIcons.TintablePrevious, Microsoft.UI.Colors.SteelBlue);
		NextPageButton.Command = _model.NextPageCommand;
		NextPageButton.Icon = TintedIcon(DemoIcons.TintableNext, Microsoft.UI.Colors.SeaGreen);
		PageLabel.Text = _model.PageLabel;

		//The Open button's arrow shows the recent files, each carrying its own file name.
		foreach (var file in _model.RecentFiles)
		{
			RecentFilesFlyout.Items.Add(new MenuFlyoutItem
			{
				Text = file,
				Command = _model.OpenRecentCommand,
				CommandParameter = file,
			});
		}

		//The Engrave button's arrow shows the engrave modes.
		foreach (var mode in _model.EngraveModes)
		{
			EngraveModesFlyout.Items.Add(new MenuFlyoutItem
			{
				Text = mode,
				Command = _model.EngraveModeCommand,
				CommandParameter = mode,
				Icon = new SvgIcon { UriSource = DemoIcons.Light("mode"), Size = 16 },
			});
		}

		SaveAsItem.Command = _model.SaveAsCommand;

		ScoreChooser.ItemsSource = _model.Scores;
		ScoreChooser.SelectedItem = _model.SelectedScore;
		ScoreChooser.SelectionChanged += (_, _) =>
			_model.SelectedScore = ScoreChooser.SelectedItem as string ?? _model.SelectedScore;

		ZoomBox.ItemsSource = _model.ZoomLevels;
		ZoomBox.Text = _model.Zoom;
		ZoomBox.TextSubmitted += (_, args) => _model.Zoom = args.Text;
		ZoomBox.SelectionChanged += (_, _) =>
			_model.Zoom = ZoomBox.SelectedItem as string ?? _model.Zoom;

		//The two bar-level switches. Both are inherited attached properties set on the TRAY, so one
		//setting reaches both bars and every item in them.
		VerboseCheck.Checked += (_, _) => SetLabelMode(LabelMode.IconAndText);
		VerboseCheck.Unchecked += (_, _) => SetLabelMode(LabelMode.IconOnly);
		ToolTipsCheck.Checked += (_, _) => ToolBarProperties.SetShowToolTips(FrescoTray, true);
		ToolTipsCheck.Unchecked += (_, _) => ToolBarProperties.SetShowToolTips(FrescoTray, false);

		_model.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(MainViewModel.PageLabel))
			{
				PageLabel.Text = _model.PageLabel;
			}
			else if (args.PropertyName == nameof(MainViewModel.LastAction))
			{
				ActionText.Text = $"Last action: {_model.LastAction}";
			}
		};
	}

	private void SetLabelMode(LabelMode mode) => ToolBarProperties.SetLabelMode(FrescoTray, mode);

	private static SvgIconSource SvgIcon(string name)
		=> new() { Source = DemoIcons.Light(name), Dark = DemoIcons.Dark(name) };

	private static SvgIconSource TintedIcon(Uri artwork, Windows.UI.Color colour)
		=> new() { Source = artwork, Tint = new SolidColorBrush(colour) };

	#endregion

	private void OnPageKeyDown(object sender, KeyRoutedEventArgs e) => _lastKey = e.Key;

	/// <summary>
	/// Reports the theme change and which artwork the icons swapped to, so a live theme change -
	/// the desktop's, or an application's own switch - can be watched happening.
	/// </summary>
	/// <param name="sender">The page.</param>
	/// <param name="args">Unused.</param>
	private void OnPageActualThemeChanged(FrameworkElement sender, object args)
	{
		//The icons have their own handler for the same event, and the order the handlers run in is
		//not this page's to choose, so the artwork is read on the NEXT turn - by which time every
		//icon has re-rendered.
		DispatcherQueue.TryEnqueue(() =>
		{
			var icon = NewToolButton.IconVisual as SvgIcon;

			Log($"Theme is now {ActualTheme}; the New button's artwork is "
				+ $"{Path.GetFileName(icon?.ResolvedUriSource?.LocalPath) ?? "none"}");
		});
	}

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		if (_hooked)
		{
			return;
		}

		_hooked = true;
		Log($"Loaded. scale={XamlRoot?.RasterizationScale:0.##} size={XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0}");

		SetUpIconProof();

		if (Environment.GetEnvironmentVariable("COMMANDBARDEMO_SELFTEST") == "1")
		{
			_ = RunSelfTestAsync();
		}
	}

	/// <summary>
	/// Gives the two proof icons their artwork: the SVG inline, the PNG through a temporary file.
	/// </summary>
	private void SetUpIconProof()
	{
		SvgIconProof.Markup = ProofSvgMarkup;

		var pngPath = Path.Combine(Path.GetTempPath(), "commandbardemo-icon-proof.png");
		File.WriteAllBytes(pngPath, Convert.FromBase64String(ProofPngBase64));
		RasterIconProof.UriSource = new Uri(pngPath);

		Log($"Icon proof: svg markup {ProofSvgMarkup.Length} chars, png {pngPath}");
		Log($"Reference demo icons: {DemoIcons.Folder}");
	}

	private void Log(string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
		_log.AppendLine(line);
		Console.WriteLine($"COMMANDBAR|{line}");
		if (LogText != null)
		{
			LogText.Text = _log.ToString();
		}
	}

	private async Task RunSelfTestAsync()
	{
		var results = new List<string>();
		void Check(string name, bool pass, string detail)
		{
			var line = $"{(pass ? "PASS" : "FAIL")} {name}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}";
			results.Add(line);
			Log($"SELFTEST: {line}");
		}

		try
		{
			Log("SELFTEST: starting in 1.5s");
			await Task.Delay(1500);

			// 1. The add-in's assembly really loaded on this head and the XAML resolved its types.
			Check("tray-realized", Tray != null, Tray?.GetType().FullName ?? "null");
			Check("bar-realized", MainBar != null, $"{MainBar?.Items.Count ?? -1} item(s)");

			// 2. Every item in the placeholder bar is the type the XAML named.
			Check("items-are-the-declared-types",
				NewButton is ToolButton
				&& MagnifierToggle is ToolToggleButton
				&& EngraveButton is ToolDropDownButton
				&& Spacer is ToolBarSpacer,
				$"{NewButton?.GetType().Name}/{MagnifierToggle?.GetType().Name}"
				+ $"/{EngraveButton?.GetType().Name}/{Spacer?.GetType().Name}");

			// 3. The inherited attached properties set in XAML on a parent that lays out reach
			//    the item below it - the scaffold's one real behaviour, measured in a live tree.
			Check("attached-properties-inherit",
				Math.Abs(ToolBarProperties.GetIconSize(HostedButton) - 32d) < 0.001
				&& ToolBarProperties.GetLabelMode(HostedButton) == LabelMode.IconAndText
				&& ToolBarProperties.GetLabelPosition(HostedButton) == LabelPosition.Bottom
				&& !ToolBarProperties.GetShowToolTips(HostedButton),
				$"IconSize={ToolBarProperties.GetIconSize(HostedButton)} "
				+ $"LabelMode={ToolBarProperties.GetLabelMode(HostedButton)} "
				+ $"LabelPosition={ToolBarProperties.GetLabelPosition(HostedButton)} "
				+ $"ShowToolTips={ToolBarProperties.GetShowToolTips(HostedButton)}");

			// 4. The default styles in the add-in's Themes/Generic.xaml were found and applied.
			Check("default-styles-applied",
				NewButton.Template != null && MainBar.Template != null
				&& HostedButton.Template != null && HostedSeparator.Template != null,
				$"bar={MainBar.Template != null} button={NewButton.Template != null} "
				+ $"hosted button={HostedButton.Template != null} "
				+ $"hosted separator={HostedSeparator.Template != null}");

			// 5. Those styles reach the SCREEN: the templated controls under a parent that lays
			//    out have a real arranged size on the head.
			Check("styled-controls-paint",
				HostedButton.ActualWidth > 0 && HostedButton.ActualHeight > 0
				&& HostedToggle.ActualWidth > 0 && HostedToggle.ActualHeight > 0
				&& HostedSeparator.ActualWidth > 0 && HostedSeparator.ActualHeight > 0,
				$"button={HostedButton.ActualWidth:0.#}x{HostedButton.ActualHeight:0.#} "
				+ $"toggle={HostedToggle.ActualWidth:0.#}x{HostedToggle.ActualHeight:0.#} "
				+ $"separator={HostedSeparator.ActualWidth:0.#}x{HostedSeparator.ActualHeight:0.#}");

			// ===== BUTTONS stream steps =====

			// 6. The three button types paint: a real arranged size on the head, through the
			//    add-in's own default template rather than a borrowed one.
			Check("buttons-paint",
				SaveButton.ActualWidth > 0 && SaveButton.ActualHeight > 0
				&& MagnifierButton.ActualWidth > 0 && MagnifierButton.ActualHeight > 0
				&& MenuDropDown.ActualWidth > 0 && MenuDropDown.ActualHeight > 0,
				$"button={SaveButton.ActualWidth:0.#}x{SaveButton.ActualHeight:0.#} "
				+ $"toggle={MagnifierButton.ActualWidth:0.#}x{MagnifierButton.ActualHeight:0.#} "
				+ $"dropdown={MenuDropDown.ActualWidth:0.#}x{MenuDropDown.ActualHeight:0.#}");

			// 7. The icon slot really draws: the element built from the icon source is in the
			//    visual tree and arranged at the inherited IconSize.
			var iconVisual = SaveButton.IconVisual;
			Check("icon-slot-paints",
				iconVisual != null && iconVisual.ActualWidth > 0 && iconVisual.ActualHeight > 0,
				$"icon={iconVisual?.ActualWidth:0.#}x{iconVisual?.ActualHeight:0.#} "
				+ $"effectiveIconSize={SaveButton.EffectiveIconSize:0.#}");

			// 8. An icon-only button is narrower than the same button showing its label, which is
			//    the whole observable difference LabelMode makes.
			Check("label-mode-changes-the-width",
				IconOnlyButton.ActualWidth > 0 && SaveButton.ActualWidth > IconOnlyButton.ActualWidth,
				$"iconOnly={IconOnlyButton.ActualWidth:0.#} iconAndText={SaveButton.ActualWidth:0.#}");

			// 9. Switching LabelMode at run time re-lays out - the "show button text" preference,
			//    which is why the mode has to be live rather than a startup choice.
			var widthBefore = IconOnlyButton.ActualWidth;
			ToolBarProperties.SetLabelMode(IconOnlyButton, LabelMode.IconAndText);
			IconOnlyButton.UpdateLayout();
			await Task.Delay(150);
			var widthAfter = IconOnlyButton.ActualWidth;
			ToolBarProperties.SetLabelMode(IconOnlyButton, LabelMode.IconOnly);
			Check("label-mode-switches-at-run-time", widthAfter > widthBefore,
				$"before={widthBefore:0.#} after={widthAfter:0.#}");

			// 10. The tooltip is composed, and the per-button override silences one button in a
			//     row that shows them.
			var composed = ToolTipService.GetToolTip(SaveButton) as string;
			var quiet = ToolTipService.GetToolTip(NoToolTipButton);
			Check("tooltip-composed-and-overridable",
				composed == "Save (Ctrl+S)" && quiet == null,
				$"save='{composed}' quiet={(quiet == null ? "null" : quiet.ToString())}");

			// 11. IsEnabled follows CanExecute, live, through a command that changes its mind.
			var enabledBefore = IconOnlyButton.IsEnabled;
			_printCommand.SetCanExecute(false);
			await Task.Delay(100);
			var enabledAfter = IconOnlyButton.IsEnabled;
			_printCommand.SetCanExecute(true);
			await Task.Delay(100);
			Check("isenabled-follows-canexecute",
				enabledBefore && !enabledAfter && IconOnlyButton.IsEnabled,
				$"before={enabledBefore} disabled={enabledAfter} restored={IconOnlyButton.IsEnabled}");

			// 12. The automation peer clicks the button for real: the command runs and the
			//     modifier-carrying event is raised alongside the ordinary one.
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(SaveButton) as ToolButtonAutomationPeer;
			peer?.Invoke();
			await Task.Delay(100);
			Check("automation-invoke-runs-the-command",
				peer != null && _saveCount == 1 && _lastClickModifiers == VirtualKeyModifiers.None,
				$"peerName='{peer?.GetName()}' saves={_saveCount} modifiers={_lastClickModifiers}");

			// 13. The toggle's checked state survives a click and reaches the automation peer.
			var togglePeer = FrameworkElementAutomationPeer.CreatePeerForElement(MagnifierButton)
				as ToolToggleButtonAutomationPeer;
			togglePeer?.Toggle();
			await Task.Delay(150);
			Check("toggle-checks-and-repaints",
				MagnifierButton.IsChecked
				&& togglePeer?.ToggleState == Microsoft.UI.Xaml.Automation.ToggleState.On,
				$"isChecked={MagnifierButton.IsChecked} peerState={togglePeer?.ToggleState}");

			// 14. The flyout really opens on this head, and closing it re-hooks the bindings of the
			//     items inside (a flyout item unsubscribes on close and never subscribes again).
			MenuDropDown.OpenFlyout();
			await Task.Delay(400);
			var opened = MenuDropDown.IsFlyoutOpen;
			MenuDropDown.CloseFlyout();
			await Task.Delay(400);
			Check("flyout-opens-and-closes",
				opened && !MenuDropDown.IsFlyoutOpen,
				$"opened={opened} closed={!MenuDropDown.IsFlyoutOpen}");

			// 15. The arrow half is real space in the two modes that have one, and absent in the
			//     mode where the whole face is the target.
			Check("dropdown-arrow-follows-the-popup-mode",
				MenuDropDown.ArrowVisibility == Visibility.Visible
				&& InstantDropDown.ArrowVisibility == Visibility.Visible
				&& DelayedDropDown.ArrowVisibility == Visibility.Collapsed
				&& MenuDropDown.ActualWidth > DelayedDropDown.ActualWidth,
				$"menuButton={MenuDropDown.ActualWidth:0.#} instant={InstantDropDown.ActualWidth:0.#} "
				+ $"delayed={DelayedDropDown.ActualWidth:0.#}");

			// 16. The Delayed mode's press-and-hold runs on a dispatcher timer, so the timer itself
			//     is proven to tick on this head; the state machine it drives is fenced host-free
			//     in ToolDropDownButtonTests.
			var holdTimer = new DispatcherTimer { Interval = DelayedDropDown.PressAndHoldDelay };
			var holdTicked = false;
			holdTimer.Tick += (_, _) =>
			{
				holdTicked = true;
				holdTimer.Stop();
			};
			holdTimer.Start();
			await Task.Delay((int)DelayedDropDown.PressAndHoldDelay.TotalMilliseconds + 400);
			Check("press-and-hold-timer-ticks", holdTicked,
				$"delay={DelayedDropDown.PressAndHoldDelay.TotalMilliseconds:0}ms ticked={holdTicked}");

			// 17. A Delayed button still opens its flyout when something else asks it to - the
			//     route an automation client and an application take.
			var delayedPeer = FrameworkElementAutomationPeer.CreatePeerForElement(DelayedDropDown)
				as ToolDropDownButtonAutomationPeer;
			delayedPeer?.Expand();
			await Task.Delay(400);
			var delayedOpened = DelayedDropDown.IsFlyoutOpen;
			delayedPeer?.Collapse();
			await Task.Delay(400);
			Check("delayed-button-opens-its-flyout",
				delayedOpened && !DelayedDropDown.IsFlyoutOpen,
				$"opened={delayedOpened} state={delayedPeer?.ExpandCollapseState}");

			// 18. The core primitives this stream considered building on, MEASURED rather than
			//     assumed. "Present in the assembly" is not "paints on the head".
			Check("core-dropdownbutton-paints",
				CoreDropDownButton.ActualWidth > 0 && CoreDropDownButton.ActualHeight > 0,
				$"{CoreDropDownButton.ActualWidth:0.#}x{CoreDropDownButton.ActualHeight:0.#}");
			Check("core-togglesplitbutton-paints",
				CoreToggleSplitButton.ActualWidth > 0 && CoreToggleSplitButton.ActualHeight > 0,
				$"{CoreToggleSplitButton.ActualWidth:0.#}x{CoreToggleSplitButton.ActualHeight:0.#}");

			var scale = XamlRoot?.RasterizationScale ?? 1d;

			// ===== ICONS stream steps =====

			// 19. The SVG route produced an image source for the inline document, and the platform
			//     rasterised it at the icon's size TIMES the display's scale - which is what makes
			//     an icon pixel-exact on a fractional-scale display rather than stretched.
			var rasterized = SvgIconProof.Source is SvgImageSource svg
				? SvgProvider.GetRasterizedPixelSize(svg)
				: default;

			Check("icon-svg-source-created", SvgIconProof.Source is SvgImageSource,
				SvgIconProof.Source?.GetType().Name ?? "null");
			Check("icon-svg-arranged-at-icon-size",
				Math.Abs(SvgIconProof.ActualWidth - 24d) < 0.6
				&& Math.Abs(SvgIconProof.ActualHeight - 24d) < 0.6,
				$"{SvgIconProof.ActualWidth:0.##}x{SvgIconProof.ActualHeight:0.##} logical");
			Check("icon-svg-rasterized-at-icon-size-times-scale",
				Math.Abs(rasterized.Width - (24d * scale)) < 1.01
				&& Math.Abs(rasterized.Height - (24d * scale)) < 1.01,
				$"{rasterized.Width:0}x{rasterized.Height:0} device px at scale {scale:0.##} "
				+ $"(expected {24d * scale:0}x{24d * scale:0})");
			Check("icon-svg-tint-composed", SvgIconProof.Tint is not null, "#2266DD");

			// 20. The raster half: a real PNG, read from disk by the platform's own decoder, with
			//     the alpha channel that a tint would use as its mask.
			Check("icon-png-resolved",
				RasterIconProof.ResolvedUriSource is { IsFile: true },
				RasterIconProof.ResolvedUriSource?.LocalPath ?? "null");
			Check("icon-png-arranged-at-icon-size",
				Math.Abs(RasterIconProof.ActualWidth - 24d) < 0.6
				&& Math.Abs(RasterIconProof.ActualHeight - 24d) < 0.6,
				$"{RasterIconProof.ActualWidth:0.##}x{RasterIconProof.ActualHeight:0.##} logical");
			Check("icon-stage-painted",
				IconStage.ActualWidth > 60 && IconStage.ActualHeight > 20,
				$"stage={IconStage.ActualWidth:0.#}x{IconStage.ActualHeight:0.#}");

			Log($"SELFTEST: tray/bar arranged size: "
				+ $"tray={Tray.ActualWidth:0.#}x{Tray.ActualHeight:0.#} "
				+ $"bar={MainBar.ActualWidth:0.#}x{MainBar.ActualHeight:0.#}");

			// ---- LAYOUT stream: the tray, the bar, its panel, groups, separators, spacers,
			//      overflow and the vertical axis, all on the head. ----

			// 21. The tray and the bar really lay out now - the scaffold's 0x0 finding is closed.
			Check("layout-tray-and-bar-arrange",
				LayoutTray.ActualWidth > 0 && LayoutTray.ActualHeight > 0
				&& LayoutBar.ActualWidth > 0 && LayoutBar.ActualHeight > 0
				&& MusicBar.ActualWidth > 0,
				$"tray={LayoutTray.ActualWidth:0.#}x{LayoutTray.ActualHeight:0.#} "
				+ $"bar={LayoutBar.ActualWidth:0.#}x{LayoutBar.ActualHeight:0.#} "
				+ $"music={MusicBar.ActualWidth:0.#}x{MusicBar.ActualHeight:0.#}");

			// 22. Every item in the bar has a real arranged size, including the ones inside a group.
			Check("layout-bar-items-arrange",
				GroupOne.ActualWidth > 0 && GroupOneFirst.ActualWidth > 0
				&& GroupTwoSecond.ActualWidth > 0 && TailToggle.ActualWidth > 0,
				$"group={GroupOne.ActualWidth:0.#}x{GroupOne.ActualHeight:0.#} "
				+ $"first={GroupOneFirst.ActualWidth:0.#}x{GroupOneFirst.ActualHeight:0.#} "
				+ $"tail={TailToggle.ActualWidth:0.#}x{TailToggle.ActualHeight:0.#}");

			// 23. Two adjacent groups get a separator between them that nobody wrote.
			var separators = ItemsHostChildren(LayoutBar).Count(c => c is ToolBarSeparator);
			Check("layout-group-separator-auto-inserted",
				separators == 2,
				$"{separators} separator(s) in the bar's panel; one is the authored one, one was "
				+ "inserted between the two groups");

			// 24. The hairline is one DEVICE pixel wide at this display's scale, not one logical one.
			var expectedLine = 1d / scale;
			Check("layout-separator-is-one-device-pixel",
				Math.Abs(LayoutSeparator.LogicalThickness - expectedLine) < 0.001
				&& Math.Abs((LayoutSeparator.ActualWidth * scale) - Math.Round(LayoutSeparator.ActualWidth * scale)) < 0.01,
				$"scale={scale:0.###} logical={LayoutSeparator.LogicalThickness:0.####} "
				+ $"arranged={LayoutSeparator.ActualWidth:0.####} "
				+ $"device={LayoutSeparator.ActualWidth * scale:0.####}");

			// 25. A filling spacer pushes the trailing item to the far end of the bar it is in.
			var tailRight = OffsetIn(StretchBar, StretchTail) + StretchTail.ActualWidth;
			Check("layout-spacer-fill-pushes-the-trailing-item",
				FillSpacer.ActualWidth > 100 && Math.Abs(tailRight - (StretchBar.ActualWidth - 4)) < 2,
				$"spacer={FillSpacer.ActualWidth:0.#} tailRight={tailRight:0.#} "
				+ $"barWidth={StretchBar.ActualWidth:0.#}");

			// 26. Shrink the bar's host - what a window shrink does to a bar - and the chevron
			//     appears with the trailing items behind it.
			LayoutHost.Width = 260;
			UpdateLayout();
			await Task.Delay(250);
			var chevron = FindChevron(LayoutBar);
			Check("layout-chevron-appears-when-the-bar-shrinks",
				LayoutBar.HasOverflowItems && chevron != null && chevron.ActualWidth > 0,
				$"hasOverflow={LayoutBar.HasOverflowItems} "
				+ $"chevron={(chevron == null ? "absent" : $"{chevron.ActualWidth:0.#}x{chevron.ActualHeight:0.#}")} "
				+ $"tailStillArranged={TailToggle.ActualWidth:0.#}");

			// 27. The tray wraps its second bar onto a further row when the row runs out.
			Check("layout-tray-wraps-the-second-bar",
				LayoutTray.ActualHeight > LayoutBar.ActualHeight + 4,
				$"tray={LayoutTray.ActualHeight:0.#} bar={LayoutBar.ActualHeight:0.#}");

			// 28. Give the space back and the same elements come home.
			LayoutHost.Width = double.NaN;
			UpdateLayout();
			await Task.Delay(250);
			Check("layout-chevron-goes-away-when-the-space-returns",
				!LayoutBar.HasOverflowItems && FindChevron(LayoutBar) == null
				&& TailToggle.ActualWidth > 0,
				$"hasOverflow={LayoutBar.HasOverflowItems} tail={TailToggle.ActualWidth:0.#}x{TailToggle.ActualHeight:0.#}");

			// 29. The turned axis: a vertical bar stacks its items and turns its separator.
			Check("layout-vertical-bar-arranges",
				VerticalBar.ActualHeight > VerticalFirst.ActualHeight + VerticalSecond.ActualHeight
				&& VerticalSeparator.Orientation == Orientation.Horizontal
				&& VerticalSeparator.ActualHeight > 0,
				$"bar={VerticalBar.ActualWidth:0.#}x{VerticalBar.ActualHeight:0.#} "
				+ $"separator={VerticalSeparator.ActualWidth:0.#}x{VerticalSeparator.ActualHeight:0.####} "
				+ $"orientation={VerticalSeparator.Orientation}");

			// 30. The keyboard walks along the bar. The key is a REAL one, sent to the real window
			//     by xdotool, so what is proven is the whole path: X11 event, framework routing,
			//     the bar's OnKeyDown, focus moving to the next item.
			FrameworkElement before = null;
			FrameworkElement after = null;
			var sent = false;
			var window = "none";

			//Another head running the same demo may be on this display at the same time, and every
			//one of them is titled "CommandBar Demo", so the candidates are this PROCESS ID's own
			//windows rather than that title. Each is tried in turn, newest first, and the one whose
			//key press MOVES THIS PAGE'S FOCUS is by definition the right one - so the check can
			//never pass on somebody else's window, and never fails just because theirs was found
			//first. The winner is remembered: every later step that resizes or captures a window
			//uses THAT id and no other.
			foreach (var candidate in ListDemoWindows())
			{
				for (var attempt = 0; attempt < 2 && !ReferenceEquals(after, GroupOneSecond); attempt++)
				{
					GroupOneFirst.Focus(FocusState.Keyboard);
					await Task.Delay(250);
					before = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
					sent = SendKeyToWindow(candidate, "Right");
					window = candidate;
					await Task.Delay(600);
					after = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
				}

				if (ReferenceEquals(after, GroupOneSecond))
				{
					_ownWindow = candidate;
					break;
				}
			}

			//"window=none" here means no window was found for this process id at all, which is the
			//head failing to publish _NET_WM_PID rather than the bar failing to move focus.
			Check("layout-keyboard-moves-along-the-bar",
				sent && ReferenceEquals(before, GroupOneFirst) && ReferenceEquals(after, GroupOneSecond),
				$"sent={sent} window={window} pid={Environment.ProcessId} "
				+ $"before={before?.Name ?? "null"} after={after?.Name ?? "null"}");

			// 31. The flyout itself. "Present" is not "paints": the bar leans on Flyout and Popup,
			//     so the items that moved into the overflow are shown to have no arranged size
			//     while it is closed and a real one once it is open on the head.
			LayoutHost.Width = 260;
			UpdateLayout();
			await Task.Delay(300);
			var hiddenWidth = TailToggle.ActualWidth;
			var opened2 = LayoutBar.ShowOverflow();
			await Task.Delay(700);
			Check("layout-overflow-flyout-opens-and-paints-the-moved-items",
				opened2 && hiddenWidth == 0 && TailToggle.ActualWidth > 0 && EngraveButtonTwo.ActualWidth > 0,
				$"opened={opened2} closedWidth={hiddenWidth:0.#} "
				+ $"tail={TailToggle.ActualWidth:0.#}x{TailToggle.ActualHeight:0.#} "
				+ $"engrave={EngraveButtonTwo.ActualWidth:0.#}x{EngraveButtonTwo.ActualHeight:0.#}");

			//Put the page back the way the reference demo needs it: the flyout dismissed and the
			//layout host free again.
			SendKeyToWindow(_ownWindow, "Escape");
			await Task.Delay(400);
			LayoutHost.Width = double.NaN;
			UpdateLayout();
			await Task.Delay(300);

			await RunReferenceDemoChecksAsync(Check);

			//The parity half. The pasted WinUI CommandBar sample lives on a page of its own, so it
			//can be read as what it is - XAML written for WinUI, running here unchanged - rather
			//than as one more row among the add-in's proof rows. The self-test navigates to it, and
			//that page reports its own measurements through the same recorder.
			await RunWinUiParityChecksAsync(Check);
		}
		catch (Exception ex)
		{
			results.Add($"FAIL selftest-exception ({ex.GetType().Name}: {ex.Message})");
			Log($"SELFTEST: exception {ex}");
		}

		var resultsPath = Environment.GetEnvironmentVariable("COMMANDBARDEMO_RESULTS");
		if (!string.IsNullOrEmpty(resultsPath))
		{
			File.WriteAllLines(resultsPath, results);
		}

		var failures = results.Count(r => r.StartsWith("FAIL", StringComparison.Ordinal));
		Log($"SELFTEST: done, {failures} failure(s); exiting");
		await Task.Delay(250);
		Environment.Exit(failures);
	}

	/// <summary>
	/// Navigates to the WinUI parity page and lets it run its own measurements.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunWinUiParityChecksAsync(Action<string, bool, string> check)
	{
		if (Frame is not { } frame)
		{
			check("winui-parity-page-reached", false, "the page is not hosted in a frame");

			return;
		}

		frame.Navigate(typeof(WinUiCommandBarPage));
		await Task.Delay(600);

		if (frame.Content is not WinUiCommandBarPage parityPage)
		{
			check("winui-parity-page-reached", false,
				$"navigated to {frame.Content?.GetType().Name ?? "nothing"}");

			return;
		}

		await parityPage.WaitForLoadedAsync();
		await Task.Delay(600);
		check("winui-parity-page-reached", true, $"{frame.Content.GetType().Name} loaded");

		await parityPage.RunParityChecksAsync(check, _ownWindow);
	}

	/// <summary>
	/// Drives the reference demo: the two bars, their commands, their icons and their switches.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunReferenceDemoChecksAsync(Action<string, bool, string> check)
	{
		// 32. Both reference bars paint, side by side in one tray.
		check("fresco-tray-two-bars-paint",
			MainToolBar.ActualWidth > 0 && MainToolBar.ActualHeight > 0
			&& MusicToolBar.ActualWidth > 0 && MusicToolBar.ActualHeight > 0
			&& FrescoHost.ActualWidth > 0,
			$"host={FrescoHost.ActualWidth:0.#}x{FrescoHost.ActualHeight:0.#} "
			+ $"main={MainToolBar.ActualWidth:0.#}x{MainToolBar.ActualHeight:0.#} "
			+ $"music={MusicToolBar.ActualWidth:0.#}x{MusicToolBar.ActualHeight:0.#}");

		// 33. A XamlUICommand drives its button whole: label, icon, description and shortcut, none
		//     of which the button states for itself.
		var newToolTip = ToolTipService.GetToolTip(NewToolButton) as string;
		check("fresco-xamluicommand-supplies-label-icon-and-shortcut",
			NewToolButton.ResolvedText == "New"
			&& NewToolButton.IconVisual is SvgIcon
			&& newToolTip != null && newToolTip.Contains("Ctrl+N") && newToolTip.Contains("new score"),
			$"text='{NewToolButton.ResolvedText}' icon={NewToolButton.IconVisual?.GetType().Name} "
			+ $"tooltip='{newToolTip?.Replace('\n', '|')}'");

		// 34. A plain ICommand disables its button when it says no, and enables it again.
		var printEnabledBefore = PrintToolButton.IsEnabled;
		_model.CanPrint = false;
		await Task.Delay(150);
		var printEnabledAfter = PrintToolButton.IsEnabled;
		_model.CanPrint = true;
		await Task.Delay(150);
		check("fresco-plain-icommand-disables-its-button",
			printEnabledBefore && !printEnabledAfter && PrintToolButton.IsEnabled,
			$"before={printEnabledBefore} said-no={printEnabledAfter} after={PrintToolButton.IsEnabled}");

		// 35. The light/dark pair. Changing the theme swaps the ARTWORK, not just the colour: the
		//     icon re-renders from the other file, and swaps back. The theme the demo STARTS in is
		//     the desktop's, which is why the check states each theme it wants rather than assuming
		//     one - this desktop is normally dark, and the icon is correctly on the dark artwork
		//     before anything is asked of it.
		var newIcon = (SvgIcon)NewToolButton.IconVisual;
		var startingArtwork = newIcon.ResolvedUriSource;
		var startingTheme = newIcon.ActualTheme;

		//The theme is switched on the element the XAML ROOT holds, which is what an application's
		//own light/dark switch does. It is NOT switched on this page: MEASURED, RequestedTheme does
		//not travel down the tree on this platform (FrameworkElement.ActualTheme is the element's
		//own RequestedTheme, or the APPLICATION's theme when that is Default - there is a standing
		//TODO in FrameworkElement.cs saying so), so a page that sets it changes only itself.
		//Setting it on the root element syncs the application theme, and every element that has not
		//asked for a theme of its own then follows.
		var themeRoot = XamlRoot?.Content as FrameworkElement;

		themeRoot.RequestedTheme = ElementTheme.Light;
		await Task.Delay(600);
		var lightArtwork = newIcon.ResolvedUriSource;
		var lightTheme = newIcon.ActualTheme;

		themeRoot.RequestedTheme = ElementTheme.Dark;
		await Task.Delay(600);
		var darkArtwork = newIcon.ResolvedUriSource;
		var darkTheme = newIcon.ActualTheme;

		themeRoot.RequestedTheme = ElementTheme.Default;
		await Task.Delay(600);
		var backAgain = newIcon.ResolvedUriSource;

		check("fresco-svg-icon-light-dark-pair-swaps",
			lightArtwork != null && darkArtwork != null
			&& darkArtwork.LocalPath.EndsWith(".dark.svg", StringComparison.Ordinal)
			&& !lightArtwork.LocalPath.EndsWith(".dark.svg", StringComparison.Ordinal)
			&& backAgain == startingArtwork,
			$"started in {startingTheme} on {Path.GetFileName(startingArtwork?.LocalPath)}; "
			+ $"light[{lightTheme}]={Path.GetFileName(lightArtwork?.LocalPath)} "
			+ $"dark[{darkTheme}]={Path.GetFileName(darkArtwork?.LocalPath)} "
			+ $"back={Path.GetFileName(backAgain?.LocalPath)}");

		// 36. A PNG with an alpha channel, tinted through that alpha.
		var magnifierIcon = MagnifierToolButton.IconVisual as RasterIcon;
		check("fresco-png-icon-with-alpha-paints",
			magnifierIcon != null
			&& magnifierIcon.ResolvedUriSource != null
			&& magnifierIcon.ResolvedUriSource.LocalPath.EndsWith(".png", StringComparison.Ordinal)
			&& magnifierIcon.ActualWidth > 0 && magnifierIcon.Tint != null,
			$"file={Path.GetFileName(magnifierIcon?.ResolvedUriSource?.LocalPath)} "
			+ $"{magnifierIcon?.ActualWidth:0.#}x{magnifierIcon?.ActualHeight:0.#} tinted={magnifierIcon?.Tint != null}");

		// 37. A JPEG, decoded by the same platform decoder with no format-specific code anywhere.
		var jpegIcon = ScoreThumbnailButton.IconVisual as RasterIcon;
		check("fresco-jpeg-icon-paints",
			jpegIcon != null
			&& jpegIcon.ResolvedUriSource != null
			&& jpegIcon.ResolvedUriSource.LocalPath.EndsWith(".jpg", StringComparison.Ordinal)
			&& jpegIcon.ActualWidth > 0,
			$"file={Path.GetFileName(jpegIcon?.ResolvedUriSource?.LocalPath)} "
			+ $"{jpegIcon?.ActualWidth:0.#}x{jpegIcon?.ActualHeight:0.#}");

		// 38. Artwork with no colour of its own, tinted: the pager's two chevrons state
		//     currentColor and each button's tint decides what that is, which is what lets one icon
		//     set serve any theme or accent without a second copy of the file.
		var prevIcon = PreviousPageButton.IconVisual as SvgIcon;
		var nextIcon = NextPageButton.IconVisual as SvgIcon;
		check("fresco-currentcolor-svg-icons-follow-their-tint",
			prevIcon != null && nextIcon != null
			&& prevIcon.Tint != null && nextIcon.Tint != null
			&& !ReferenceEquals(prevIcon.Tint, nextIcon.Tint)
			&& prevIcon.ActualWidth > 0 && nextIcon.ActualWidth > 0,
			$"previous={Path.GetFileName(prevIcon?.ResolvedUriSource?.LocalPath)} "
			+ $"next={Path.GetFileName(nextIcon?.ResolvedUriSource?.LocalPath)} "
			+ $"tinted separately, {prevIcon?.ActualWidth:0}x{prevIcon?.ActualHeight:0} each");

		// 39. The recent-files menu opens behind the Open button's arrow and its items really run
		//     the view model's command with the file they carry.
		OpenToolButton.OpenFlyout();
		await Task.Delay(500);
		var recentOpened = OpenToolButton.IsFlyoutOpen;
		var recentItem = RecentFilesFlyout.Items[0] as MenuFlyoutItem;
		var recentPeer = FrameworkElementAutomationPeer.CreatePeerForElement(recentItem) as MenuFlyoutItemAutomationPeer;
		recentPeer?.Invoke();
		await Task.Delay(400);
		OpenToolButton.CloseFlyout();
		await Task.Delay(300);
		check("fresco-recent-files-menu-opens-and-runs-its-command",
			recentOpened && _model.LastAction.StartsWith("Open recent:", StringComparison.Ordinal),
			$"opened={recentOpened} items={RecentFilesFlyout.Items.Count} action='{_model.LastAction}'");

		// 40. The Save button is split: its main half runs the command, its arrow half opens the
		//     menu, and both halves are on one button.
		var savePeer = FrameworkElementAutomationPeer.CreatePeerForElement(SaveToolButton)
			as ToolDropDownButtonAutomationPeer;
		savePeer?.Invoke();
		await Task.Delay(300);
		var savedByTheMainHalf = _model.LastAction == "Save";
		SaveToolButton.OpenFlyout();
		await Task.Delay(500);
		var arrowOpened = SaveToolButton.IsFlyoutOpen;
		SaveToolButton.CloseFlyout();
		await Task.Delay(300);
		check("fresco-save-split-runs-the-command-and-opens-its-menu",
			savedByTheMainHalf && arrowOpened,
			$"mainHalf='{_model.LastAction}' arrowOpened={arrowOpened}");

		// 41. The Engrave button's menu lists the engrave modes, each with an icon of its own.
		EngraveToolButton.OpenFlyout();
		await Task.Delay(500);
		var modesOpened = EngraveToolButton.IsFlyoutOpen;
		var modeItem = EngraveModesFlyout.Items[1] as MenuFlyoutItem;
		var modePeer = FrameworkElementAutomationPeer.CreatePeerForElement(modeItem) as MenuFlyoutItemAutomationPeer;
		modePeer?.Invoke();
		await Task.Delay(400);
		EngraveToolButton.CloseFlyout();
		await Task.Delay(300);
		check("fresco-engrave-menu-lists-its-modes",
			modesOpened && EngraveModesFlyout.Items.Count == 3
			&& _model.LastAction == "Engrave mode: Publish",
			$"opened={modesOpened} modes={EngraveModesFlyout.Items.Count} action='{_model.LastAction}'");

		// 42. Any control at all can live in a bar. The chooser is an ordinary ComboBox, laid out
		//     and centred by the bar like a button.
		ScoreChooser.SelectedIndex = 2;
		await Task.Delay(250);
		check("fresco-inline-combobox-chooser-selects",
			ScoreChooser.ActualWidth > 100 && ScoreChooser.ActualHeight > 0
			&& _model.SelectedScore == "Clair de Lune",
			$"{ScoreChooser.ActualWidth:0.#}x{ScoreChooser.ActualHeight:0.#} score='{_model.SelectedScore}'");

		// 43. The zoom box is editable: a value chosen from the list reaches the view model, and so
		//     would one typed in.
		ZoomBox.SelectedItem = "150%";
		await Task.Delay(250);
		check("fresco-editable-zoom-combobox-takes-a-value",
			ZoomBox.IsEditable && ZoomBox.ActualWidth > 0 && _model.Zoom == "150%",
			$"editable={ZoomBox.IsEditable} {ZoomBox.ActualWidth:0.#}x{ZoomBox.ActualHeight:0.#} zoom='{_model.Zoom}'");

		// 44. The pager: two commands that say no at the ends, and a label between them.
		var atStart = PreviousPageButton.IsEnabled;
		var nextPeer = FrameworkElementAutomationPeer.CreatePeerForElement(NextPageButton) as ToolButtonAutomationPeer;
		nextPeer?.Invoke();
		await Task.Delay(250);
		var afterOne = _model.PageNumber;
		var backEnabled = PreviousPageButton.IsEnabled;
		check("fresco-pager-advances-and-says-no-at-the-ends",
			!atStart && afterOne == 2 && backEnabled && PageLabel.Text == "2 / 4",
			$"prevAtStart={atStart} page={afterOne} prevNow={backEnabled} label='{PageLabel.Text}'");

		// 45. The magnifier is a checkable button bound two-way to the view model.
		var magnifierPeer = FrameworkElementAutomationPeer.CreatePeerForElement(MagnifierToolButton)
			as ToolToggleButtonAutomationPeer;
		magnifierPeer?.Toggle();
		await Task.Delay(250);
		check("fresco-magnifier-toggles-through-the-view-model",
			MagnifierToolButton.IsChecked && _model.IsMagnifierOn,
			$"isChecked={MagnifierToolButton.IsChecked} viewModel={_model.IsMagnifierOn}");

		// 46. The label switch. One setting on the TRAY reaches every button in both bars, at run
		//     time - which is what a "show button text" preference needs.
		var narrow = NewToolButton.ActualWidth;
		VerboseCheck.IsChecked = true;
		UpdateLayout();
		await Task.Delay(400);
		var wide = NewToolButton.ActualWidth;
		var musicButtonFollowed = ScoreThumbnailButton.TextVisibility == Visibility.Visible;
		VerboseCheck.IsChecked = false;
		UpdateLayout();
		await Task.Delay(400);
		check("fresco-labelmode-switch-relabels-both-bars",
			wide > narrow && musicButtonFollowed && NewToolButton.ActualWidth < wide,
			$"iconOnly={narrow:0.#} withText={wide:0.#} otherBarFollowed={musicButtonFollowed} "
			+ $"backAgain={NewToolButton.ActualWidth:0.#}");

		// 47. The tooltip switch, the same way: off on the tray silences every button in it.
		var tipBefore = ToolTipService.GetToolTip(PrintToolButton) as string;
		ToolTipsCheck.IsChecked = false;
		await Task.Delay(300);
		var tipAfter = ToolTipService.GetToolTip(PrintToolButton);
		ToolTipsCheck.IsChecked = true;
		await Task.Delay(300);
		check("fresco-showtooltips-switch-silences-the-bars",
			tipBefore == "Print (Ctrl+P)" && tipAfter == null
			&& ToolTipService.GetToolTip(PrintToolButton) as string == "Print (Ctrl+P)",
			$"before='{tipBefore}' off={(tipAfter == null ? "null" : tipAfter.ToString())} "
			+ $"back='{ToolTipService.GetToolTip(PrintToolButton)}'");

		// 48. The keyboard opens a drop-down button's menu: a REAL Down key, sent to the real
		//     window, on a ToolDropDownButton focused in the bar. The bar has to recognise the
		//     button's OWN Flyout, not only an attached one.
		EngraveToolButton.Focus(FocusState.Keyboard);
		await Task.Delay(300);
		var keySent = SendKeyToWindow(_ownWindow, "Down");
		await Task.Delay(700);
		var keyboardOpened = EngraveToolButton.IsFlyoutOpen;
		EngraveToolButton.CloseFlyout();
		await Task.Delay(400);
		check("fresco-keyboard-opens-a-drop-down-in-the-bar",
			keySent && keyboardOpened,
			$"sent={keySent} window={_ownWindow ?? "none"} opened={keyboardOpened}");

		// 49. A modifier-aware click: a modifier really held down while the button is activated,
		//     read AT THE CLICK rather than remembered from the last key event. This is the
		//     Shift-click a score editor uses for "engrave with options".
		//What is ASSERTED is Shift and Control: the two a score editor really binds, and the two no
		//window manager takes for itself over a client area. Alt is measured LAST and only reported,
		//because what happens to it is decided by the DESKTOP rather than by the platform - this one
		//uses Alt as its window-drag modifier, so it takes the Alt press and the application never
		//sees the activation at all. The check that follows proves that what the desktop swallowed
		//cannot leak into the next click.
		var shiftClick = await ClickWithModifierAsync("shift");
		var controlClick = await ClickWithModifierAsync("ctrl");
		var plainClick = await ClickWithModifierAsync(null);
		var altClick = await ClickWithModifierAsync("alt");
		var dragModifier = RunShell(
			"gsettings get org.cinnamon.desktop.wm.preferences mouse-button-modifier").Trim();

		check("fresco-modifier-aware-click-reports-the-modifiers-held",
			shiftClick.HasFlag(VirtualKeyModifiers.Shift)
			&& controlClick.HasFlag(VirtualKeyModifiers.Control)
			&& plainClick == VirtualKeyModifiers.None,
			$"shift-click={shiftClick} control-click={controlClick} plain-click={plainClick}; "
			+ $"alt-click={altClick} (informational: this desktop's window-drag modifier is "
			+ $"{(dragModifier.Length == 0 ? "unreported" : dragModifier)}, so it takes the Alt "
			+ "press for itself and no Alt activation ever reaches the application)");

		// 50. A modifier the desktop swallowed must not stick. Alt+Space is this desktop's window
		//     menu: the application sees the Alt press and never the Alt release, so a modifier
		//     state fed only by key down and key up would report Alt held on every later click until
		//     the window was deactivated. The state has to follow the modifier MASK that the next
		//     event carries, so a plain activation straight afterwards reports no modifiers at all.
		RunShell($"xdotool windowactivate --sync {_ownWindow}; xdotool keydown alt; sleep 0.3; "
			+ "xdotool key space; sleep 0.5; xdotool keyup alt");
		await Task.Delay(800);

		//Dismiss whatever the desktop opened over the demo, and give the window back to the page.
		RunShell($"xdotool key --clearmodifiers Escape; xdotool windowactivate --sync {_ownWindow}");
		await Task.Delay(500);
		var afterSwallowedRelease = await ClickWithModifierAsync(null);

		check("fresco-swallowed-modifier-release-does-not-stick",
			afterSwallowedRelease == VirtualKeyModifiers.None,
			"after alt+space - which this desktop keeps for its window menu, so the Alt release "
			+ $"never arrives - a plain activation read {afterSwallowedRelease}");

		// 51. The X11 head's own modifier translation, which the modifier-aware click rests on:
		//     Alt (X11's Mod1) has to arrive as VirtualKeyModifiers.Menu. It used to arrive as
		//     Shift, and an access key - which is Alt plus a letter, and is resolved from the
		//     modifiers the head reports rather than from the key-state table - could therefore
		//     never fire on X11. The value itself is internal to the framework; an access key is
		//     the public behaviour that depends on it.
		_accessKeyInvocations = 0;
		_lastKey = VirtualKey.None;
		for (var attempt = 0; attempt < 4 && _accessKeyInvocations == 0; attempt++)
		{
			RunShell($"xdotool windowactivate --sync {_ownWindow}; sleep 0.4; xdotool key alt+g");
			await Task.Delay(900);
		}

		check("fresco-x11-alt-invokes-an-access-key",
			_accessKeyInvocations > 0,
			$"AccessKey='{PrintToolButton.AccessKey}' invocations={_accessKeyInvocations} "
			+ $"lastKey={_lastKey}; Alt must arrive as VirtualKeyModifiers.Menu for this to resolve");

		// 52. A REAL window shrink, through the window manager, makes the chevron appear on the
		//     reference bar - the whole point of the overflow.
		var sizeBefore = XamlRoot?.Size ?? default;
		RunShell($"xdotool windowsize --sync {_ownWindow} 420 900");
		await Task.Delay(1200);
		UpdateLayout();
		await Task.Delay(400);
		var chevron = FindChevron(MainToolBar);
		check("fresco-window-shrink-shows-the-chevron",
			MainToolBar.HasOverflowItems && chevron != null && chevron.ActualWidth > 0,
			$"window={sizeBefore.Width:0}x{sizeBefore.Height:0} -> "
			+ $"{XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0} "
			+ $"hasOverflow={MainToolBar.HasOverflowItems} "
			+ $"chevron={(chevron == null ? "absent" : $"{chevron.ActualWidth:0.#}x{chevron.ActualHeight:0.#}")}");

		// 53. What is BEHIND the chevron still belongs to the bar: the seam wave 3 closed. An
		//     overflowed button keeps the bar's label mode and still follows its command.
		VerboseCheck.IsChecked = true;
		UpdateLayout();
		await Task.Delay(500);
		var overflowed = ButtonsIn(OverflowChildren(MainToolBar));
		var overflowedLabelled = overflowed.Count > 0
			&& overflowed.TrueForAll(b => b.TextVisibility == Visibility.Visible);
		var commandFollower = overflowed.Find(b => ReferenceEquals(b.Command, _model.PrintCommand));
		var enabledInOverflow = commandFollower?.IsEnabled;
		_model.CanPrint = false;
		await Task.Delay(300);
		var disabledInOverflow = commandFollower?.IsEnabled;
		_model.CanPrint = true;
		await Task.Delay(300);
		check("fresco-overflowed-buttons-keep-the-bars-settings-and-their-commands",
			overflowed.Count > 0 && overflowedLabelled
			&& commandFollower != null && enabledInOverflow == true && disabledInOverflow == false,
			$"inOverflow={overflowed.Count} labelled={overflowedLabelled} "
			+ $"printFound={commandFollower != null} enabled={enabledInOverflow} "
			+ $"afterCanExecuteFalse={disabledInOverflow}");

		// 54. The chevron's flyout paints the moved items on the head.
		var flyoutOpened = MainToolBar.ShowOverflow();
		await Task.Delay(900);
		var paintedInFlyout = overflowed.Count > 0 && overflowed[0].ActualWidth > 0;
		check("fresco-chevron-flyout-paints-the-overflowed-items",
			flyoutOpened && paintedInFlyout,
			$"opened={flyoutOpened} first={overflowed.FirstOrDefault()?.ActualWidth:0.#}"
			+ $"x{overflowed.FirstOrDefault()?.ActualHeight:0.#}");

		SendKeyToWindow(_ownWindow, "Escape");
		await Task.Delay(400);
		VerboseCheck.IsChecked = false;

		// 55. The window really painted. A capture of THIS window - not the root, which on this
		//     compositing window manager comes back without window contents - is searched for the
		//     colour the reference tray sits on, and the count is compared with the area the tray
		//     was arranged at.
		RunShell($"xdotool windowsize --sync {_ownWindow} 1100 900");
		await Task.Delay(1200);
		UpdateLayout();
		await Task.Delay(500);

		var capture = Path.Combine(Path.GetTempPath(), "commandbardemo-capture.png");
		var expectedPixels = FrescoHost.ActualWidth * FrescoHost.ActualHeight
			* (XamlRoot?.RasterizationScale ?? 1d) * (XamlRoot?.RasterizationScale ?? 1d);
		var counted = CaptureAndCount(_ownWindow, capture, TrayColourInCapture);

		//What is VISIBLE of the tray's own background is the frame around the bars and the gaps
		//between them - the bars cover the rest - so the count is compared with a floor rather than
		//with the tray's whole area.
		check("fresco-window-capture-shows-the-tray",
			counted > 1000,
			$"{counted} pixels of {TrayColourInCapture} in {capture}; the tray arranged "
			+ $"{FrescoHost.ActualWidth:0}x{FrescoHost.ActualHeight:0} logical "
			+ $"({expectedPixels:0} device px in all, most of it covered by the bars themselves)");
	}

	private static IEnumerable<UIElement> ItemsHostChildren(ToolBar bar)
	{
		var panel = FindDescendant<ToolBarPanel>(bar);

		return panel?.Children ?? Enumerable.Empty<UIElement>();
	}

	private static IEnumerable<UIElement> OverflowChildren(ToolBar bar) => bar.OverflowItems;

	/// <summary>Every tool button among the given elements, including the ones inside a group.</summary>
	/// <param name="elements">The elements to look through.</param>
	/// <returns>The buttons found, in order.</returns>
	private static List<ToolButton> ButtonsIn(IEnumerable<UIElement> elements)
	{
		var found = new List<ToolButton>();
		Collect(elements, found);

		return found;

		static void Collect(IEnumerable<UIElement> from, List<ToolButton> into)
		{
			foreach (var element in from)
			{
				if (element is ToolButton button)
				{
					into.Add(button);
				}
				else if (element is Panel panel)
				{
					//A group travels into the overflow whole, so its buttons went with it.
					Collect(panel.Children, into);
				}
			}
		}
	}


	private static ToolBarOverflowButton FindChevron(ToolBar bar)
		=> ItemsHostChildren(bar).OfType<ToolBarOverflowButton>().FirstOrDefault();

	private static T FindDescendant<T>(DependencyObject root) where T : class
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T match)
			{
				return match;
			}

			var deeper = FindDescendant<T>(child);
			if (deeper != null)
			{
				return deeper;
			}
		}

		return null;
	}

	private static double OffsetIn(UIElement bar, UIElement element)
		=> element.TransformToVisual(bar).TransformPoint(new Point(0, 0)).X;

	/// <summary>
	/// Lists the visible windows this process owns, newest first.
	/// </summary>
	/// <returns>The window ids to try, in the order to try them.</returns>
	/// <remarks>
	/// The candidates are the windows the desktop attributes to this PROCESS ID, never the windows
	/// carrying this demo's title: another head running the same demo is titled the same, and a
	/// title is not an identity. Which of them is this page's own is then settled by the focus probe
	/// in the keyboard check - the window whose key press moves THIS page's focus.
	/// </remarks>
	private static IEnumerable<string> ListDemoWindows()
	{
		var output = RunShell($"xdotool search --onlyvisible --pid {Environment.ProcessId}");

		//Window ids ascend with creation order, so the newest - the one the page is in, when a head
		//opened more than one - is tried first.
		return output
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0)
			.Reverse()
			.ToArray();
	}

	/// <summary>
	/// Activates the Print button from the keyboard with one modifier really held down, and reports
	/// what the button saw.
	/// </summary>
	/// <param name="modifier">The xdotool modifier name, or null for a plain activation.</param>
	/// <returns>The modifiers the button read at the click.</returns>
	private async Task<VirtualKeyModifiers> ClickWithModifierAsync(string modifier)
	{
		//Synthetic input is not always delivered first time - the window manager may still be
		//settling an activation - so the attempt is repeated until the button really was clicked,
		//and what is reported is the modifier state of the attempt that landed.
		for (var attempt = 0; attempt < 3; attempt++)
		{
			_lastPrintClickModifiers = VirtualKeyModifiers.None;
			_printClicks = 0;

			RunShell($"xdotool windowactivate --sync {_ownWindow}; xdotool windowfocus --sync {_ownWindow}");
			await Task.Delay(300);
			PrintToolButton.Focus(FocusState.Keyboard);
			await Task.Delay(300);

			RunShell(modifier == null
				? "xdotool key --clearmodifiers space"
				: $"xdotool keydown {modifier}; sleep 0.3; xdotool key space; sleep 0.3; xdotool keyup {modifier}");
			await Task.Delay(800);

			if (_printClicks > 0)
			{
				break;
			}
		}

		return _lastPrintClickModifiers;
	}

	private static bool SendKeyToWindow(string windowId, string key)
	{
		if (string.IsNullOrEmpty(windowId))
		{
			return false;
		}

		var script =
			$"xdotool windowactivate --sync {windowId}; "
			+ $"xdotool windowfocus --sync {windowId}; "
			+ "sleep 0.3; "
			+ $"xdotool key --clearmodifiers {key}";

		return RunShell(script) != null;
	}

	/// <summary>
	/// Captures one window and counts the pixels of one colour in it.
	/// </summary>
	/// <param name="windowId">The window to capture.</param>
	/// <param name="path">Where to write the capture.</param>
	/// <param name="hexColour">The colour to count, as "#RRGGBB".</param>
	/// <returns>How many pixels of that colour the capture holds, or -1 when it could not be taken.</returns>
	/// <remarks>
	/// The capture is of the WINDOW, not of the root: this window manager composites, and a root
	/// capture comes back without window contents at all - measured, and the reason every capture
	/// in this demo names a window id.
	/// </remarks>
	private static int CaptureAndCount(string windowId, string path, string hexColour)
	{
		if (string.IsNullOrEmpty(windowId))
		{
			return -1;
		}

		var output = RunShell(
			$"import -window {windowId} '{path}' && convert '{path}' -depth 8 txt:- | grep -c -- '{hexColour}'");

		return int.TryParse(output.Trim(), out var count) ? count : -1;
	}

	private static string RunShell(string script)
	{
		try
		{
			var process = Process.Start(new ProcessStartInfo("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\"")
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			});

			if (process == null)
			{
				return string.Empty;
			}

			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit(20000);

			return output;
		}
		catch (Exception)
		{
			//A head with no xdotool cannot drive a window; the check reports that rather than
			//bringing the whole self-test down.
			return string.Empty;
		}
	}

	/// <summary>
	/// A tool bar icon that draws a filled square, so the icon slot can be proven on the head
	/// without waiting for the real SVG and raster sources.
	/// </summary>
	private sealed class DemoSquareIconSource : ToolIconSource
	{
		protected override IconElement CreateIconElementCore()
		{
			var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = true, IsFilled = true };
			figure.Segments.Add(new LineSegment { Point = new Point(16, 0) });
			figure.Segments.Add(new LineSegment { Point = new Point(16, 16) });
			figure.Segments.Add(new LineSegment { Point = new Point(0, 16) });

			var geometry = new PathGeometry();
			geometry.Figures.Add(figure);

			return new PathIcon { Data = geometry };
		}
	}

	/// <summary>A command whose answer to CanExecute the self-test can change.</summary>
	private sealed class SwitchableDemoCommand : System.Windows.Input.ICommand
	{
		private bool _canExecute = true;

		public event EventHandler CanExecuteChanged;

		public int ExecutionCount { get; private set; }

		public void SetCanExecute(bool value)
		{
			_canExecute = value;
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool CanExecute(object parameter) => _canExecute;

		public void Execute(object parameter) => ExecutionCount++;
	}
}
