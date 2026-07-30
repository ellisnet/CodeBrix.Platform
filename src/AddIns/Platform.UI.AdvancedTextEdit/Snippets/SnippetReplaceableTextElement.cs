#nullable enable

using System;
using System.Linq;

using SkiaSharp;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/SnippetReplaceableTextElement.cs in the
//AvalonEdit repo (MIT). The ToTextRun override is not ported (see SnippetElement.cs). The nested
//Renderer draws the geometry-builder output with SKPaint per the port's drawing rules: the
//background fill is LimeGreen at 40% opacity, and the active border is a black 1-DIP stroke
//(upstream's WPF DashStyles.Dot pen is approximated with a Skia dash effect).

/// <summary>
/// Text element that is supposed to be replaced by the user.
/// Will register an <see cref="IReplaceableActiveElement"/>.
/// </summary>
[Serializable]
public class SnippetReplaceableTextElement : SnippetTextElement
{
	/// <inheritdoc/>
	public override void Insert(InsertionContext context)
	{
		int start = context.InsertionPosition;
		base.Insert(context);
		int end = context.InsertionPosition;
		context.RegisterActiveElement(this, new ReplaceableActiveElement(context, start, end));
	}
}

/// <summary>
/// Interface for active element registered by <see cref="SnippetReplaceableTextElement"/>.
/// </summary>
public interface IReplaceableActiveElement : IActiveElement
{
	/// <summary>
	/// Gets the current text inside the element. Valid once the insertion has completed.
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Occurs when the text inside the element changes.
	/// </summary>
	event EventHandler? TextChanged;
}

sealed class ReplaceableActiveElement : IReplaceableActiveElement, IWeakEventListener
{
	readonly InsertionContext context;
	readonly int startOffset, endOffset;
	TextAnchor? start, end;

	public ReplaceableActiveElement(InsertionContext context, int startOffset, int endOffset)
	{
		this.context = context;
		this.startOffset = startOffset;
		this.endOffset = endOffset;
	}

	void AnchorDeleted(object? sender, EventArgs e)
	{
		context.Deactivate(new SnippetEventArgs(DeactivateReason.Deleted));
	}

	public void OnInsertionCompleted()
	{
		// anchors must be created in OnInsertionCompleted because they should move only
		// due to user insertions, not due to insertions of other snippet parts
		start = context.Document.CreateAnchor(startOffset);
		start.MovementType = AnchorMovementType.BeforeInsertion;
		end = context.Document.CreateAnchor(endOffset);
		end.MovementType = AnchorMovementType.AfterInsertion;
		start.Deleted += AnchorDeleted;
		end.Deleted += AnchorDeleted;

		// Be careful with references from the document to the editing/snippet layer - use weak events
		// to prevent memory leaks when the text area control gets dropped from the UI while the snippet is active.
		// The InsertionContext will keep us alive as long as the snippet is in interactive mode.
		TextDocumentWeakEventManager.TextChanged.AddListener(context.Document, this);

		background = new Renderer(KnownLayer.Background, this);
		foreground = new Renderer(KnownLayer.Text, this);
		context.TextArea.TextView.BackgroundRenderers.Add(background);
		context.TextArea.TextView.BackgroundRenderers.Add(foreground);
		context.TextArea.Caret.PositionChanged += Caret_PositionChanged;
		Caret_PositionChanged(null, EventArgs.Empty);

		this.Text = GetText();
	}

	public void Deactivate(SnippetEventArgs e)
	{
		TextDocumentWeakEventManager.TextChanged.RemoveListener(context.Document, this);
		if (background != null)
			context.TextArea.TextView.BackgroundRenderers.Remove(background);
		if (foreground != null)
			context.TextArea.TextView.BackgroundRenderers.Remove(foreground);
		context.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
	}

	bool isCaretInside;

	void Caret_PositionChanged(object? sender, EventArgs e)
	{
		ISegment? s = this.Segment;
		if (s != null)
		{
			bool newIsCaretInside = s.Contains(context.TextArea.Caret.Offset, 0);
			if (newIsCaretInside != isCaretInside)
			{
				isCaretInside = newIsCaretInside;
				if (foreground != null)
					context.TextArea.TextView.InvalidateLayer(foreground.Layer);
			}
		}
	}

	Renderer? background, foreground;

	public string Text { get; private set; } = string.Empty;

	string GetText()
	{
		if (start == null || end == null || start.IsDeleted || end.IsDeleted)
			return string.Empty;
		else
			return context.Document.GetText(start.Offset, Math.Max(0, end.Offset - start.Offset));
	}

	public event EventHandler? TextChanged;

	bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		if (managerType == typeof(TextDocumentWeakEventManager.TextChanged))
		{
			string newText = GetText();
			if (this.Text != newText)
			{
				this.Text = newText;
				TextChanged?.Invoke(this, e);
			}
			return true;
		}
		return false;
	}

	public bool IsEditable {
		get { return true; }
	}

	public ISegment? Segment {
		get {
			if (start == null || end == null || start.IsDeleted || end.IsDeleted)
				return null;
			else
				return new SimpleSegment(start.Offset, Math.Max(0, end.Offset - start.Offset));
		}
	}

	sealed class Renderer : IBackgroundRenderer
	{
		//was previously: a frozen LimeGreen SolidColorBrush with Opacity 0.4; LimeGreen is
		//(50, 205, 50), 40% opacity is alpha 102.
		static readonly SKColor BackgroundColor = new SKColor(50, 205, 50, 102);

		//was previously: a frozen black Pen with thickness 1 and DashStyles.Dot.
		const float ActiveBorderThickness = 1f;

		readonly ReplaceableActiveElement element;

		public Renderer(KnownLayer layer, ReplaceableActiveElement element)
		{
			this.Layer = layer;
			this.element = element;
		}

		public KnownLayer Layer { get; }

		public void Draw(TextView textView, SKCanvas canvas)
		{
			ISegment? s = element.Segment;
			if (s == null)
				return;
			BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
			geoBuilder.AlignToWholePixels = true;
			geoBuilder.BorderThickness = ActiveBorderThickness;
			if (Layer == KnownLayer.Background)
			{
				geoBuilder.AddSegment(textView, s);
				using SKPath? path = geoBuilder.CreatePath();
				if (path != null)
				{
					using var fillPaint = new SKPaint {
						Color = BackgroundColor,
						Style = SKPaintStyle.Fill,
						IsAntialias = true,
					};
					canvas.DrawPath(path, fillPaint);
				}
			}
			else
			{
				// draw foreground only if active
				if (element.isCaretInside)
				{
					geoBuilder.AddSegment(textView, s);
					foreach (BoundActiveElement boundElement in element.context.ActiveElements.OfType<BoundActiveElement>())
					{
						if (boundElement.targetElement == element)
						{
							geoBuilder.AddSegment(textView, boundElement.Segment);
							geoBuilder.CloseFigure();
						}
					}
					using SKPath? path = geoBuilder.CreatePath();
					if (path != null)
					{
						using var dashEffect = SKPathEffect.CreateDash(new float[] { 1f, 2f }, 0);
						using var borderPaint = new SKPaint {
							Color = SKColors.Black,
							Style = SKPaintStyle.Stroke,
							StrokeWidth = ActiveBorderThickness,
							PathEffect = dashEffect,
						};
						canvas.DrawPath(path, borderPaint);
					}
				}
			}
		}
	}
}
