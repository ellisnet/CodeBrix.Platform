using System.Collections.Generic;
using System.Reflection;
using CodeBrix.Platform.UI.Hosting;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The opt-in direct present path: instead of painting into a staging array and copying that into
/// the on-screen bitmap, the control paints straight into the bitmap's own pixel buffer, one
/// full-frame copy fewer per paint.
/// </summary>
/// <remarks>
/// The latch is process-wide, so this class has its own collection and every test restores the
/// latch in a finally (<see cref="DirectSkiaCanvasModeSwitch.Enabled"/>).
/// </remarks>
[Collection(DirectSkiaCanvasModeCollection.Name)]
public class DirectSkiaCanvasModeTests
{
	[Fact]
	public void the_mode_is_off_unless_an_application_turns_it_on()
	{
		//The suite would be measuring the wrong path everywhere else if this were not true.
		DirectSkiaCanvasMode.IsEnabled.Should().BeFalse();
	}

	[Fact]
	public void the_shipped_latch_is_still_one_way_and_internal()
	{
		//Arrange
		//The suite reaches around the latch by reflection, so it owes a check that the SHIPPED
		//shape has not quietly changed: no public setter, and Enable() not public.
		var type = typeof(DirectSkiaCanvasMode);

		//Act
		var isEnabled = type.GetProperty(nameof(DirectSkiaCanvasMode.IsEnabled));
		var enable = type.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);

		//Assert
		isEnabled.Should().NotBeNull();
		isEnabled!.SetMethod.Should().BeNull();
		enable.Should().NotBeNull();
		enable!.IsPublic.Should().BeFalse();
		type.GetMethod("Disable").Should().BeNull();
	}

	[Fact]
	public void the_direct_path_paints_into_the_bitmap_without_a_staging_array()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(40, 40);
			canvas.PaintSurface += (s, e) =>
			{
				e.FillWith(SKColors.White);
				e.FillRect(new SKRect(10, 10, 20, 20), SKColors.Blue);
			};

			//Act
			canvas.Invalidate();

			//Assert
			//The same picture as the default path produces...
			canvas.PresentedBitmap()!.PixelWidth.Should().Be(40);
			canvas.PresentedPixelAt(15, 15).Should().Be(SKColors.Blue);
			canvas.PresentedPixelAt(5, 5).Should().Be(SKColors.White);
			canvas.PresentedPixelAt(25, 25).Should().Be(SKColors.White);
			//...and the point of the mode: the staging array was never allocated, so there was
			//nothing to copy from.
			canvas.StagingPixels().Should().BeNull();
		});
	}

	[Fact]
	public void the_direct_path_presents_BGRA_premultiplied_bytes_too()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(4, 4);
			canvas.PaintSurface += (s, e) => e.FillWith(new SKColor(0xFF, 0x00, 0x00, 0x80));

			//Act
			canvas.Invalidate();
			var pixels = canvas.PresentedPixels();

			//Assert
			//Byte for byte the same contract as the default path - the mode changes where the
			//drawing happens, not what it produces.
			pixels[0].Should().Be(0x00);
			pixels[1].Should().Be(0x00);
			pixels[2].Should().BeInRange((byte)0x7E, (byte)0x81);
			pixels[3].Should().Be(0x80);
		});
	}

	[Fact]
	public void the_direct_path_reuses_one_event_args_instance_between_paints()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(64, 64);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) => raised.Add(e);

			//Act
			canvas.Invalidate();
			canvas.Invalidate();

			//Assert
			//The direct path has its own cache, revalidated against the bitmap's raw pointer every
			//frame; while the pointer and the format are unchanged the args must be reused.
			raised.Count.Should().Be(2);
			ReferenceEquals(raised[0], raised[1]).Should().BeTrue();
			ReferenceEquals(raised[0].Surface, raised[1].Surface).Should().BeTrue();
		});
	}

	[Fact]
	public void the_direct_path_revalidates_its_cached_surface_after_a_resize()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(40, 40);
			var raised = new List<SKPaintSurfaceEventArgs>();
			canvas.PaintSurface += (s, e) =>
			{
				raised.Add(e);
				e.FillWith(SKColors.White);
				e.FillRect(new SKRect(0, 0, 10, 10), SKColors.Green);
			};
			canvas.Invalidate();
			var first = canvas.PresentedBitmap();

			//Act
			canvas.LayOutAt(80, 20);
			canvas.Invalidate();

			//Assert
			//A resize allocates a new bitmap, so the cached surface points at freed memory until it
			//is revalidated; if it were not, this would paint into the old buffer (or crash).
			var second = canvas.PresentedBitmap();
			ReferenceEquals(first, second).Should().BeFalse();
			second!.PixelWidth.Should().Be(80);
			second.PixelHeight.Should().Be(20);
			ReferenceEquals(raised[0], raised[1]).Should().BeFalse();
			canvas.PresentedPixelAt(5, 5).Should().Be(SKColors.Green);
			canvas.PresentedPixelAt(79, 19).Should().Be(SKColors.White);
			canvas.StagingPixels().Should().BeNull();
		});
	}

	[Fact]
	public void the_direct_path_resets_the_cached_surface_matrix_between_paints()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() =>
		{
			var canvas = new SKXamlCanvas();
			canvas.LayOutAt(40, 40);
			var paintCount = 0;
			canvas.PaintSurface += (s, e) =>
			{
				paintCount++;
				if (paintCount == 1)
				{
					e.Surface.Canvas.Scale(2f);
					e.Surface.Canvas.Save();
				}

				e.FillWith(SKColors.White);
				e.FillRect(new SKRect(0, 0, 10, 10), SKColors.Red);
			};

			//Act
			canvas.Invalidate();
			var scaledEdgeAfterFirstPaint = canvas.PresentedPixelAt(15, 15);
			canvas.Invalidate();

			//Assert
			scaledEdgeAfterFirstPaint.Should().Be(SKColors.Red);
			canvas.PresentedPixelAt(15, 15).Should().Be(SKColors.White);
			canvas.PresentedPixelAt(5, 5).Should().Be(SKColors.Red);
		});
	}

	[Fact]
	public void the_default_path_is_restored_after_a_direct_mode_test()
	{
		//Arrange
		DirectSkiaCanvasModeSwitch.Enabled(() => { });

		//Act
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(8, 8);
		canvas.PaintSurface += (s, e) => e.FillWith(SKColors.Yellow);
		canvas.Invalidate();

		//Assert
		//The guarantee the rest of the suite rests on: the latch really did go back off, and the
		//staging array is allocated again.
		DirectSkiaCanvasMode.IsEnabled.Should().BeFalse();
		canvas.StagingPixels().Should().NotBeNull();
		canvas.PresentedPixelAt(4, 4).Should().Be(SKColors.Yellow);
	}
}
