using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Toolkit;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

// ReSharper disable CheckNamespace

namespace TriPaneViewDemo.Views;

/// <summary>
/// The TriPaneView demo page: a control strip over a TriPaneView that fills the rest of the window.
/// Set TRIPANEVIEWDEMO_SELFTEST=1 to run the scripted checks on screen and exit with the number of
/// failures; TRIPANEVIEWDEMO_RESULTS names a file to write the PASS/FAIL lines to.
/// </summary>
/// <remarks>
/// The self-test drives the dividers with real pointer input through
/// Windows.UI.Input.Preview.Injection.InputInjector and resizes the real window with xdotool, so
/// what it proves is the whole path: the default style found and applied, the template parts wired,
/// the pointer drag reaching the divider, the weights written back, and the columns and rows the
/// grid actually arranged.
/// </remarks>
public sealed partial class MainPage : Page
{
	/// <summary>How many rows the side pane's list carries, so the pane certainly overflows.</summary>
	private const int SideListRowCount = 60;

	/// <summary>The text the upper pane's text box has to still be carrying after a round trip.</summary>
	private const string SurvivalText = "This text was typed before the pane was minimized.";

	/// <summary>The pane length, in pixels, the min-length checks put the floor at.</summary>
	private const double SideMinLengthUnderTest = 200d;

	private readonly StringBuilder _log = new();

	private Grid _rootGrid;
	private Grid _stackGrid;
	private ScrollViewer _sidePaneScrollViewer;
	private ScrollViewer _upperPaneScrollViewer;
	private ScrollViewer _lowerPaneScrollViewer;
	private TriPaneViewDivider _sideDivider;
	private TriPaneViewDivider _stackDivider;

	private DemoMouse _mouse;
	private bool _isBuildingControls = true;
	private bool _hooked;
	private int _dragCompletedCount;

	/// <summary>This process's own window, once a probe has proven which one it is.</summary>
	private string _ownWindow;

	public MainPage()
	{
		InitializeComponent();

		BuildSideList();
		BuildControlStrip();

		SurvivalTextBox.Text = SurvivalText;
		Panes.DividerDragCompleted += OnDividerDragCompleted;

		Loaded += OnPageLoaded;
	}

	#region The demo itself

	/// <summary>
	/// Fills the side pane with enough rows that it certainly does not fit, which is what makes the
	/// pane's own vertical scrolling visible.
	/// </summary>
	private void BuildSideList()
	{
		for (var row = 1; row <= SideListRowCount; row++)
		{
			SideList.Children.Add(new TextBlock { Text = $"Side pane row {row:00}" });
		}
	}

	/// <summary>
	/// Gives the strip's combo boxes their choices and points them at the values the XAML already
	/// declared, so the strip opens showing what the control is actually set to.
	/// </summary>
	private void BuildControlStrip()
	{
		GripModeCombo.ItemsSource = Enum.GetNames(typeof(TriPaneViewRestoreGripMode));
		SideScrollCombo.ItemsSource = Enum.GetNames(typeof(TriPaneViewHorizontalScrollMode));
		UpperScrollCombo.ItemsSource = Enum.GetNames(typeof(TriPaneViewHorizontalScrollMode));
		LowerScrollCombo.ItemsSource = Enum.GetNames(typeof(TriPaneViewHorizontalScrollMode));

		GripModeCombo.SelectedItem = Panes.RestoreGripMode.ToString();
		SideScrollCombo.SelectedItem = Panes.SidePaneHorizontalScrollMode.ToString();
		UpperScrollCombo.SelectedItem = Panes.UpperPaneHorizontalScrollMode.ToString();
		LowerScrollCombo.SelectedItem = Panes.LowerPaneHorizontalScrollMode.ToString();

		_isBuildingControls = false;
	}

	private void OnDividerDragCompleted(object sender, TriPaneViewDividerDragCompletedEventArgs args)
	{
		_dragCompletedCount++;
		Log($"DividerDragCompleted: {args.Divider} divider, side={Panes.SidePanePercent:0.#} "
			+ $"stack={Panes.StackPercent:0.#} upper={Panes.UpperPanePercent:0.#} "
			+ $"lower={Panes.LowerPanePercent:0.#}");
	}

	private void OnPlacementChanged(object sender, RoutedEventArgs e)
	{
		if (_isBuildingControls)
		{
			return;
		}

		Panes.SidePanePlacement = PlacementRightCheck.IsChecked == true
			? TriPaneViewSidePanePlacement.Right
			: TriPaneViewSidePanePlacement.Left;
	}

	private void OnDividerOptionChanged(object sender, RoutedEventArgs e)
	{
		if (_isBuildingControls)
		{
			return;
		}

		Panes.CanUserDragSideDivider = DragSideCheck.IsChecked == true;
		Panes.CanUserDragStackDivider = DragStackCheck.IsChecked == true;
		Panes.IsDragToMinimizeEnabled = DragToMinimizeCheck.IsChecked == true;
	}

