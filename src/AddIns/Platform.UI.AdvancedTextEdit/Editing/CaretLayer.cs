#nullable enable

using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;

using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/CaretLayer.cs in the AvalonEdit repo (MIT),
//a UIElement Layer whose OnRender drew the caret rectangle. In this port the caret layer is an
//IBackgroundRenderer draw phase (KnownLayer.Caret) on the text view's paint pass. The blink
//interval is a 500 ms constant (upstream read Win32.CaretBlinkTime, including its "blinking
//disabled" negative value); blink ticks only invalidate the caret layer, which repaints the
//render surface. The default caret brush falls back to the text view's foreground.

sealed class CaretLayer : IBackgroundRenderer
{
	/// <summary>
	/// The caret blink half-period. Upstream asked Win32 for the system blink time; this port
	/// uses the common 500 ms default on every platform.
	/// </summary>
	internal static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(500);

	readonly TextArea textArea;

	bool isVisible;
	Rect caretRectangle;

	readonly DispatcherTimer caretBlinkTimer = new DispatcherTimer();
	bool blink;

	public CaretLayer(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;
		caretBlinkTimer.Tick += CaretBlinkTimer_Tick;
	}

	public KnownLayer Layer {
		get { return KnownLayer.Caret; }
	}

	void CaretBlinkTimer_Tick(object? sender, object e)
	{
		blink = !blink;
		InvalidateVisual();
	}

	void InvalidateVisual()
	{
		textArea.TextView.InvalidateLayer(KnownLayer.Caret);
	}

	public void Show(Rect caretRectangle)
	{
		this.caretRectangle = caretRectangle;
		this.isVisible = true;
		StartBlinkAnimation();
		InvalidateVisual();
	}

	public void Hide()
	{
		if (isVisible)
		{
			isVisible = false;
			StopBlinkAnimation();
			InvalidateVisual();
		}
	}

	void StartBlinkAnimation()
	{
		blink = true; // the caret should be visible initially
		caretBlinkTimer.Interval = BlinkInterval;
		caretBlinkTimer.Start();
	}

	void StopBlinkAnimation()
	{
		caretBlinkTimer.Stop();
	}

	internal Brush? CaretBrush;

	public void Draw(TextView textView, SKCanvas canvas)
	{
		if (!isVisible || !blink)
			return;

		//was previously: the fallback read TextBlock.Foreground off the text view's inherited
		//properties; a Panel inherits none here, so the text view's internal Foreground is used.
		Brush? caretBrush = this.CaretBrush ?? textView.Foreground;
		SKColor color = VisualLineElementTextRunProperties.GetSolidColor(caretBrush) ?? SKColors.Black;

		if (this.textArea.OverstrikeMode)
		{
			color = color.WithAlpha(100);
		}

		Rect r = new Rect(caretRectangle.X - textView.HorizontalOffset,
						  caretRectangle.Y - textView.VerticalOffset,
						  caretRectangle.Width,
						  caretRectangle.Height);
		r = PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(textView));
		using var paint = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
		canvas.DrawRect(SKRect.Create((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height), paint);
	}
}
