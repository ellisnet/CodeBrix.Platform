using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The control that actually ships, exercised host-free: constructed, measured, arranged, painted,
/// and read back through the bitmap it presents.
/// </summary>
/// <remarks>
/// <para>
/// The dispatcher overrides installed by DispatcherInitializer are what make this possible and what
/// make it synchronous: <c>Invalidate()</c> paints before it returns, so every test below can
/// assert on the frame immediately.
/// </para>
/// <para>
/// These tests run at the default display scale (Dpi 1.0); the scaled half of the control is in
/// <see cref="SKXamlCanvasScaledTests"/>, and the opt-in direct present path is in
/// <see cref="SKXamlCanvasDirectModeTests"/>.
/// </para>
/// </remarks>
public class SKXamlCanvasTests
{
	private static readonly SKColor Opaque = new(0x10, 0x20, 0x30, 0xFF);

	[Fact]
	public void ctor_without_a_head_yields_dpi_one_and_an_empty_canvas_size()
	{
		//Arrange
		//Act
		var canvas = new SKXamlCanvas();

		//Assert
		//No head means no IDisplayInformationExtension answering anything but the suite's fake,
		//which defaults to 96 dpi - the framework's own base, i.e. a 100% display.
		canvas.Dpi.Should().Be(1.0);
		canvas.CanvasSize.Should().Be(SKSize.Empty);
		canvas.IgnorePixelScaling.Should().BeFalse();
		canvas.Background.Should().BeNull();
	}

	[Fact]
	public void Invalidate_raises_PaintSurface_with_the_arranged_size()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		var raised = new List<SKPaintSurfaceEventArgs>();
		canvas.PaintSurface += (s, e) => raised.Add(e);

		//Act
		canvas.Invalidate();