	private void OnGripModeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isBuildingControls || GripModeCombo.SelectedItem is not string mode)
		{
			return;
		}

		Panes.RestoreGripMode = Enum.Parse<TriPaneViewRestoreGripMode>(mode);
	}

	private void OnScrollModeChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isBuildingControls)
		{
			return;
		}

		if (SideScrollCombo.SelectedItem is string side)
		{
			Panes.SidePaneHorizontalScrollMode = Enum.Parse<TriPaneViewHorizontalScrollMode>(side);
		}

		if (UpperScrollCombo.SelectedItem is string upper)
		{
			Panes.UpperPaneHorizontalScrollMode = Enum.Parse<TriPaneViewHorizontalScrollMode>(upper);
		}

		if (LowerScrollCombo.SelectedItem is string lower)
		{
			Panes.LowerPaneHorizontalScrollMode = Enum.Parse<TriPaneViewHorizontalScrollMode>(lower);
		}
	}

	private void OnMinimizeSide(object sender, RoutedEventArgs e) => Panes.MinimizeSidePane();

	private void OnRestoreSide(object sender, RoutedEventArgs e) => Panes.RestoreSidePane();

	private void OnMinimizeUpper(object sender, RoutedEventArgs e) => Panes.MinimizeUpperPane();

	private void OnRestoreUpper(object sender, RoutedEventArgs e) => Panes.RestoreUpperPane();

	private void OnMinimizeLower(object sender, RoutedEventArgs e) => Panes.MinimizeLowerPane();

	private void OnRestoreLower(object sender, RoutedEventArgs e) => Panes.RestoreLowerPane();

	private void OnRestoreAll(object sender, RoutedEventArgs e) => Panes.RestoreAll();

	#endregion

	#region The self-test

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		if (_hooked)
		{
			return;
		}

		_hooked = true;
		Log($"Loaded. scale={XamlRoot?.RasterizationScale:0.##} "
			+ $"size={XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0}");

		if (Environment.GetEnvironmentVariable("TRIPANEVIEWDEMO_SELFTEST") == "1")
		{
			_ = RunSelfTestAsync();
		}
	}

	private void Log(string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
		_log.AppendLine(line);
		Console.WriteLine($"TRIPANEVIEW|{line}");
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

			//The divider cursor is set through UIElement.ProtectedCursor, which no public API reads
			//back and no capture can show; it is left to the eye, as the spec says.
			Log("SELFTEST: the resize cursor is not observable from managed code - skipped by design");

			await RunTemplateChecksAsync(Check);
		}
		catch (Exception ex)
		{
			results.Add($"FAIL selftest-exception ({ex.GetType().Name}: {ex.Message})");
			Log($"SELFTEST: exception {ex}");
		}

		var resultsPath = Environment.GetEnvironmentVariable("TRIPANEVIEWDEMO_RESULTS");
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
	/// Runs every check that needs a live template: the parts, the measured proportions, the two
	/// divider drags, the minimum-length rules, the restore grip, content survival, the placement
	/// swap and the portrait rule.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunTemplateChecksAsync(Action<string, bool, string> check)
	{
		// 1. The Toolkit's default style was found and applied, and every part the control asks for
		//    is really in the tree. Nothing below this can mean anything if it is not.
		var partsFound = ResolveParts();
		check("template-applied-and-parts-found",
			Panes.Template != null && partsFound,
			$"template={(Panes.Template == null ? "null" : "applied")} root={Describe(_rootGrid)} "
			+ $"columns={_rootGrid?.ColumnDefinitions.Count ?? -1} rows={_stackGrid?.RowDefinitions.Count ?? -1} "
			+ $"sideDivider={Describe(_sideDivider)} stackDivider={Describe(_stackDivider)} "
			+ $"panes={Describe(_sidePaneScrollViewer)}/{Describe(_upperPaneScrollViewer)}/"
			+ $"{Describe(_lowerPaneScrollViewer)}");

		if (!partsFound)
		{
			check("template-parts-needed-by-the-remaining-checks", false,
				"the remaining checks were not run because the template parts were not found");

			return;
		}

		// 2. The default weights reach the SCREEN: 33.3 / 66.7 across, 50 / 50 down the stack,
		//    measured on the arranged panes rather than read back off the properties.
		var sideWidth = _sidePaneScrollViewer.ActualWidth;
		var stackWidth = _stackGrid.ActualWidth;
		var sideShare = sideWidth / (sideWidth + stackWidth) * 100d;
		var upperHeight = _upperPaneScrollViewer.ActualHeight;
		var lowerHeight = _lowerPaneScrollViewer.ActualHeight;

		check("default-weights-reach-the-measured-panes",
			Math.Abs(sideShare - Panes.SidePanePercent) < 1.5d
			&& Math.Abs(upperHeight - lowerHeight) < 2d
			&& _sideDivider.ActualWidth > 0
			&& _stackDivider.ActualHeight > 0,
			$"side={sideWidth:0.#} stack={stackWidth:0.#} -> {sideShare:0.##}% "
			+ $"(SidePanePercent={Panes.SidePanePercent:0.##}); upper={upperHeight:0.#} "
			+ $"lower={lowerHeight:0.#}; dividers={_sideDivider.ActualWidth:0.#}x"
			+ $"{_sideDivider.ActualHeight:0.#} / {_stackDivider.ActualWidth:0.#}x"
			+ $"{_stackDivider.ActualHeight:0.#}");

		// 3. The side pane really scrolls: its content is taller than the viewport it is given.
		check("side-pane-list-scrolls-vertically",
			_sidePaneScrollViewer.ExtentHeight > _sidePaneScrollViewer.ViewportHeight + 1d,
			$"extent={_sidePaneScrollViewer.ExtentHeight:0.#} "
			+ $"viewport={_sidePaneScrollViewer.ViewportHeight:0.#} rows={SideListRowCount}");

		var injector = InputInjector.TryCreate();
		_mouse = injector == null ? null : new DemoMouse(injector);

		if (_mouse == null)
		{
			check("pointer-injection-available", false,
				"InputInjector.TryCreate() returned null, so no drag could be performed");
		}
		else
		{
			await RunDragChecksAsync(check);
		}

		await RunMinimizeChecksAsync(check);
		await RunPlacementAndPortraitChecksAsync(check);
	}

	/// <summary>
	/// The drags: a real pointer press, a real move and a real release on each divider, and then the
	/// minimum-length rules that only a drag can reach.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunDragChecksAsync(Action<string, bool, string> check)
	{
		// 4. The side divider drag: the pointer moves 120 pixels and the side pane grows by exactly
		//    that, the pair is written back normalized to 100, and the column the grid arranged
		//    followed.
		var completedBefore = _dragCompletedCount;
		var percentBefore = Panes.SidePanePercent;
		var widthBefore = _sidePaneScrollViewer.ActualWidth;

		await DragAsync(_sideDivider, 120d, 0d);

		var widthAfter = _sidePaneScrollViewer.ActualWidth;

		check("side-divider-drag-moves-the-weights-and-the-columns",
			Panes.SidePanePercent > percentBefore + 3d
			&& Math.Abs(widthAfter - (widthBefore + 120d)) < 4d
			&& Math.Abs(Panes.SidePanePercent + Panes.StackPercent - 100d) < 0.01d
			&& Math.Abs(SideColumnActualLength() - widthAfter) < 1d,
			$"percent {percentBefore:0.##} -> {Panes.SidePanePercent:0.##} "
			+ $"(+stack {Panes.StackPercent:0.##}); width {widthBefore:0.#} -> {widthAfter:0.#} "
			+ $"for a 120 pixel drag; column={SideColumnActualLength():0.#}");

		// 5. The drag is reported: DividerDragCompleted is the hook an application persists from.
		check("divider-drag-completed-is-raised",
			_dragCompletedCount == completedBefore + 1,
			$"raised {_dragCompletedCount - completedBefore} time(s)");

		// 6. The percent boxes are bound two-way, so a drag - which writes the properties - moves
		//    the numbers on screen without the demo touching them.
		check("percent-boxes-follow-the-drag-two-way",
			Math.Abs(SidePercentBox.Value - Panes.SidePanePercent) < 0.01d
			&& Math.Abs(StackPercentBox.Value - Panes.StackPercent) < 0.01d,
			$"box={SidePercentBox.Value:0.####}/{StackPercentBox.Value:0.####} "
			+ $"control={Panes.SidePanePercent:0.####}/{Panes.StackPercent:0.####}");

		// 7. The stack divider drag: the same, one axis over.
		var upperBefore = _upperPaneScrollViewer.ActualHeight;
		var upperPercentBefore = Panes.UpperPanePercent;

		await DragAsync(_stackDivider, 0d, -80d);

		var upperAfter = _upperPaneScrollViewer.ActualHeight;

		check("stack-divider-drag-moves-the-weights-and-the-rows",
			Panes.UpperPanePercent < upperPercentBefore - 3d
			&& Math.Abs(upperAfter - (upperBefore - 80d)) < 4d
			&& Math.Abs(Panes.UpperPanePercent + Panes.LowerPanePercent - 100d) < 0.01d
			&& Math.Abs(_stackGrid.RowDefinitions[0].ActualHeight - upperAfter) < 1d,
			$"percent {upperPercentBefore:0.##} -> {Panes.UpperPanePercent:0.##} "
			+ $"(+lower {Panes.LowerPanePercent:0.##}); upper {upperBefore:0.#} -> {upperAfter:0.#} "
			+ $"for a -80 pixel drag; row={_stackGrid.RowDefinitions[0].ActualHeight:0.#}");

		// 8. The floor. With drag-to-minimize off, a drag that asks for less than the minimum length
		//    stops dead at the minimum and the pane stays open.
		Panes.SidePaneMinLength = SideMinLengthUnderTest;
		Panes.IsDragToMinimizeEnabled = false;
		await SettleAsync();

		var beforeFloor = _sidePaneScrollViewer.ActualWidth;
		await DragAsync(_sideDivider, -(beforeFloor - 100d), 0d);
		var atFloor = _sidePaneScrollViewer.ActualWidth;

		check("min-length-is-a-floor-while-drag-to-minimize-is-off",
			!Panes.IsSidePaneMinimized
			&& Math.Abs(atFloor - SideMinLengthUnderTest) < 3d
			&& Panes.SidePanePercent > 0d,
			$"MinLength={SideMinLengthUnderTest:0} asked for 100, got {atFloor:0.#} "
			+ $"(from {beforeFloor:0.#}); minimized={Panes.IsSidePaneMinimized} "
			+ $"percent={Panes.SidePanePercent:0.##}");

		// 9. The snap. With drag-to-minimize on, the same drag - one that asks for less than the
		//    minimum - takes the pane to zero instead, and the pane reports itself minimized.
		Panes.IsDragToMinimizeEnabled = true;
		await SettleAsync();

		var beforeSnap = _sidePaneScrollViewer.ActualWidth;
		await DragAsync(_sideDivider, -(beforeSnap - 100d), 0d);

		check("drag-to-minimize-snaps-a-pane-below-its-minimum-to-zero",
			Panes.IsSidePaneMinimized
			&& Panes.SidePanePercent == 0d
			&& _sidePaneScrollViewer.ActualWidth == 0d
			&& SideColumnActualLength() == 0d,
			$"asked for 100 with MinLength={SideMinLengthUnderTest:0}: "
			+ $"percent={Panes.SidePanePercent:0.##} width={_sidePaneScrollViewer.ActualWidth:0.#} "
			+ $"column={SideColumnActualLength():0.#} minimized={Panes.IsSidePaneMinimized}");

		// 10. A pane the USER shut keeps its grip under the Auto mode, and the grip is the divider
		//     itself, still on screen at the edge the pane collapsed to.
		check("auto-mode-keeps-a-grip-on-a-drag-minimized-pane",
			_sideDivider.Visibility == Visibility.Visible
			&& _sideDivider.IsRestoreGrip
			&& _sideDivider.IsGripTowardStart
			&& Panes.RestoreGripMode == TriPaneViewRestoreGripMode.Auto,
			$"visibility={_sideDivider.Visibility} grip={_sideDivider.IsRestoreGrip} "
			+ $"towardStart={_sideDivider.IsGripTowardStart} mode={Panes.RestoreGripMode}");

		// 11. Clicking that grip - a press and a release with no movement at all - brings the pane
		//     back. This is the tap the divider has to tell apart from a drag.
		await ClickAsync(_sideDivider);

		check("a-click-on-the-restore-grip-reopens-the-pane",
			!Panes.IsSidePaneMinimized
			&& Panes.SidePanePercent > 0d
			&& _sidePaneScrollViewer.ActualWidth > 0d,
			$"percent={Panes.SidePanePercent:0.##} width={_sidePaneScrollViewer.ActualWidth:0.#} "
			+ $"minimized={Panes.IsSidePaneMinimized}");

		Panes.SidePaneMinLength = 0d;
		Panes.IsDragToMinimizeEnabled = false;
		await SettleAsync();
	}

	/// <summary>
	/// The minimize state model as it looks through the template: the grip rules for each mode and
	/// each cause, and the guarantee that a minimized pane keeps its own content elements.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunMinimizeChecksAsync(Action<string, bool, string> check)
	{
		// 12. A pane the CODE shut gets no grip under Auto, and with no grip the divider goes away
		//     too and the sibling takes the whole width.
		Panes.RestoreGripMode = TriPaneViewRestoreGripMode.Auto;
		Panes.MinimizeSidePane();
		await SettleAsync();

		var stackWidthWhenSideShut = _stackGrid.ActualWidth;

		check("auto-mode-gives-no-grip-to-a-code-minimized-pane",
			Panes.IsSidePaneMinimized
			&& !_sideDivider.IsRestoreGrip
			&& _sideDivider.Visibility == Visibility.Collapsed
			&& Math.Abs(stackWidthWhenSideShut - Panes.ActualWidth) < 1d,
			$"minimized={Panes.IsSidePaneMinimized} grip={_sideDivider.IsRestoreGrip} "
			+ $"visibility={_sideDivider.Visibility} stack={stackWidthWhenSideShut:0.#} "
			+ $"of {Panes.ActualWidth:0.#}");

		// 13. Always means always: the same code-minimized pane gets its grip back the moment the
		//     mode changes, with no other state touched.
		Panes.RestoreGripMode = TriPaneViewRestoreGripMode.Always;
		await SettleAsync();

		check("always-mode-shows-a-grip-for-a-code-minimized-pane",
			Panes.IsSidePaneMinimized
			&& _sideDivider.IsRestoreGrip
			&& _sideDivider.Visibility == Visibility.Visible,
			$"grip={_sideDivider.IsRestoreGrip} visibility={_sideDivider.Visibility}");

		// 14. Never means never, even for the cause Auto would have shown a grip for: the pane is
		//     dragged shut and still gets nothing. This is also the mode in which the divider is due
		//     to disappear the moment the pane reaches zero - so the same gesture proves it does NOT
		//     disappear while the pointer is still holding it (15), and only goes once the drag ends.
		Panes.RestoreGripMode = TriPaneViewRestoreGripMode.Never;
		Panes.RestoreSidePane();
		await SettleAsync();

		if (_mouse != null)
		{
			var openWidth = _sidePaneScrollViewer.ActualWidth;

			await PressAsync(_sideDivider);
			await MoveByAsync(-(openWidth + 40d), 0d);

			var heldVisibility = _sideDivider.Visibility;
			var heldWidth = _sideDivider.ActualWidth;
			var heldEnabled = _sideDivider.IsEnabled;
			var heldMinimized = Panes.IsSidePaneMinimized;

			await MoveByAsync(openWidth + 40d, 0d);

			var reopenedPercent = Panes.SidePanePercent;

			await MoveByAsync(-(openWidth + 40d), 0d);
			await ReleaseAsync();

			check("a-divider-stays-under-the-pointer-while-it-drags-a-pane-shut",
				heldVisibility == Visibility.Visible
				&& heldWidth > 0d
				&& heldEnabled
				&& heldMinimized
				&& reopenedPercent > 0d,
				$"mid-gesture with mode={Panes.RestoreGripMode}: visibility={heldVisibility} "
				+ $"width={heldWidth:0.#} enabled={heldEnabled} minimized={heldMinimized}; "
				+ $"dragged back out to {reopenedPercent:0.##}%");
		}
		else
		{
			Panes.SidePanePercent = 0d;
			await SettleAsync();

			check("a-divider-stays-under-the-pointer-while-it-drags-a-pane-shut", false,
				"no injector, so the gesture could not be driven");
		}

		check("never-mode-gives-no-grip-to-a-drag-minimized-pane",
			Panes.IsSidePaneMinimized
			&& !_sideDivider.IsRestoreGrip
			&& _sideDivider.Visibility == Visibility.Collapsed,
			$"minimized={Panes.IsSidePaneMinimized} grip={_sideDivider.IsRestoreGrip} "
			+ $"visibility={_sideDivider.Visibility} mode={Panes.RestoreGripMode}");

		Panes.RestoreGripMode = TriPaneViewRestoreGripMode.Auto;
		Panes.RestoreAll();
		await SettleAsync();

		// 15. The content guarantee. The text box in the upper pane is found in the visual tree
		//     before, during and after a minimize, it is the same instance every time, and it still
		//     carries what was typed into it.
		var before = FindDescendant<TextBox>(_upperPaneScrollViewer);

		Panes.MinimizeUpperPane();
		await SettleAsync();

		var whileMinimized = FindDescendant<TextBox>(_upperPaneScrollViewer);
		var heightWhileMinimized = _upperPaneScrollViewer.ActualHeight;

		Panes.RestoreUpperPane();
		await SettleAsync();

		var after = FindDescendant<TextBox>(_upperPaneScrollViewer);

		check("a-minimized-pane-keeps-the-same-content-instance-and-its-text",
			before != null
			&& ReferenceEquals(before, SurvivalTextBox)
			&& ReferenceEquals(before, whileMinimized)
			&& ReferenceEquals(before, after)
			&& after.Text == SurvivalText
			&& heightWhileMinimized == 0d
			&& _upperPaneScrollViewer.ActualHeight > 0d,
			$"sameInstance={ReferenceEquals(before, after)} "
			+ $"aliveWhileMinimized={ReferenceEquals(before, whileMinimized)} "
			+ $"text='{after?.Text}' heightWhileMinimized={heightWhileMinimized:0.#} "
			+ $"heightAfter={_upperPaneScrollViewer.ActualHeight:0.#}");
	}

	/// <summary>
	/// The placement swap and the portrait rule, both of which need the real window.
	/// </summary>
	/// <param name="check">Records one PASS or FAIL line.</param>
	private async Task RunPlacementAndPortraitChecksAsync(Action<string, bool, string> check)
	{
		// 16. The placement swap moves the side pane to the other edge, and the divider with it.
		var sideLeftX = AbsoluteBounds(_sidePaneScrollViewer).X;
		var stackLeftX = AbsoluteBounds(_stackGrid).X;

		Panes.SidePanePlacement = TriPaneViewSidePanePlacement.Right;
		await SettleAsync();

		var sideRightX = AbsoluteBounds(_sidePaneScrollViewer).X;
		var stackRightX = AbsoluteBounds(_stackGrid).X;

		check("placement-swap-moves-the-side-pane-to-the-other-edge",
			sideLeftX < stackLeftX
			&& sideRightX > stackRightX
			&& _sidePaneScrollViewer.ActualWidth > 0d,
			$"left placement: side={sideLeftX:0.#} stack={stackLeftX:0.#}; "
			+ $"right placement: side={sideRightX:0.#} stack={stackRightX:0.#}");

		Panes.SidePanePlacement = TriPaneViewSidePanePlacement.Left;
		await SettleAsync();

		// 17. AutoOnPortrait, driven by a REAL window resize through the window manager rather than
		//     by setting a size on the page.
		var claimed = await ClaimWindowAsync();

		if (!claimed)
		{
			check("auto-on-portrait-follows-the-window-shape", false,
				$"no window of process {Environment.ProcessId} answered a resize, so the portrait "
				+ "rule could not be driven");

			return;
		}

		await ResizeWindowAsync(420, 900);

		var portraitMode = _lowerPaneScrollViewer.HorizontalScrollMode;
		var portraitBars = _lowerPaneScrollViewer.HorizontalScrollBarVisibility;
		var portraitExtent = _lowerPaneScrollViewer.ExtentWidth;
		var portraitViewport = _lowerPaneScrollViewer.ViewportWidth;
		var portraitScrolls = portraitExtent > portraitViewport + 1d;
		var portraitShape = $"{Panes.ActualWidth:0}x{Panes.ActualHeight:0}";

		await ResizeWindowAsync(1100, 700);

		var landscapeMode = _lowerPaneScrollViewer.HorizontalScrollMode;
		var landscapeBars = _lowerPaneScrollViewer.HorizontalScrollBarVisibility;
		var landscapeShape = $"{Panes.ActualWidth:0}x{Panes.ActualHeight:0}";

		check("auto-on-portrait-follows-the-window-shape",
			portraitMode == ScrollMode.Enabled
			&& portraitBars == ScrollBarVisibility.Auto
			&& portraitScrolls
			&& landscapeMode == ScrollMode.Disabled
			&& landscapeBars == ScrollBarVisibility.Disabled,
			$"portrait {portraitShape}: mode={portraitMode} bars={portraitBars} "
			+ $"scrolls={portraitScrolls} ({portraitExtent:0.#} over {portraitViewport:0.#}); "
			+ $"landscape {landscapeShape}: mode={landscapeMode} bars={landscapeBars} "
			+ $"({_lowerPaneScrollViewer.ExtentWidth:0.#} over "
			+ $"{_lowerPaneScrollViewer.ViewportWidth:0.#}); window={_ownWindow}");

		// 18. The panes that were NOT set to AutoOnPortrait are untouched by the same resize.
		check("the-other-panes-keep-their-own-horizontal-scroll-mode",
			_sidePaneScrollViewer.HorizontalScrollMode == ScrollMode.Disabled
			&& _upperPaneScrollViewer.HorizontalScrollMode == ScrollMode.Disabled,
			$"side={_sidePaneScrollViewer.HorizontalScrollMode} "
			+ $"upper={_upperPaneScrollViewer.HorizontalScrollMode} "
			+ $"lower={_lowerPaneScrollViewer.HorizontalScrollMode}");

		// 19. DividerThickness is not just a template constant: it reaches both divider tracks.
		Panes.DividerThickness = 14d;
		await SettleAsync();

		var thickSideWidth = _sideDivider.ActualWidth;
		var thickSideColumn = _rootGrid.ColumnDefinitions[1].ActualWidth;
		var thickStackHeight = _stackDivider.ActualHeight;
		var thickStackRow = _stackGrid.RowDefinitions[1].ActualHeight;

		Panes.DividerThickness = 6d;
		await SettleAsync();

		check("divider-thickness-reaches-both-divider-tracks",
			Math.Abs(thickSideWidth - 14d) < 0.5d
			&& Math.Abs(thickSideColumn - 14d) < 0.5d
			&& Math.Abs(thickStackHeight - 14d) < 0.5d
			&& Math.Abs(thickStackRow - 14d) < 0.5d
			&& Math.Abs(_sideDivider.ActualWidth - 6d) < 0.5d
			&& Math.Abs(_stackDivider.ActualHeight - 6d) < 0.5d,
			$"at 14: side={thickSideWidth:0.#} (column {thickSideColumn:0.#}) "
			+ $"stack={thickStackHeight:0.#} (row {thickStackRow:0.#}); "
			+ $"back at 6: side={_sideDivider.ActualWidth:0.#} stack={_stackDivider.ActualHeight:0.#}");
	}

	#endregion

	#region Reaching the template, the window and the pointer

	/// <summary>
	/// Finds the template parts the checks measure. Nothing is looked up by name: the parts are
	/// identified by where the template puts them - the root grid's own children for the side pane,
	/// the side divider and the stack, and the stack grid's rows for the rest - so the checks stay
	/// honest about the SHAPE the template promises rather than about its naming.
	/// </summary>
	/// <returns><see langword="true"/> when every part was found.</returns>
	private bool ResolveParts()
	{
		_rootGrid = FirstVisualChild<Grid>(Panes);

		if (_rootGrid == null)
		{
			return false;
		}

		_sidePaneScrollViewer = _rootGrid.Children.OfType<ScrollViewer>().FirstOrDefault();
		_sideDivider = _rootGrid.Children.OfType<TriPaneViewDivider>().FirstOrDefault();
		_stackGrid = _rootGrid.Children.OfType<Grid>().FirstOrDefault();

		if (_stackGrid == null)
		{
			return false;
		}

		_stackDivider = _stackGrid.Children.OfType<TriPaneViewDivider>().FirstOrDefault();
		_upperPaneScrollViewer = _stackGrid.Children.OfType<ScrollViewer>()
			.FirstOrDefault(pane => Grid.GetRow(pane) == 0);
		_lowerPaneScrollViewer = _stackGrid.Children.OfType<ScrollViewer>()
			.FirstOrDefault(pane => Grid.GetRow(pane) == 2);

		return _sidePaneScrollViewer != null
			&& _sideDivider != null
			&& _stackDivider != null
			&& _upperPaneScrollViewer != null
			&& _lowerPaneScrollViewer != null
			&& _rootGrid.ColumnDefinitions.Count == 3
			&& _stackGrid.RowDefinitions.Count == 3;
	}

	/// <summary>
	/// The width the grid actually gave the column the side pane is in, whichever end that is.
	/// </summary>
	/// <returns>The column width, in pixels.</returns>
	private double SideColumnActualLength()
		=> Panes.SidePanePlacement == TriPaneViewSidePanePlacement.Left
			? _rootGrid.ColumnDefinitions[0].ActualWidth
			: _rootGrid.ColumnDefinitions[2].ActualWidth;

	private static string Describe(FrameworkElement element)
		=> element == null ? "null" : $"{element.GetType().Name}[{element.ActualWidth:0.#}x{element.ActualHeight:0.#}]";

	private static Rect AbsoluteBounds(FrameworkElement element)
		=> element.TransformToVisual(null)
			.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private async Task SettleAsync()
	{
		UpdateLayout();
		await Task.Delay(250);
		UpdateLayout();
		await Task.Delay(150);
	}

	/// <summary>
	/// Presses the pointer on a divider, moves it and releases - a real drag, through the same
	/// pointer pipeline a mouse uses.
	/// </summary>
	/// <param name="divider">The divider to drag.</param>
	/// <param name="deltaX">How far to move horizontally, in pixels.</param>
	/// <param name="deltaY">How far to move vertically, in pixels.</param>
	private async Task DragAsync(FrameworkElement divider, double deltaX, double deltaY)
	{
		await PressAsync(divider);
		await MoveByAsync(deltaX, deltaY);
		await ReleaseAsync();
	}

	/// <summary>
	/// Puts the pointer on the middle of an element and presses, WITHOUT releasing, so a check can
	/// look at the tree while the gesture is still running.
	/// </summary>
	/// <param name="element">The element to press on.</param>
	private async Task PressAsync(FrameworkElement element)
	{
		var bounds = AbsoluteBounds(element);

		_mouse.MoveTo(new Point(bounds.X + (bounds.Width / 2d), bounds.Y + (bounds.Height / 2d)), 4);
		await Task.Delay(150);
		_mouse.Press();
		await Task.Delay(150);
	}

	/// <summary>
	/// Moves the held pointer on from wherever it is now.
	/// </summary>
	/// <param name="deltaX">How far to move horizontally, in pixels.</param>
	/// <param name="deltaY">How far to move vertically, in pixels.</param>
	private async Task MoveByAsync(double deltaX, double deltaY)
	{
		_mouse.MoveBy(deltaX, deltaY, 12);
		await Task.Delay(250);
		UpdateLayout();
		await Task.Delay(100);
	}

	/// <summary>
	/// Releases the pointer and lets the layout settle.
	/// </summary>
	private async Task ReleaseAsync()
	{
		_mouse.Release();
		await SettleAsync();
	}

	/// <summary>
	/// Presses and releases the pointer on an element without moving it at all, which is the tap a
	/// restore grip has to answer.
	/// </summary>
	/// <param name="element">The element to click.</param>
	private async Task ClickAsync(FrameworkElement element)
	{
		var bounds = AbsoluteBounds(element);

		_mouse.MoveTo(new Point(bounds.X + (bounds.Width / 2d), bounds.Y + (bounds.Height / 2d)), 4);
		await Task.Delay(200);
		_mouse.Press();
		await Task.Delay(200);
		_mouse.Release();
		await SettleAsync();
	}

	/// <summary>
	/// Settles which of this process's windows the page is in, by resizing each candidate and
	/// watching for the one whose resize the page's own root actually follows.
	/// </summary>
	/// <returns><see langword="true"/> when a window answered.</returns>
	/// <remarks>
	/// The candidates are the windows the desktop attributes to this PROCESS ID, never the windows
	/// carrying this demo's title: another head running the same demo is titled the same, and a
	/// title is not an identity.
	/// </remarks>
	private async Task<bool> ClaimWindowAsync()
	{
		foreach (var candidate in ListDemoWindows())
		{
			RunShell($"xdotool windowsize --sync {candidate} 980 700");
			await Task.Delay(1000);
			var narrow = XamlRoot?.Size ?? default;

			RunShell($"xdotool windowsize --sync {candidate} 1120 760");
			await Task.Delay(1000);
			var wide = XamlRoot?.Size ?? default;

			if (wide.Width - narrow.Width > 50d)
			{
				_ownWindow = candidate;
				Log($"SELFTEST: window {candidate} answered the resize "
					+ $"({narrow.Width:0}x{narrow.Height:0} -> {wide.Width:0}x{wide.Height:0})");

				return true;
			}
		}

		return false;
	}

	private async Task ResizeWindowAsync(int width, int height)
	{
		RunShell($"xdotool windowsize --sync {_ownWindow} {width} {height}");
		await Task.Delay(1200);
		UpdateLayout();
		await Task.Delay(500);
		UpdateLayout();
		await Task.Delay(300);
	}

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

	private static T FirstVisualChild<T>(DependencyObject root) where T : class
	{
		if (root == null)
		{
			return null;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);

		for (var index = 0; index < count; index++)
		{
			if (VisualTreeHelper.GetChild(root, index) is T match)
			{
				return match;
			}
		}

		return null;
	}

	private static T FindDescendant<T>(DependencyObject root) where T : class
	{
		if (root == null)
		{
			return null;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);

		for (var index = 0; index < count; index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);

			if (child is T match)
			{
				return match;
			}

			if (FindDescendant<T>(child) is T deeper)
			{
				return deeper;
			}
		}

		return null;
	}

	/// <summary>
	/// A minimal pointer of the kind the platform's own injector drives: it remembers where it left
	/// the pointer, because the injected mouse is moved by deltas rather than placed at a position.
	/// </summary>
	private sealed class DemoMouse
	{
		private readonly InputInjector _injector;
		private Point _position;

		public DemoMouse(InputInjector injector) => _injector = injector;

		/// <summary>
		/// Moves the pointer to a point in the window's own coordinates, in whole-pixel steps so the
		/// element under it sees a stream of moves rather than one jump.
		/// </summary>
		/// <param name="target">Where to move to.</param>
		/// <param name="steps">How many moves to break the travel into.</param>
		public void MoveTo(Point target, int steps)
		{
			var count = Math.Max(steps, 1);
			var startX = _position.X;
			var startY = _position.Y;
			var previousX = (int)Math.Round(startX);
			var previousY = (int)Math.Round(startY);
			var moves = new List<InjectedInputMouseInfo>();

			for (var step = 1; step <= count; step++)
			{
				var x = (int)Math.Round(startX + ((target.X - startX) * step / count));
				var y = (int)Math.Round(startY + ((target.Y - startY) * step / count));

				moves.Add(new InjectedInputMouseInfo
				{
					DeltaX = x - previousX,
					DeltaY = y - previousY,
					TimeOffsetInMilliseconds = 1,
					MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce
				});

				previousX = x;
				previousY = y;
			}

			_injector.InjectMouseInput(moves);
			_position = new Point(previousX, previousY);
		}

		/// <summary>
		/// Moves the pointer on from where it is now, in the same whole-pixel steps.
		/// </summary>
		/// <param name="deltaX">How far to move horizontally, in pixels.</param>
		/// <param name="deltaY">How far to move vertically, in pixels.</param>
		/// <param name="steps">How many moves to break the travel into.</param>
		public void MoveBy(double deltaX, double deltaY, int steps)
			=> MoveTo(new Point(_position.X + deltaX, _position.Y + deltaY), steps);

		public void Press()
			=> _injector.InjectMouseInput(
			[
				new InjectedInputMouseInfo
				{
					TimeOffsetInMilliseconds = 1,
					MouseOptions = InjectedInputMouseOptions.LeftDown
				}
			]);

		public void Release()
			=> _injector.InjectMouseInput(
			[
				new InjectedInputMouseInfo
				{
					TimeOffsetInMilliseconds = 1,
					MouseOptions = InjectedInputMouseOptions.LeftUp
				}
			]);
	}

	#endregion
}
