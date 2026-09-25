#nullable enable

using System;
using System.Linq;
using CodeBrix.Platform.UI.PlotterView.Engine;
using CodeBrix.Plotter;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The PlotterView engine (WPE1 C7): PlotHost attaches a CodeBrix.Plotter model to a view, renders it (typefaces from
/// the platform's font source) onto an SKCanvas, and turns mouse, wheel, key and touch input into controller calls -
/// the tracker (mouse and touch), zoom and reset - with only the engine Core, CodeBrix.Plotter and SkiaSharp in the process: no WinUI
/// assembly loads, and no TextLayout either (the chart's typefaces come straight from the font source).
/// </summary>
public class PlotterViewEngineTests
{
	[Fact]
	public void When_A_Model_Is_Attached_Rendered_And_Driven_By_Input_Then_It_Paints_Tracks_Zooms_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var fontSource = TestFontSourcePlatform.EnsureRegistered();
		var requestsBefore = fontSource.TypefaceRequests;
		const int width = 400;
		const int height = 300;
		var model = new PlotModel { Title = "Engine proof", Background = PlotterColors.White };
		var xAxis = new CodeBrix.Plotter.Axes.LinearAxis { Position = CodeBrix.Plotter.Axes.AxisPosition.Bottom, Minimum = 0, Maximum = 10 };
		var yAxis = new CodeBrix.Plotter.Axes.LinearAxis { Position = CodeBrix.Plotter.Axes.AxisPosition.Left, Minimum = 0, Maximum = 100 };
		model.Axes.Add(xAxis);
		model.Axes.Add(yAxis);
		var series = new CodeBrix.Plotter.Series.LineSeries { Color = PlotterColors.Red, StrokeThickness = 3 };
		for (var i = 0; i <= 10; i++)
		{
			series.Points.Add(new DataPoint(i, i * i));
		}

		model.Series.Add(series);
		var view = new TestPlotView(width, height);
		var host = view.Host;

		//Act: attach and paint
		host.SetModel(null, model);
		view.InvalidatePlot(true);
		using var bitmap = new SKBitmap(width, height);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.Transparent);
		host.Paint(canvas, new SKSize(width, height));
		canvas.Flush();
		var firstPixels = bitmap.Pixels;
		var fullRange = (xAxis.ActualMinimum, xAxis.ActualMaximum);

		//Act: the tracker (left press on a data point), a wheel zoom, the reset key, a touch press and drag
		var onPoint = series.Transform(new DataPoint(5, 25));
		host.MouseDown(PlotterMouseButton.Left, onPoint, 1_000);
		var trackerShown = host.Tracker;
		host.Paint(canvas, new SKSize(width, height));
		host.MouseUp(onPoint);
		var center = new ScreenPoint(width / 2.0, height / 2.0);
		host.MouseWheel(center, 120);
		host.Paint(canvas, new SKSize(width, height));
		var zoomedRange = (xAxis.ActualMinimum, xAxis.ActualMaximum);
		host.KeyDown(PlotterKey.A);
		host.Paint(canvas, new SKSize(width, height));
		var resetRange = (xAxis.ActualMinimum, xAxis.ActualMaximum);
		var showsBeforeTouch = view.TrackerShows;
		host.TouchTracker.Down(1, onPoint).Should().BeTrue(); // the first contact starts the gesture
		host.TouchStarted(onPoint);
		var touchTracker = host.Tracker;
		var moved = host.TouchMoved(1, new ScreenPoint(onPoint.X - 40, onPoint.Y));
		var trackerAfterMove = host.Tracker;
		var unknownContact = host.TouchMoved(7, new ScreenPoint(0, 0));
		var lastContact = host.TouchTracker.Up(1);
		host.TouchCompleted(new ScreenPoint(onPoint.X - 40, onPoint.Y));

		host.SetModel(model, null);

		//Assert
		view.AttachedModel(model).Should().BeFalse(); // detached again
		firstPixels.Count(p => p.Alpha == 255 && p.Red > 200 && p.Green < 80 && p.Blue < 80).Should().BeGreaterThan(50); // the red series
		firstPixels.Count(p => p.Alpha == 255 && p.Red < 80 && p.Green < 80 && p.Blue < 80).Should().BeGreaterThan(50); // title, axes and labels
		fontSource.TypefaceRequests.Should().BeGreaterThan(requestsBefore); // the chart's text asked the platform's font source
		fullRange.Should().Be((0d, 10d));
		trackerShown.Should().NotBeNull();
		view.TrackerShows.Should().BeGreaterThan(0);
		(zoomedRange.ActualMaximum - zoomedRange.ActualMinimum).Should().BeLessThan(10d); // the wheel zoomed in
		resetRange.Should().Be(fullRange);                                                // A reset the axes
		// Touch, with CodeBrix.Plotter's default controller: a press on a series shows the tracker, a drag hides it
		view.TrackerShows.Should().BeGreaterThan(showsBeforeTouch);
		touchTracker.Should().NotBeNull();
		moved.Should().BeTrue();
		trackerAfterMove.Should().BeNull();
		unknownContact.Should().BeFalse();
		lastContact.Should().BeTrue();
		view.Invalidations.Should().BeGreaterThan(0);

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.PlotterView.Core");
		typeof(PlotHost).Assembly.GetReferencedAssemblies().Select(a => a.Name).Should()
			.NotContain("CodeBrix.Platform.UI.TextLayout.Core"); // WPE1 C7: the typefaces need no TextLayout
		EngineIsolation.AssertNoWinUILoaded("PlotterView");
	}

	[Fact]
	public void When_Modifiers_And_Tracker_State_Change_Then_The_Host_Reports_Only_Real_Changes_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var view = new TestPlotView(100, 100);
		var host = view.Host;

		//Act + Assert
		host.UpdateModifier(PlotterModifierKeys.None, isDown: true).Should().BeFalse();
		host.UpdateModifier(PlotterModifierKeys.Shift, isDown: true).Should().BeTrue();
		host.UpdateModifier(PlotterModifierKeys.Control, isDown: true).Should().BeTrue();
		host.Modifiers.Should().Be(PlotterModifierKeys.Shift | PlotterModifierKeys.Control);
		host.UpdateModifier(PlotterModifierKeys.Shift, isDown: false);
		host.Modifiers.Should().Be(PlotterModifierKeys.Control);
		host.HideTracker().Should().BeFalse();
		host.ShowTracker(new TrackerHitResult { Text = "t" });
		host.HideTracker().Should().BeTrue();
		host.HideZoomRectangle().Should().BeFalse();
		host.ShowZoomRectangle(new PlotterRect(1, 1, 5, 5));
		host.HideZoomRectangle().Should().BeTrue();
		host.SetCursorType(CursorType.Default).Should().BeFalse();
		host.SetCursorType(CursorType.Pan).Should().BeTrue();
		host.TrackerFontSize = 1;
		host.TrackerFontSize.Should().Be(4);
		host.ActualController.Should().BeOfType<PlotController>();

		EngineIsolation.AssertNoWinUILoaded("PlotterView");
	}

	/// <summary>A view that forwards to its engine, as PlotterControl does, with no XAML.</summary>
	private sealed class TestPlotView : IPlotView
	{
		private readonly double _width;
		private readonly double _height;

		internal TestPlotView(double width, double height)
		{
			_width = width;
			_height = height;
			Host = new PlotHost(this, () => Invalidations++, action =>
			{
				action();
				return true;
			});
		}

		internal PlotHost Host { get; }

		internal int Invalidations { get; private set; }

		internal int TrackerShows { get; private set; }

		internal bool AttachedModel(PlotModel model) => ReferenceEquals(model.PlotView, this);

		public PlotModel? ActualModel => Host.Model;

		CodeBrix.Plotter.Model IView.ActualModel => Host.Model!;

		public IController ActualController => Host.ActualController;

		public PlotterRect ClientArea => new(0, 0, _width, _height);

		public void InvalidatePlot(bool updateData = true)
		{
			Host.MarkForUpdate(updateData);
			Invalidations++;
		}

		public void ShowTracker(TrackerHitResult trackerHitResult)
		{
			TrackerShows++;
			Host.ShowTracker(trackerHitResult);
		}

		public void HideTracker() => Host.HideTracker();

		public void ShowZoomRectangle(PlotterRect rectangle) => Host.ShowZoomRectangle(rectangle);

		public void HideZoomRectangle() => Host.HideZoomRectangle();

		public void SetCursorType(CursorType cursorType) => Host.SetCursorType(cursorType);

		public void SetClipboardText(string text)
		{
		}
	}
}
