#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/LineNumberMargin.cs in the AvalonEdit repo (MIT).
//The measuring/selection logic is transliterated. The WPF OnRender/FormattedText pair became a
//hosted RenderCanvas (child 0, mirroring TextView) painted through TextLayoutEngine: the numbers
//render right-aligned with the text view's font at 3/4 of its size. The margin has no inherited
//font/foreground dependency properties (Panel base), so it exposes its own Foreground brush that
//the editor control binds to its LineNumbersForeground. Mouse handling moved to pointer events
//with explicit capture; the protected WPF Typeface field is gone (the engine resolves fonts from
//the family name), only emSize remains for subclasses.

/// <summary>
/// Margin showing line numbers.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class LineNumberMargin : AbstractMargin, IWeakEventListener
{
	readonly RenderCanvas renderCanvas = new RenderCanvas();

	/// <summary>
	/// Creates a new instance of a LineNumberMargin
	/// </summary>
	public LineNumberMargin()
	{
		//was previously: FlowDirection was forced LeftToRight to override property value
		//inheritance; the canvas paints left-to-right regardless in this port.
		renderCanvas.Paint += RenderCanvasPaint;
		Children.Add(renderCanvas);

		//was previously: a HitTestCore override accepted clicks on the transparent background.
		Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 255, 255, 255));

		PointerPressed += LineNumberMargin_PointerPressed;
		PointerMoved += LineNumberMargin_PointerMoved;
		PointerReleased += LineNumberMargin_PointerReleased;
	}

	TextArea? textArea;

	/// <summary>
	/// The font size used for rendering the line numbers: 3/4 of the text view's font size.
	/// This field is calculated in MeasureOverride().
	/// </summary>
	protected double emSize = 9.0;

	/// <summary>
	/// The <see cref="Foreground"/> property.
	/// </summary>
	public static readonly DependencyProperty ForegroundProperty =
		DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(LineNumberMargin),
									new PropertyMetadata(null, OnForegroundChanged));

	/// <summary>
	/// Gets/Sets the brush used for the line number text. The editor control keeps this in sync
	/// with its line-numbers foreground; a null value renders gray.
	/// </summary>
	//was previously: the margin used the inherited Control.Foreground value.
	public Brush? Foreground {
		get { return (Brush?)GetValue(ForegroundProperty); }
		set { SetValue(ForegroundProperty, value); }
	}

	static void OnForegroundChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((LineNumberMargin)dp).renderCanvas.Invalidate();
	}

	TextRunDescriptor CreateRun(string text)
	{
		TextView? textView = this.TextView;
		return new TextRunDescriptor(
			text,
			textView?.FontFamily,
			(float)emSize,
			VisualLineElementTextRunProperties.ToTextFontWeight(textView?.FontWeight ?? Microsoft.UI.Text.FontWeights.Normal),
			VisualLineElementTextRunProperties.ToTextFontStyle(textView?.FontStyle ?? global::Windows.UI.Text.FontStyle.Normal),
			VisualLineElementTextRunProperties.ToTextFontStretch(textView?.FontStretch ?? global::Windows.UI.Text.FontStretch.Normal));
	}

	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
	{
		TextView? textView = this.TextView;
		emSize = textView != null ? textView.FontSize * 3.0 / 4.0 : 9.0;

		renderCanvas.Measure(availableSize);

		using TextLayoutResult layout = TextLayoutEngine.Layout(new[] { CreateRun(new string('9', maxLineNumberLength)) });
		return new Size(layout.Size.Width, 0);
	}

	/// <inheritdoc/>
	protected override Size ArrangeOverride(Size finalSize)
	{
		renderCanvas.Arrange(new Rect(new Point(0, 0), finalSize));
		renderCanvas.Invalidate();
		return finalSize;
	}

	//was previously: OnRender(DrawingContext) drawing FormattedText objects; one paint pass on
	//the hosted canvas draws every visible line's number right-aligned.
	void RenderCanvasPaint(SKCanvas canvas, SKSize size)
	{
		TextView? textView = this.TextView;
		if (textView == null || !textView.VisualLinesValid)
			return;
		SKColor foreground = VisualLineElementTextRunProperties.GetSolidColor(Foreground)
			?? new SKColor(128, 128, 128);
		using var paint = new SKPaint { Color = foreground, IsAntialias = true };
		foreach (VisualLine line in textView.VisualLines)
		{
			int lineNumber = line.FirstDocumentLine.LineNumber;
			using TextLayoutResult layout = TextLayoutEngine.Layout(
				new[] { CreateRun(lineNumber.ToString(CultureInfo.CurrentCulture)) });
			double y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
			layout.Draw(canvas,
						new SKPoint((float)(size.Width - layout.Size.Width), (float)(y - textView.VerticalOffset)),
						paint);
		}
	}

	/// <inheritdoc/>
	protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
	{
		if (oldTextView != null)
		{
			oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
			oldTextView.ScrollOffsetChanged -= TextViewVisualLinesChanged;
		}
		base.OnTextViewChanged(oldTextView, newTextView);
		if (newTextView != null)
		{
			newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
			newTextView.ScrollOffsetChanged += TextViewVisualLinesChanged;

			// find the text area belonging to the new text view
			textArea = newTextView.GetService(typeof(TextArea)) as TextArea;
		}
		else
		{
			textArea = null;
		}
		renderCanvas.Invalidate();
	}

	/// <inheritdoc/>
	protected override void OnDocumentChanged(TextDocument? oldDocument, TextDocument? newDocument)
	{
		if (oldDocument != null)
		{
			PropertyChangedWeakEventManager.RemoveListener(oldDocument, this);
		}
		base.OnDocumentChanged(oldDocument, newDocument);
		if (newDocument != null)
		{
			PropertyChangedWeakEventManager.AddListener(newDocument, this);
		}
		OnDocumentLineCountChanged();
	}

	/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
	protected virtual bool ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		if (managerType == typeof(PropertyChangedWeakEventManager))
		{
			//was previously: WPF's PropertyChangedEventManager filtered on "LineCount"; the
			//port's manager delivers every property change, so the filter lives here.
			if (e is PropertyChangedEventArgs args
				&& (args.PropertyName == null || args.PropertyName == nameof(TextDocument.LineCount)))
			{
				OnDocumentLineCountChanged();
			}
			return true;
		}
		return false;
	}

	bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		return ReceiveWeakEvent(managerType, sender, e);
	}

	/// <summary>
	/// Maximum length of a line number, in characters
	/// </summary>
	protected int maxLineNumberLength = 1;

	void OnDocumentLineCountChanged()
	{
		int documentLineCount = Document != null ? Document.LineCount : 1;
		int newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

		// The margin looks too small when there is only one digit, so always reserve space for
		// at least two digits
		if (newLength < 2)
			newLength = 2;

		if (newLength != maxLineNumberLength)
		{
			maxLineNumberLength = newLength;
			InvalidateMeasure();
			renderCanvas.Invalidate();
		}
	}

	void TextViewVisualLinesChanged(object? sender, EventArgs e)
	{
		renderCanvas.Invalidate();
	}

	AnchorSegment? selectionStart;
	bool selecting;
	Pointer? capturedPointer;

	void LineNumberMargin_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		TextView? textView = this.TextView;
		if (!e.Handled && textView != null && textArea != null && textArea.Document != null
			&& e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			e.Handled = true;
			textArea.Focus(FocusState.Pointer);

			SimpleSegment currentSeg = GetTextLineSegment(textView, e);
			if (currentSeg == SimpleSegment.Invalid)
				return;
			textArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
			if (TryCapturePointer(e.Pointer))
			{
				selecting = true;
				selectionStart = new AnchorSegment(textArea.Document, currentSeg.Offset, currentSeg.Length);
				bool shift = (e.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
				if (shift)
				{
					if (textArea.Selection is SimpleSelection simpleSelection
						&& simpleSelection.SurroundingSegment is ISegment surroundingSegment)
					{
						selectionStart = new AnchorSegment(textArea.Document, surroundingSegment);
					}
				}
				textArea.Selection = Selection.Create(textArea, selectionStart);
				if (shift)
				{
					ExtendSelection(currentSeg);
				}
				textArea.Caret.BringCaretToView(5.0);
			}
		}
	}

	bool TryCapturePointer(Pointer pointer)
	{
		if (CapturePointer(pointer))
		{
			capturedPointer = pointer;
			return true;
		}
		return false;
	}

	void ReleaseCapturedPointer()
	{
		Pointer? pointer = capturedPointer;
		capturedPointer = null;
		if (pointer != null)
		{
			ReleasePointerCapture(pointer);
		}
	}

	SimpleSegment GetTextLineSegment(TextView textView, PointerRoutedEventArgs e)
	{
		Point pos = e.GetCurrentPoint(textView).Position;
		pos = new Point(0, pos.Y.CoerceValue(0, textView.ActualHeight) + textView.VerticalOffset);
		VisualLine? vl = textView.GetVisualLineFromVisualTop(pos.Y);
		if (vl == null)
			return SimpleSegment.Invalid;
		TextLineLayout tl = vl.GetTextLineByVisualYPosition(pos.Y);
		int visualStartColumn = vl.GetTextLineVisualStartColumn(tl);
		int visualEndColumn = visualStartColumn + tl.Length;
		int relStart = vl.FirstDocumentLine.Offset;
		int startOffset = vl.GetRelativeOffset(visualStartColumn) + relStart;
		int endOffset = vl.GetRelativeOffset(visualEndColumn) + relStart;
		if (endOffset == vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length)
			endOffset += vl.LastDocumentLine.DelimiterLength;
		return new SimpleSegment(startOffset, endOffset - startOffset);
	}

	void ExtendSelection(SimpleSegment currentSeg)
	{
		if (textArea == null || selectionStart == null)
			return;
		if (currentSeg.Offset < selectionStart.Offset)
		{
			textArea.Caret.Offset = currentSeg.Offset;
			textArea.Selection = Selection.Create(textArea, currentSeg.Offset, selectionStart.Offset + selectionStart.Length);
		}
		else
		{
			textArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
			textArea.Selection = Selection.Create(textArea, selectionStart.Offset, currentSeg.Offset + currentSeg.Length);
		}
	}

	void LineNumberMargin_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		TextView? textView = this.TextView;
		if (selecting && textArea != null && textView != null)
		{
			e.Handled = true;
			SimpleSegment currentSeg = GetTextLineSegment(textView, e);
			if (currentSeg == SimpleSegment.Invalid)
				return;
			ExtendSelection(currentSeg);
			textArea.Caret.BringCaretToView(5.0);
		}
	}

	void LineNumberMargin_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (selecting)
		{
			selecting = false;
			selectionStart = null;
			ReleaseCapturedPointer();
			e.Handled = true;
		}
	}
}
