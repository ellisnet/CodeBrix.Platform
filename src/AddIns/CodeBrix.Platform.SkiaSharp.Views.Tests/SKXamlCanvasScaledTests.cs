using System.Collections.Generic;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The control at a display scale other than 100%, which is where <c>Info</c>, <c>RawInfo</c> and
/// <c>IgnorePixelScaling</c> stop being the same thing.
/// </summary>
/// <remarks>
/// <para>
/// The scale comes from the suite's fake display extension (see <see cref="DisplayScale"/>). It is
/// process-wide, so this class is in its own collection and every test restores 96 dpi in a finally
/// - <see cref="DisplayScale.At"/> does that. A canvas reads the dpi once, in its constructor, so
/// each test constructs its canvas INSIDE the scaled block.
/// </para>
/// </remarks>
[Collection(DisplayScaleCollection.Name)]
public class SKXamlCanvasScaledTests
{
	[Fact]
	public void Dpi_reports_the_display_scale_the_canvas_was_created_under()
	{
		//Arrange
		//Act
		//Assert
		DisplayScale.At(2.0, () =>
		{
			var canvas = new SKXamlCanvas();
			canvas.Dpi.Should().Be(2.0);
		});
	}

	[Fact]
	public void the_default_scale_is_restored_afterwards()
	{
		//Arrange
		DisplayScale.At(2.0, () => new SKXamlCanvas().Dpi.Should().Be(2.0));

		//Act
		var canvas = new SKXamlCanvas();

		//Assert
		//The guarantee the whole collection rests on: a scaled test cannot leak its scale into the
		//rest of the suite.
		canvas.Dpi.Should().Be(1.0);
		DisplayScale.LogicalDpi.Should().Be(DisplayScale.BaseDpi);
	}

	[Fact]
	public void at_a_200_percent_display_the_surface_is_twice_the_arranged_size()
	{
		//Arrange
		DisplayScale.At(2.0, () =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(200, 100);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) => raised.Add(e);

			//Act
			canvas.Invalidate();

			//Assert
			//With IgnorePixelScaling off the handler is told the device truth: it is drawing on a
			//400x200 surface, and the bitmap that reaches the screen is 400x200 too.
			raised.Count.Should().Be(1);
			raised[0].Info.Width.Should().Be(400);
			raised[0].Info.Height.Should().Be(200);
			raised[0].RawInfo.Width.Should().Be(400);
			raised[0].RawInfo.Height.Should().Be(200);
			canvas.CanvasSize.Should().Be(new SKSize(400, 200));
			canvas.PresentedBitmap()!.PixelWidth.Should().Be(400);
			canvas.PresentedBitmap()!.PixelHeight.Should().Be(200);
		});
	}

	[Fact]
	public void IgnorePixelScaling_reports_the_unscaled_size_and_prescales_the_canvas()
	{
		//Arrange
		DisplayScale.At(2.0, () =>
		{
			var canvas = new SKXamlCanvas { IgnorePixelScaling = true };
			canvas.LayOutAt(200, 100);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) =>
			{
				raised.Add(e);
				e.FillWith(SKColors.White);
				//A one-unit mark, in the handler's own coordinates.
				e.FillRect(new SKRect(10, 10, 11, 11), SKColors.Red);
			};

			//Act
			canvas.Invalidate();

			//Assert
			//The handler draws in unscaled coordinates and the control pre-scales the canvas, so
			//Info is the 200x100 the application asked for while RawInfo is the 400x200 that really
			//exists...
			raised.Count.Should().Be(1);
			raised[0].Info.Width.Should().Be(200);
			raised[0].Info.Height.Should().Be(100);
			raised[0].RawInfo.Width.Should().Be(400);
			raised[0].RawInfo.Height.Should().Be(200);
			canvas.CanvasSize.Should().Be(new SKSize(200, 100));
			canvas.PresentedBitmap()!.PixelWidth.Should().Be(400);

			//...and the proof that the pre-scale really happened: the one-unit mark at (10,10)
			//covers device pixels (20,20) and (21,21), and nothing at (10,10).
			canvas.PresentedPixelAt(20, 20).Should().Be(SKColors.Red);
			canvas.PresentedPixelAt(21, 21).Should().Be(SKColors.Red);
			canvas.PresentedPixelAt(10, 10).Should().Be(SKColors.White);
			canvas.PresentedPixelAt(22, 22).Should().Be(SKColors.White);
		});
	}

	[Fact]
	public void toggling_IgnorePixelScaling_at_a_scaled_display_repaints_at_the_other_size()
	{
		//Arrange
		DisplayScale.At(2.0, () =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(200, 100);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) => raised.Add(e);
			canvas.Invalidate();

			//Act
			canvas.IgnorePixelScaling = true;

			//Assert
			//The setter repaints by itself, and the size the handler is told changes with it - the
			//raw surface behind it does not.
			raised.Count.Should().Be(2);
			raised[0].Info.Width.Should().Be(400);
			raised[1].Info.Width.Should().Be(200);
			raised[1].RawInfo.Width.Should().Be(400);
			//A different Info size means a new args instance, even at the same raw size.
			ReferenceEquals(raised[0], raised[1]).Should().BeFalse();
		});
	}

	[Fact]
	public void a_fractional_display_scale_truncates_the_surface_to_whole_pixels()
	{
		//Arrange
		//150% is the commonest fractional scale on a real desktop; the control multiplies and
		//truncates, so 200x100 logical becomes 300x150 raw and 101 logical rows become 151, not 152.
		DisplayScale.At(1.5, () =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(200, 101);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) => raised.Add(e);

			//Act
			canvas.Invalidate();

			//Assert
			canvas.Dpi.Should().Be(1.5);
			raised.Count.Should().Be(1);
			raised[0].RawInfo.Width.Should().Be(300);
			raised[0].RawInfo.Height.Should().Be(151);
		});
	}
}