		//Assert
		raised.Count.Should().Be(1);
		raised[0].Info.Width.Should().Be(200);
		raised[0].Info.Height.Should().Be(100);
		raised[0].RawInfo.Width.Should().Be(200);
		raised[0].RawInfo.Height.Should().Be(100);
		raised[0].Info.ColorType.Should().Be(SKColorType.Bgra8888);
		raised[0].Info.AlphaType.Should().Be(SKAlphaType.Premul);
		raised[0].Surface.Should().NotBeNull();
		canvas.CanvasSize.Should().Be(new SKSize(200, 100));
	}

	[Fact]
	public void the_sender_of_PaintSurface_is_the_canvas()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(50, 50);
		object? sender = null;
		canvas.PaintSurface += (s, e) => sender = s;

		//Act
		canvas.Invalidate();

		//Assert
		sender.Should().BeSameAs(canvas);
	}

	[Fact]
	public void OnPaintSurface_is_the_raise_path_a_subclass_can_intercept()
	{
		//Arrange
		//The protected virtual is public API of the control: a subclass is the supported way to
		//paint without wiring an event, and it must see the same args the event would carry.
		var canvas = new PaintingCanvas();
		canvas.LayOutAt(20, 20);
		var eventRaised = 0;
		canvas.PaintSurface += (s, e) => eventRaised++;

		//Act
		canvas.Invalidate();

		//Assert
		canvas.Overridden.Count.Should().Be(1);
		canvas.Overridden[0].Info.Width.Should().Be(20);
		//The base implementation still raises the event, because the override calls it.
		eventRaised.Should().Be(1);
	}

	[Fact]
	public void Invalidate_presents_the_painted_pixels_into_the_writeablebitmap()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		canvas.PaintSurface += (s, e) => e.FillWith(Opaque);

		//Act
		canvas.Invalidate();

		//Assert
		//This is the whole present path end to end: SKSurface over the staging buffer, staging
		//buffer copied through the framework's raw-buffer accessor into the WriteableBitmap.
		var brush = canvas.PresentedBrush();
		brush.Should().NotBeNull();
		brush!.Stretch.Should().Be(Stretch.Fill);
		brush.AlignmentX.Should().Be(AlignmentX.Left);
		brush.AlignmentY.Should().Be(AlignmentY.Top);

		var bitmap = canvas.PresentedBitmap();
		bitmap.Should().NotBeNull();
		bitmap!.PixelWidth.Should().Be(200);
		bitmap.PixelHeight.Should().Be(100);

		canvas.PresentedPixelAt(0, 0).Should().Be(Opaque);
		canvas.PresentedPixelAt(199, 99).Should().Be(Opaque);
		canvas.PresentedPixelAt(100, 50).Should().Be(Opaque);
	}

	[Fact]
	public void the_presented_bytes_are_BGRA_premultiplied()
	{
		//Arrange
		//The byte ORDER and the premultiplication are what an application reading PixelBuffer
		//depends on, and they are the first thing a SkiaSharp change would move silently. Half
		//alpha over pure red premultiplies to roughly half red, with the blue and green channels
		//still zero and alpha carried through unchanged.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(4, 4);
		canvas.PaintSurface += (s, e) => e.FillWith(new SKColor(0xFF, 0x00, 0x00, 0x80));

		//Act
		canvas.Invalidate();
		var pixels = canvas.PresentedPixels();

		//Assert
		pixels.Length.Should().Be(4 * 4 * 4);
		pixels[0].Should().Be(0x00);                            //blue
		pixels[1].Should().Be(0x00);                            //green
		pixels[2].Should().BeInRange((byte)0x7E, (byte)0x81);   //red, premultiplied by 0x80/0xFF
		pixels[3].Should().Be(0x80);                            //alpha, carried through
	}

	[Fact]
	public void a_rectangle_drawn_at_a_position_lands_at_that_position_in_the_bitmap()
	{
		//Arrange
		//Proves the surface really maps onto the presented bitmap rather than merely being the
		//right size: colour is checked INSIDE the rectangle and OUTSIDE it.
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
		canvas.PresentedPixelAt(15, 15).Should().Be(SKColors.Blue);
		canvas.PresentedPixelAt(5, 5).Should().Be(SKColors.White);
		canvas.PresentedPixelAt(25, 25).Should().Be(SKColors.White);
		//The rectangle's edges are half-open, so 10 is inside and 20 is not.
		canvas.PresentedPixelAt(10, 10).Should().Be(SKColors.Blue);
		canvas.PresentedPixelAt(20, 20).Should().Be(SKColors.White);
	}

	[Fact]
	public void two_paints_at_the_same_size_reuse_one_event_args_instance()
	{
		//Arrange
		//The fork caches the surface and the args while the staging buffer is unchanged, so that
		//painting at game frame rates does not allocate per frame. This fences that cache: if a
		//future change went back to allocating per paint, the performance work would be undone
		//silently and this test is what notices.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(64, 64);
		var raised = new List<SKPaintSurfaceEventArgs>();
		canvas.PaintSurface += (s, e) => raised.Add(e);

		//Act
		canvas.Invalidate();
		canvas.Invalidate();

		//Assert
		raised.Count.Should().Be(2);
		ReferenceEquals(raised[0], raised[1]).Should().BeTrue();
		ReferenceEquals(raised[0].Surface, raised[1].Surface).Should().BeTrue();
	}

	[Fact]
	public void the_cached_surface_matrix_is_reset_between_paints()
	{
		//Arrange
		//The consequence of caching one surface across frames: a handler that scales or saves must
		//not leak that state into the next frame. The control resets the matrix and the save stack
		//before every raise; this proves it by scaling in the FIRST paint only.
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
		//Scaled by two, the 10x10 rectangle covers pixel (15,15); unscaled it does not.
		scaledEdgeAfterFirstPaint.Should().Be(SKColors.Red);
		canvas.PresentedPixelAt(15, 15).Should().Be(SKColors.White);
		canvas.PresentedPixelAt(5, 5).Should().Be(SKColors.Red);
	}

	[Fact]
	public void a_size_change_reallocates_the_presented_bitmap()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		canvas.PaintSurface += (s, e) => e.FillWith(SKColors.Green);
		canvas.Invalidate();
		var first = canvas.PresentedBitmap();

		//Act
		canvas.LayOutAt(300, 50);
		canvas.Invalidate();

		//Assert
		var second = canvas.PresentedBitmap();
		second.Should().NotBeNull();
		second!.PixelWidth.Should().Be(300);
		second.PixelHeight.Should().Be(50);
		ReferenceEquals(first, second).Should().BeFalse();
		canvas.CanvasSize.Should().Be(new SKSize(300, 50));
		canvas.PresentedPixelAt(299, 49).Should().Be(SKColors.Green);
	}

	[Fact]
	public void a_size_change_invalidates_the_cached_event_args()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		var raised = new List<SKPaintSurfaceEventArgs>();
		canvas.PaintSurface += (s, e) => raised.Add(e);
		canvas.Invalidate();

		//Act
		canvas.LayOutAt(300, 50);
		canvas.Invalidate();

		//Assert
		//The args carry the size, so a resize must hand out a new instance - the opposite of
		//two_paints_at_the_same_size_reuse_one_event_args_instance, and the reason that cache is
		//safe.
		raised.Count.Should().Be(2);
		ReferenceEquals(raised[0], raised[1]).Should().BeFalse();
		raised[1].Info.Width.Should().Be(300);
		raised[1].Info.Height.Should().Be(50);
	}

	[Fact]
	public void Invalidate_paints_nothing_while_collapsed()
	{
		//Arrange
		//Visibility reaches the control through a binding it sets on ITSELF in Initialize
		//(ProxyVisibility -> OnVisibilityChanged -> isVisible), so this is the one test that
		//exercises SetBinding, and it proves the binding resolves outside a visual tree.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(20, 20);
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;
		canvas.Invalidate();
		paintCount.Should().Be(1);

		//Act
		canvas.Visibility = Visibility.Collapsed;
		canvas.Invalidate();

		//Assert
		paintCount.Should().Be(1);
		//This is what says the BINDING stopped the paint rather than the zero size a collapsed
		//element gets: the isVisible gate returns before CanvasSize is touched, so CanvasSize keeps
		//the value from the last real paint. Had the binding not fired, the control would have gone
		//down the "no size" road and emptied it.
		canvas.CanvasSize.Should().Be(new SKSize(20, 20));
		canvas.ActualWidth.Should().Be(0);
	}

	[Fact]
	public void a_collapsed_canvas_paints_again_once_it_is_visible_and_laid_out_afresh()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(20, 20);
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;
		canvas.Invalidate();
		canvas.Visibility = Visibility.Collapsed;
		canvas.Invalidate();
		paintCount.Should().Be(1);

		//Act
		canvas.Visibility = Visibility.Visible;
		var afterVisible = paintCount;
		canvas.LayOutAt(20, 20);
		canvas.Invalidate();

		//Assert
		//Turning Visibility back on re-raises through the same binding, but a collapsed element's
		//arranged size was zeroed, so that first pass finds nothing to paint and empties CanvasSize;
		//the parent's next layout pass is what brings the picture back. In an application that
		//layout pass happens by itself - here it is explicit.
		afterVisible.Should().Be(1);
		paintCount.Should().Be(2);
		canvas.CanvasSize.Should().Be(new SKSize(20, 20));
	}

	[Fact]
	public void Invalidate_with_no_layout_leaves_the_canvas_size_empty_and_raises_nothing()
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;

		//Act
		canvas.Invalidate();

		//Assert
		paintCount.Should().Be(0);
		canvas.CanvasSize.Should().Be(SKSize.Empty);
		canvas.Background.Should().BeNull();
	}

	[Theory]
	[InlineData(0d, 0d)]
	[InlineData(0d, 100d)]
	[InlineData(200d, 0d)]
	public void a_zero_dimension_leaves_the_canvas_size_empty_and_raises_nothing(double width, double height)
	{
		//Arrange
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		canvas.PaintSurface += (s, e) => { };
		canvas.Invalidate();
		canvas.CanvasSize.Should().Be(new SKSize(200, 100));
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;

		//Act
		canvas.LayOutAt(width, height);
		canvas.Invalidate();

		//Assert
		paintCount.Should().Be(0);
		canvas.CanvasSize.Should().Be(SKSize.Empty);
	}

	[Fact]
	public void a_NaN_arranged_size_leaves_the_canvas_size_empty_and_raises_nothing()
	{
		//Arrange
		//CreateSize guards against NaN and infinity as well as zero; a NaN cannot arrive from a
		//normal Arrange, so it is set on the size fields the same way a broken parent would.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;
		canvas.Width = double.NaN;
		canvas.Height = double.NaN;

		//Act
		canvas.Measure(new Size(double.NaN, double.NaN));
		canvas.Arrange(new Rect(0, 0, 0, 0));
		canvas.Invalidate();

		//Assert
		paintCount.Should().Be(0);
		canvas.CanvasSize.Should().Be(SKSize.Empty);
	}

	[Fact]
	public void IgnorePixelScaling_agrees_with_the_raw_info_at_a_100_percent_display()
	{
		//Arrange
		//At Dpi 1 the scaled and unscaled sizes are the same number, so this says only that the
		//two paths agree - the interesting case is at Dpi 2, in SKXamlCanvasScaledTests.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(200, 100);
		var raised = new List<SKPaintSurfaceEventArgs>();
		canvas.PaintSurface += (s, e) => raised.Add(e);
		canvas.Invalidate();

		//Act
		canvas.IgnorePixelScaling = true;

		//Assert
		//Setting the property re-raises by itself: the setter calls Invalidate.
		canvas.IgnorePixelScaling.Should().BeTrue();
		raised.Count.Should().Be(2);
		raised[1].Info.Should().Be(raised[1].RawInfo);
		raised[1].Info.Width.Should().Be(200);
		raised[1].Info.Height.Should().Be(100);
	}

	[Fact]
	public void setting_IgnorePixelScaling_repaints_even_when_the_value_is_unchanged()
	{
		//Arrange
		//Characterisation: the setter has no equality guard, so assigning the same value repaints.
		//An application driving this property per frame would pay for a frame each time.
		var canvas = new SKXamlCanvas();
		canvas.LayOutAt(20, 20);
		var paintCount = 0;
		canvas.PaintSurface += (s, e) => paintCount++;
		canvas.Invalidate();

		//Act
		canvas.IgnorePixelScaling = false;

		//Assert
		paintCount.Should().Be(2);
	}

	[Fact]
	public void the_canvas_is_a_panel_so_it_can_hold_children_and_a_background()
	{
		//Arrange
		//Act
		//Assert
		//The control derives from Canvas, which is how it can present through Background at all;
		//an application also relies on it to overlay XAML children on the painted surface.
		typeof(SKXamlCanvas).Should().BeAssignableTo<Microsoft.UI.Xaml.Controls.Canvas>();
	}

	private sealed class PaintingCanvas : SKXamlCanvas
	{
		internal List<SKPaintSurfaceEventArgs> Overridden { get; } = new();

		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			Overridden.Add(e);
			base.OnPaintSurface(e);
		}
	}
}
