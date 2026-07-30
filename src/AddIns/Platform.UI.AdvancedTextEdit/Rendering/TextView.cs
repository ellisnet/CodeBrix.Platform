#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/TextView.cs in the AvalonEdit repo (MIT).
//The virtualization lifecycle - height tree, visual-line construction and reuse, redraw
//invalidation - is transliterated. The structural re-expressions:
//- FrameworkElement+IScrollInfo became Panel with an explicit scroll surface (HorizontalOffset/
//  VerticalOffset, extent/viewport, SetScrollOffset, MakeVisible as pure math); the editor
//  template syncs ScrollBar controls to it, and word wrap is the public WordWrap property
//  instead of the inverse of IScrollInfo.CanHorizontallyScroll.
//- The UIElement layer stack (Layer/TextLayer/InsertLayer) collapsed into one paint pass on the
//  child RenderCanvas: background renderers per known layer, each visual line's Draw (element
//  backgrounds + text + decorations), then caret-layer renderers. IBackgroundRenderer is the
//  layer extension point; InvalidateLayer only schedules a repaint.
//- DispatcherPriority parameters were dropped from Redraw/InvalidateLayer. Upstream's default
//  Normal priority invalidated measure immediately (Normal > Render in WPF); the batching path
//  for below-Render priorities went away with the parameters, so every redraw is immediate.
//- The WPF TextFormatter/TextViewCachedElements/typeface plumbing is replaced by the static
//  TextLayoutEngine; the font is a set of internal settable properties the editor pushes down
//  (a Panel inherits no Control font properties), and the WPF Pen properties became
//  brush+thickness pairs (ColumnRulerBrush/-Thickness, CurrentLineBorderBrush/-Thickness).
//- MouseHover events are plain .NET events (no tunneling/bubbling RoutedEvent re-raise), element
//  pointer routing subscribes PointerPressed/PointerReleased (Panel has no override seam), and
//  hit testing everywhere comes from a transparent Panel.Background instead of HitTestCore.
//- Mouse.UpdateCursor/OnQueryCursor cursor shaping, UI automation, and the keyboard-focus
//  workaround for removed inline objects have no counterpart here and are dropped.

/// <summary>
/// A virtualizing panel producing+showing <see cref="VisualLine"/>s for a <see cref="TextDocument"/>.
///
/// This is the heart of the text editor, this class controls the text rendering process.
///
/// Taken as a standalone control, it's a text viewer without any editing capability.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class TextView : Panel, ITextEditorComponent, IWeakEventListener
{
	#region Constructor
	readonly RenderCanvas renderCanvas = new RenderCanvas();
	readonly ObserveAddRemoveCollection<VisualLineElementGenerator> elementGenerators;
	readonly ObserveAddRemoveCollection<IVisualLineTransformer> lineTransformers;
	readonly ObserveAddRemoveCollection<IBackgroundRenderer> backgroundRenderers;
	readonly ColumnRulerRenderer columnRulerRenderer;
	readonly CurrentLineHighlightRenderer currentLineHighlightRenderer;
	readonly MouseHoverLogic hoverLogic;

	/// <summary>
	/// Creates a new TextView instance.
	/// </summary>
	public TextView()
	{
		services.AddService(typeof(TextView), this);
		elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
		lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
		backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
		columnRulerRenderer = new ColumnRulerRenderer(this);
		currentLineHighlightRenderer = new CurrentLineHighlightRenderer(this);
		this.Options = new AdvancedTextEditOptions();

		Debug.Assert(singleCharacterElementGenerator != null); // assert that the option change created the builtin element generators

		//was previously: the layer collection was populated here (InsertLayer(textLayer, ...));
		//the port draws everything in one paint pass on this child canvas (child 0).
		renderCanvas.Paint += RenderCanvasPaint;
		Children.Add(renderCanvas);

		//was previously: a HitTestCore override accepted clicks even where the text area draws no
		//background; a transparent Panel background achieves the same here.
		Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 255, 255, 255));

		this.hoverLogic = new MouseHoverLogic(this);
		//was previously: each hover notification re-raised a tunneling Preview* RoutedEvent followed
		//by a bubbling one with Handled propagation; the port raises two plain .NET events in the
		//same order without routing.
		this.hoverLogic.MouseHover += (sender, e) =>
		{
			PreviewMouseHover?.Invoke(this, e);
			MouseHover?.Invoke(this, e);
		};
		this.hoverLogic.MouseHoverStopped += (sender, e) =>
		{
			PreviewMouseHoverStopped?.Invoke(this, e);
			MouseHoverStopped?.Invoke(this, e);
		};

		PointerPressed += TextViewPointerPressed;
		PointerReleased += TextViewPointerReleased;
		SizeChanged += (sender, e) => InvalidateLayer(KnownLayer.Selection);
	}
	#endregion

	#region Thread verification
	/// <summary>
	/// Throws when the view is accessed from a thread other than its UI thread.
	/// No-op when the view has no dispatcher (host-free unit tests).
	/// </summary>
	void VerifyAccess()
	{
		var dispatcherQueue = DispatcherQueue;
		if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
			throw new InvalidOperationException("TextView can be accessed only from the thread that owns it.");
	}
	#endregion

	#region Document Property
	/// <summary>
	/// Document property.
	/// </summary>
	public static readonly DependencyProperty DocumentProperty =
		DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextView),
									new PropertyMetadata(null, OnDocumentChanged));

	TextDocument? document;
	HeightTree? heightTree;

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// The value is null while no document is attached.
	/// </summary>
	public TextDocument Document
	{
		get { return (TextDocument)GetValue(DocumentProperty); }
		set { SetValue(DocumentProperty, value); }
	}

	static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextView)dp).OnDocumentChanged((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);
	}

	/// <summary>
	/// Occurs when the document property has changed.
	/// </summary>
	public event EventHandler? DocumentChanged;

	void OnDocumentChanged(TextDocument? oldValue, TextDocument? newValue)
	{
		if (oldValue != null)
		{
			heightTree?.Dispose();
			heightTree = null;
			//was previously: the WPF text formatter and the cached-element table were also disposed
			//here; the engine façade is static and this port keeps no cached elements.
			TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
		}
		this.document = newValue;
		ClearScrollData();
		ClearVisualLines();
		if (newValue != null)
		{
			TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
			InvalidateDefaultTextMetrics(); // measuring DefaultLineHeight must happen before the height tree is built
			heightTree = new HeightTree(newValue, DefaultLineHeight);
		}
		InvalidateMeasure();
		DocumentChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
	protected virtual bool ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		if (managerType == typeof(TextDocumentWeakEventManager.Changing))
		{
			DocumentChangeEventArgs change = (DocumentChangeEventArgs)e;
			Redraw(change.Offset, change.RemovalLength);
			return true;
		}
		else if (managerType == typeof(PropertyChangedWeakEventManager))
		{
			OnOptionChanged((PropertyChangedEventArgs)e);
			return true;
		}
		return false;
	}

	bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		return ReceiveWeakEvent(managerType, sender, e);
	}
	#endregion

	#region Options property
	/// <summary>
	/// Options property.
	/// </summary>
	public static readonly DependencyProperty OptionsProperty =
		DependencyProperty.Register(nameof(Options), typeof(AdvancedTextEditOptions), typeof(TextView),
									new PropertyMetadata(null, OnOptionsChanged));

	/// <summary>
	/// Gets/Sets the options used by the text editor.
	/// </summary>
	public AdvancedTextEditOptions Options
	{
		get { return (AdvancedTextEditOptions)GetValue(OptionsProperty); }
		set { SetValue(OptionsProperty, value); }
	}

	/// <summary>
	/// Occurs when a text editor option has changed.
	/// </summary>
	public event PropertyChangedEventHandler? OptionChanged;

	/// <summary>
	/// Raises the <see cref="OptionChanged"/> event.
	/// </summary>
	protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
	{
		OptionChanged?.Invoke(this, e);

		if (Options.ShowColumnRuler)
			columnRulerRenderer.SetRuler(Options.ColumnRulerPosition, ColumnRulerBrush, ColumnRulerThickness);
		else
			columnRulerRenderer.SetRuler(-1, ColumnRulerBrush, ColumnRulerThickness);

		UpdateBuiltinElementGeneratorsFromOptions();
		Redraw();
	}

	static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextView)dp).OnOptionsChanged((AdvancedTextEditOptions?)e.OldValue, (AdvancedTextEditOptions?)e.NewValue);
	}

	void OnOptionsChanged(AdvancedTextEditOptions? oldValue, AdvancedTextEditOptions? newValue)
	{
		if (oldValue != null)
		{
			PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
		}
		if (newValue != null)
		{
			PropertyChangedWeakEventManager.AddListener(newValue, this);
		}
		OnOptionChanged(new PropertyChangedEventArgs(null));
	}
	#endregion

	#region ElementGenerators+LineTransformers Properties
	/// <summary>
	/// Gets a collection where element generators can be registered.
	/// </summary>
	public IList<VisualLineElementGenerator> ElementGenerators
	{
		get { return elementGenerators; }
	}

	void ElementGenerator_Added(VisualLineElementGenerator generator)
	{
		ConnectToTextView(generator);
		Redraw();
	}

	void ElementGenerator_Removed(VisualLineElementGenerator generator)
	{
		DisconnectFromTextView(generator);
		Redraw();
	}

	/// <summary>
	/// Gets a collection where line transformers can be registered.
	/// </summary>
	public IList<IVisualLineTransformer> LineTransformers
	{
		get { return lineTransformers; }
	}

	void LineTransformer_Added(IVisualLineTransformer lineTransformer)
	{
		ConnectToTextView(lineTransformer);
		Redraw();
	}

	void LineTransformer_Removed(IVisualLineTransformer lineTransformer)
	{
		DisconnectFromTextView(lineTransformer);
		Redraw();
	}
	#endregion

	#region Builtin ElementGenerators
	SingleCharacterElementGenerator? singleCharacterElementGenerator;
	LinkElementGenerator? linkElementGenerator;
	MailLinkElementGenerator? mailLinkElementGenerator;

	void UpdateBuiltinElementGeneratorsFromOptions()
	{
		AdvancedTextEditOptions options = this.Options;

		//was previously: the single-character generator was registered only when
		//ShowBoxForControlCharacters/ShowSpaces/ShowTabs demanded it; in this port every tab needs
		//the generator (the engine has no tab stops - the tab element expands itself), so it is
		//always registered and the options only toggle its markers.
		AddRemoveDefaultElementGeneratorOnDemand(ref singleCharacterElementGenerator, true);
		AddRemoveDefaultElementGeneratorOnDemand(ref linkElementGenerator, options.EnableHyperlinks);
		AddRemoveDefaultElementGeneratorOnDemand(ref mailLinkElementGenerator, options.EnableEmailHyperlinks);
	}

	void AddRemoveDefaultElementGeneratorOnDemand<T>(ref T? generator, bool demand)
		where T : VisualLineElementGenerator, IBuiltinElementGenerator, new()
	{
		if (demand)
		{
			if (generator == null)
			{
				generator = new T();
				this.ElementGenerators.Add(generator);
			}
		}
		else if (generator != null)
		{
			this.ElementGenerators.Remove(generator);
			generator = null;
		}
		generator?.FetchOptions(this.Options);
	}
	#endregion

	#region Inline object handling
	readonly List<InlineObjectRun> inlineObjects = new List<InlineObjectRun>();

	/// <summary>
	/// Adds a new inline object.
	/// </summary>
	internal void AddInlineObject(InlineObjectRun inlineObject)
	{
		Debug.Assert(inlineObject.VisualLine != null);

		// Remove inline object if its already added, can happen e.g. when recreating textrun for word-wrapping
		bool alreadyAdded = false;
		for (int i = 0; i < inlineObjects.Count; i++)
		{
			if (inlineObjects[i].Element == inlineObject.Element)
			{
				RemoveInlineObjectRun(inlineObjects[i], true);
				inlineObjects.RemoveAt(i);
				alreadyAdded = true;
				break;
			}
		}

		inlineObjects.Add(inlineObject);
		if (!alreadyAdded)
		{
			Children.Add(inlineObject.Element);
		}
		inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		inlineObject.DesiredSize = inlineObject.Element.DesiredSize;
	}

	void MeasureInlineObjects()
	{
		// As part of MeasureOverride(), re-measure the inline objects
		foreach (InlineObjectRun inlineObject in inlineObjects)
		{
			VisualLine? ownerLine = inlineObject.VisualLine;
			if (ownerLine == null || ownerLine.IsDisposed)
			{
				// Don't re-measure inline objects that are going to be removed anyways.
				// If the inline object will be reused in a different VisualLine, we'll measure it in the AddInlineObject() call.
				continue;
			}
			inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			if (!inlineObject.Element.DesiredSize.IsClose(inlineObject.DesiredSize))
			{
				// the element changed size -> recreate its parent visual line
				inlineObject.DesiredSize = inlineObject.Element.DesiredSize;
				if (allVisualLines.Remove(ownerLine))
				{
					DisposeVisualLine(ownerLine);
				}
			}
		}
	}

	readonly List<VisualLine> visualLinesWithOutstandingInlineObjects = new List<VisualLine>();

	void RemoveInlineObjects(VisualLine visualLine)
	{
		// Delay removing inline objects:
		// A document change immediately invalidates affected visual lines, but it does not
		// cause an immediate redraw.
		// To prevent inline objects from flickering when they are recreated, we delay removing
		// inline objects until the next redraw.
		if (visualLine.hasInlineObjects)
		{
			visualLinesWithOutstandingInlineObjects.Add(visualLine);
		}
	}

	/// <summary>
	/// Remove the inline objects that were marked for removal.
	/// </summary>
	void RemoveInlineObjectsNow()
	{
		if (visualLinesWithOutstandingInlineObjects.Count == 0)
			return;
		inlineObjects.RemoveAll(
			ior =>
			{
				if (ior.VisualLine != null && visualLinesWithOutstandingInlineObjects.Contains(ior.VisualLine))
				{
					RemoveInlineObjectRun(ior, false);
					return true;
				}
				return false;
			});
		visualLinesWithOutstandingInlineObjects.Clear();
	}

	// Remove InlineObjectRun.Element from the children.
	// Caller of RemoveInlineObjectRun will remove it from inlineObjects collection.
	void RemoveInlineObjectRun(InlineObjectRun ior, bool keepElement)
	{
		//was previously: when the removed element held keyboard focus, focus was moved to the next
		//focusable parent to work around a WPF focus reset; no equivalent seam exists here.
		ior.VisualLine = null;
		if (!keepElement)
			Children.Remove(ior.Element);
	}
	#endregion

	#region Brushes
	static Brush CreateSolidBrush(byte a, byte r, byte g, byte b)
	{
		return new SolidColorBrush(global::Windows.UI.Color.FromArgb(a, r, g, b));
	}

	static void OnRedrawRequiredPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		//was previously: brush changes recreated the cached-element table before redrawing; there
		//is no cache in this port, so a redraw suffices (the values are read while formatting).
		((TextView)dp).Redraw();
	}

	/// <summary>
	/// NonPrintableCharacterBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty NonPrintableCharacterBrushProperty =
		DependencyProperty.Register(nameof(NonPrintableCharacterBrush), typeof(Brush), typeof(TextView),
									new PropertyMetadata(CreateSolidBrush(255, 211, 211, 211), OnRedrawRequiredPropertyChanged));

	/// <summary>
	/// Gets/sets the Brush used for displaying non-printable characters.
	/// </summary>
	public Brush? NonPrintableCharacterBrush
	{
		get { return (Brush?)GetValue(NonPrintableCharacterBrushProperty); }
		set { SetValue(NonPrintableCharacterBrushProperty, value); }
	}

	/// <summary>
	/// LinkTextForegroundBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty LinkTextForegroundBrushProperty =
		DependencyProperty.Register(nameof(LinkTextForegroundBrush), typeof(Brush), typeof(TextView),
									new PropertyMetadata(CreateSolidBrush(255, 0, 0, 255), OnRedrawRequiredPropertyChanged));

	/// <summary>
	/// Gets/sets the Brush used for displaying link texts.
	/// </summary>
	public Brush? LinkTextForegroundBrush
	{
		get { return (Brush?)GetValue(LinkTextForegroundBrushProperty); }
		set { SetValue(LinkTextForegroundBrushProperty, value); }
	}

	/// <summary>
	/// LinkTextBackgroundBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty LinkTextBackgroundBrushProperty =
		DependencyProperty.Register(nameof(LinkTextBackgroundBrush), typeof(Brush), typeof(TextView),
									new PropertyMetadata(CreateSolidBrush(0, 255, 255, 255), OnRedrawRequiredPropertyChanged));

	/// <summary>
	/// Gets/sets the Brush used for the background of link texts.
	/// </summary>
	public Brush? LinkTextBackgroundBrush
	{
		get { return (Brush?)GetValue(LinkTextBackgroundBrushProperty); }
		set { SetValue(LinkTextBackgroundBrushProperty, value); }
	}

	/// <summary>
	/// LinkTextUnderline dependency property.
	/// </summary>
	public static readonly DependencyProperty LinkTextUnderlineProperty =
		DependencyProperty.Register(nameof(LinkTextUnderline), typeof(bool), typeof(TextView),
									new PropertyMetadata(Boxes.True, OnRedrawRequiredPropertyChanged));

	/// <summary>
	/// Gets/sets whether to underline link texts.
	/// </summary>
	/// <remarks>
	/// Note that when setting this property to false, link text remains clickable and the LinkTextForegroundBrush (if any) is still applied.
	/// Set AdvancedTextEditOptions.EnableHyperlinks and EnableEmailHyperlinks to false to disable links completely.
	/// </remarks>
	public bool LinkTextUnderline
	{
		get { return (bool)GetValue(LinkTextUnderlineProperty); }
		set { SetValue(LinkTextUnderlineProperty, Boxes.Box(value)); }
	}
	#endregion

	#region WordWrap
	/// <summary>
	/// WordWrap dependency property.
	/// </summary>
	public static readonly DependencyProperty WordWrapProperty =
		DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(TextView),
									new PropertyMetadata(Boxes.False, OnWordWrapChanged));

	/// <summary>
	/// Gets/Sets whether long lines wrap at the viewport width. While enabled, the horizontal
	/// scroll offset stays 0 and the wrap width is the width the view was measured with.
	/// </summary>
	//was previously: word wrap was the inverse of IScrollInfo.CanHorizontallyScroll, set by the
	//ScrollViewer; this port has no ScrollViewer, so the editor control sets this property.
	public bool WordWrap
	{
		get { return (bool)GetValue(WordWrapProperty); }
		set { SetValue(WordWrapProperty, Boxes.Box(value)); }
	}

	static void OnWordWrapChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextView)dp).OnWordWrapChanged();
	}

	void OnWordWrapChanged()
	{
		ClearVisualLines();
		if (WordWrap && scrollOffsetX != 0)
			ApplyScrollOffset(0, scrollOffsetY);
		InvalidateMeasure();
	}
	#endregion

	#region Redraw methods / VisualLine invalidation
	/// <summary>
	/// Causes the text editor to regenerate all visual lines.
	/// </summary>
	//was previously: every Redraw overload took a DispatcherPriority; the priority parameters are
	//dropped and each redraw invalidates immediately (see the file header note).
	public void Redraw()
	{
		VerifyAccess();
		ClearVisualLines();
		InvalidateMeasure();
	}

	/// <summary>
	/// Causes the text editor to regenerate the specified visual line.
	/// </summary>
	public void Redraw(VisualLine visualLine)
	{
		VerifyAccess();
		if (allVisualLines.Remove(visualLine))
		{
			DisposeVisualLine(visualLine);
			InvalidateMeasure();
		}
	}

	/// <summary>
	/// Causes the text editor to redraw all lines overlapping with the specified segment.
	/// </summary>
	public void Redraw(int offset, int length)
	{
		VerifyAccess();
		bool changedSomethingBeforeOrInLine = false;
		for (int i = 0; i < allVisualLines.Count; i++)
		{
			VisualLine visualLine = allVisualLines[i];
			int lineStart = visualLine.FirstDocumentLine.Offset;
			int lineEnd = visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength;
			if (offset <= lineEnd)
			{
				changedSomethingBeforeOrInLine = true;
				if (offset + length >= lineStart)
				{
					allVisualLines.RemoveAt(i--);
					DisposeVisualLine(visualLine);
				}
			}
		}
		if (changedSomethingBeforeOrInLine)
		{
			// Repaint not only when something in visible area was changed, but also when anything in front of it
			// was changed. We might have to redraw the line number margin. Or the highlighting changed.
			// However, we'll try to reuse the existing VisualLines.
			InvalidateMeasure();
		}
	}

	/// <summary>
	/// Causes the text editor to redraw all lines overlapping with the specified segment.
	/// Does nothing if segment is null.
	/// </summary>
	public void Redraw(ISegment? segment)
	{
		if (segment != null)
		{
			Redraw(segment.Offset, segment.Length);
		}
	}

	/// <summary>
	/// Causes a known layer to redraw.
	/// This method does not invalidate visual lines;
	/// use the <see cref="Redraw()"/> method to do that.
	/// </summary>
	//was previously: layers were UIElements arranged during measure, so this invalidated measure;
	//a layer is a draw phase on the render canvas here, so a repaint is the exact equivalent.
	public void InvalidateLayer(KnownLayer knownLayer)
	{
		renderCanvas.Invalidate();
	}

	/// <summary>
	/// Invalidates all visual lines.
	/// The caller of ClearVisualLines() must also call InvalidateMeasure() to ensure
	/// that the visual lines will be recreated.
	/// </summary>
	void ClearVisualLines()
	{
		visibleVisualLines = null;
		if (allVisualLines.Count != 0)
		{
			foreach (VisualLine visualLine in allVisualLines)
			{
				DisposeVisualLine(visualLine);
			}
			allVisualLines.Clear();
		}
	}

	void DisposeVisualLine(VisualLine visualLine)
	{
		if (newVisualLines != null && newVisualLines.Contains(visualLine))
		{
			throw new ArgumentException("Cannot dispose visual line because it is in construction!");
		}
		visibleVisualLines = null;
		visualLine.Dispose();
		RemoveInlineObjects(visualLine);
	}
	#endregion

	#region Get(OrConstruct)VisualLine
	/// <summary>
	/// Gets the visual line that contains the document line with the specified number.
	/// Returns null if the document line is outside the visible range.
	/// </summary>
	public VisualLine? GetVisualLine(int documentLineNumber)
	{
		// TODO: EnsureVisualLines() ?
		foreach (VisualLine visualLine in allVisualLines)
		{
			Debug.Assert(visualLine.IsDisposed == false);
			int start = visualLine.FirstDocumentLine.LineNumber;
			int end = visualLine.LastDocumentLine.LineNumber;
			if (documentLineNumber >= start && documentLineNumber <= end)
				return visualLine;
		}
		return null;
	}

	/// <summary>
	/// Gets the visual line that contains the document line with the specified number.
	/// If that line is outside the visible range, a new VisualLine for that document line is constructed.
	/// </summary>
	public VisualLine GetOrConstructVisualLine(DocumentLine documentLine)
	{
		if (documentLine == null)
			throw new ArgumentNullException(nameof(documentLine));
		TextDocument document = this.document ?? throw ThrowUtil.NoDocumentAssigned();
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		if (!document.Lines.Contains(documentLine))
			throw new InvalidOperationException("Line belongs to wrong document");
		VerifyAccess();

		VisualLine? l = GetVisualLine(documentLine.LineNumber);
		if (l == null)
		{
			GlobalTextRunProperties globalTextRunProperties = CreateGlobalTextRunProperties();

			while (heightTree.GetIsCollapsed(documentLine.LineNumber))
			{
				DocumentLine? previousLine = documentLine.PreviousLine;
				if (previousLine == null)
					break; // line 1 is never collapsed while the height tree is consistent
				documentLine = previousLine;
			}

			l = BuildVisualLine(documentLine,
								globalTextRunProperties,
								elementGenerators.ToArray(), lineTransformers.ToArray(),
								lastAvailableSize);
			allVisualLines.Add(l);
			// update all visual top values (building the line might have changed visual top of other lines due to word wrapping)
			foreach (var line in allVisualLines)
			{
				line.VisualTop = heightTree.GetVisualPosition(line.FirstDocumentLine);
			}
		}
		return l;
	}
	#endregion

	#region Visual Lines (fields and properties)
	List<VisualLine> allVisualLines = new List<VisualLine>();
	ReadOnlyCollection<VisualLine>? visibleVisualLines;
	double clippedPixelsOnTop;
	List<VisualLine>? newVisualLines;

	/// <summary>
	/// Gets the currently visible visual lines.
	/// </summary>
	/// <exception cref="VisualLinesInvalidException">
	/// Gets thrown if there are invalid visual lines when this property is accessed.
	/// You can use the <see cref="VisualLinesValid"/> property to check for this case,
	/// or use the <see cref="EnsureVisualLines()"/> method to force creating the visual lines
	/// when they are invalid.
	/// </exception>
	public ReadOnlyCollection<VisualLine> VisualLines
	{
		get
		{
			if (visibleVisualLines == null)
				throw new VisualLinesInvalidException();
			return visibleVisualLines;
		}
	}

	/// <summary>
	/// Gets whether the visual lines are valid.
	/// Will return false after a call to Redraw().
	/// Accessing the visual lines property will cause a <see cref="VisualLinesInvalidException"/>
	/// if this property is <c>false</c>.
	/// </summary>
	public bool VisualLinesValid
	{
		get { return visibleVisualLines != null; }
	}

	/// <summary>
	/// Occurs when the TextView is about to be measured and will regenerate its visual lines.
	/// This event may be used to mark visual lines as invalid that would otherwise be reused.
	/// </summary>
	public event EventHandler<VisualLineConstructionStartEventArgs>? VisualLineConstructionStarting;

	/// <summary>
	/// Occurs when the TextView was measured and changed its visual lines.
	/// </summary>
	public event EventHandler? VisualLinesChanged;

	/// <summary>
	/// If the visual lines are invalid, creates new visual lines for the visible part
	/// of the document.
	/// If all visual lines are valid, this method does nothing.
	/// </summary>
	/// <exception cref="InvalidOperationException">The visual line build process is already running.
	/// It is not allowed to call this method during the construction of a visual line.</exception>
	public void EnsureVisualLines()
	{
		VerifyAccess();
		if (inMeasure)
			throw new InvalidOperationException("The visual line build process is already running! Cannot EnsureVisualLines() during Measure!");
		if (!VisualLinesValid)
		{
			// invalidate measure and force immediate re-layout
			InvalidateMeasure();
			UpdateLayout();
		}
		// Sometimes we still have invalid lines after UpdateLayout - work around the problem
		// by calling MeasureOverride directly.
		if (!VisualLinesValid)
		{
			Debug.WriteLine("UpdateLayout() failed in EnsureVisualLines");
			MeasureOverride(lastAvailableSize);
		}
		if (!VisualLinesValid)
			throw new VisualLinesInvalidException("Internal error: visual lines invalid after EnsureVisualLines call");
	}
	#endregion

	#region Measure
	/// <summary>
	/// Additonal amount that allows horizontal scrolling past the end of the longest line.
	/// This is necessary to ensure the caret always is visible, even when it is at the end of the longest line.
	/// </summary>
	const double AdditionalHorizontalScrollAmount = 3;

	/// <summary>
	/// The minimum distance the caret keeps from the view border when it is brought into view.
	/// </summary>
	//was previously: Editing.Caret.MinimumDistanceToViewBorder; hosted here until the Editing wave
	//lands (the Caret port should consume this constant so the two stay in sync).
	internal const double MinimumDistanceToViewBorder = 30;

	Size lastAvailableSize;
	bool inMeasure;

	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
	{
		// We don't support infinite available width, so we'll limit it to 32000 pixels.
		if (availableSize.Width > 32000)
			availableSize.Width = 32000;

		//was previously: gated on !CanHorizontallyScroll; word wrap is the port's equivalent.
		if (WordWrap && !availableSize.Width.IsClose(lastAvailableSize.Width))
			ClearVisualLines();
		lastAvailableSize = availableSize;

		renderCanvas.Measure(availableSize);
		MeasureInlineObjects();

		double maxWidth;
		if (document == null)
		{
			// no document -> create empty list of lines
			allVisualLines = new List<VisualLine>();
			visibleVisualLines = allVisualLines.AsReadOnly();
			maxWidth = 0;
		}
		else
		{
			inMeasure = true;
			try
			{
				maxWidth = CreateAndMeasureVisualLines(availableSize);
			}
			finally
			{
				inMeasure = false;
			}
		}

		// remove inline objects only at the end, so that inline objects that were re-used are not removed from the editor
		RemoveInlineObjectsNow();

		maxWidth += AdditionalHorizontalScrollAmount;
		double heightTreeHeight = this.DocumentHeight;
		AdvancedTextEditOptions options = this.Options;
		if (options.AllowScrollBelowDocument)
		{
			if (!double.IsInfinity(scrollViewport.Height))
			{
				// HACK: we need to keep at least MinimumDistanceToViewBorder visible so that we don't scroll back up when the user types after
				// scrolling to the very bottom.
				double minVisibleDocumentHeight = Math.Max(DefaultLineHeight, MinimumDistanceToViewBorder);
				// scrollViewportBottom: bottom of scroll view port, but clamped so that at least minVisibleDocumentHeight of the document stays visible.
				double scrollViewportBottom = Math.Min(heightTreeHeight - minVisibleDocumentHeight, scrollOffsetY) + scrollViewport.Height;
				// increase the extend height to allow scrolling below the document
				heightTreeHeight = Math.Max(heightTreeHeight, scrollViewportBottom);
			}
		}

		SetScrollData(availableSize,
					  new Size(maxWidth, heightTreeHeight),
					  scrollOffsetX, scrollOffsetY);
		VisualLinesChanged?.Invoke(this, EventArgs.Empty);

		return new Size(Math.Min(availableSize.Width, maxWidth), Math.Min(availableSize.Height, heightTreeHeight));
	}

	/// <summary>
	/// Build all VisualLines in the visible range.
	/// </summary>
	/// <returns>Width the longest line</returns>
	double CreateAndMeasureVisualLines(Size availableSize)
	{
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		GlobalTextRunProperties globalTextRunProperties = CreateGlobalTextRunProperties();

		var firstLineInView = heightTree.GetLineByVisualPosition(scrollOffsetY);

		// number of pixels clipped from the first visual line(s)
		clippedPixelsOnTop = scrollOffsetY - heightTree.GetVisualPosition(firstLineInView);
		// clippedPixelsOnTop should be >= 0, except for floating point inaccuracy.
		Debug.Assert(clippedPixelsOnTop >= -ExtensionMethods.Epsilon);

		newVisualLines = new List<VisualLine>();

		VisualLineConstructionStarting?.Invoke(this, new VisualLineConstructionStartEventArgs(firstLineInView));

		var elementGeneratorsArray = elementGenerators.ToArray();
		var lineTransformersArray = lineTransformers.ToArray();
		DocumentLine? nextLine = firstLineInView;
		double maxWidth = 0;
		double yPos = -clippedPixelsOnTop;
		while (yPos < availableSize.Height && nextLine != null)
		{
			VisualLine? visualLine = GetVisualLine(nextLine.LineNumber);
			visualLine ??= BuildVisualLine(nextLine,
										   globalTextRunProperties,
										   elementGeneratorsArray, lineTransformersArray,
										   availableSize);

			visualLine.VisualTop = scrollOffsetY + yPos;

			nextLine = visualLine.LastDocumentLine.NextLine;

			yPos += visualLine.Height;

			foreach (TextLineLayout textLine in visualLine.TextLines)
			{
				if (textLine.Width > maxWidth)
					maxWidth = textLine.Width;
			}

			newVisualLines.Add(visualLine);
		}

		foreach (VisualLine line in allVisualLines)
		{
			Debug.Assert(line.IsDisposed == false);
			if (!newVisualLines.Contains(line))
				DisposeVisualLine(line);
		}

		allVisualLines = newVisualLines;
		// visibleVisualLines = readonly copy of visual lines
		visibleVisualLines = new ReadOnlyCollection<VisualLine>(newVisualLines.ToArray());
		newVisualLines = null;

		if (allVisualLines.Any(line => line.IsDisposed))
		{
			throw new InvalidOperationException("A visual line was disposed even though it is still in use.\n" +
												"This can happen when Redraw() is called during measure for lines " +
												"that are already constructed.");
		}
		return maxWidth;
	}
	#endregion

	#region BuildVisualLine
	GlobalTextRunProperties CreateGlobalTextRunProperties()
	{
		//was previously: built a WPF Typeface and read Control.Foreground from the inherited DPs;
		//the port copies the internal font properties the editor control pushes down.
		var p = new GlobalTextRunProperties();
		p.FontFamily = fontFamily;
		p.FontSize = fontSize;
		p.FontWeight = fontWeight;
		p.FontStyle = fontStyle;
		p.FontStretch = fontStretch;
		p.ForegroundBrush = foreground;
		p.CultureInfo = CultureInfo.CurrentCulture;
		return p;
	}

	VisualLine BuildVisualLine(DocumentLine documentLine,
							   GlobalTextRunProperties globalTextRunProperties,
							   VisualLineElementGenerator[] elementGeneratorsArray,
							   IVisualLineTransformer[] lineTransformersArray,
							   Size availableSize)
	{
		TextDocument document = this.document ?? throw ThrowUtil.NoDocumentAssigned();
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		if (heightTree.GetIsCollapsed(documentLine.LineNumber))
			throw new InvalidOperationException("Trying to build visual line from collapsed line");

		VisualLine visualLine = new VisualLine(this, documentLine);
		VisualLineTextSource textSource = new VisualLineTextSource(visualLine)
		{
			Document = document,
			GlobalTextRunProperties = globalTextRunProperties,
			TextView = this
		};

		visualLine.ConstructVisualElements(textSource, elementGeneratorsArray);

		if (visualLine.FirstDocumentLine != visualLine.LastDocumentLine)
		{
			// Check whether the lines are collapsed correctly:
			DocumentLine lineAfterFirst = visualLine.FirstDocumentLine.NextLine
				?? throw new InvalidOperationException("The visual line spans multiple document lines, but the first line has no successor.");
			double firstLinePos = heightTree.GetVisualPosition(lineAfterFirst);
			double lastLinePos = heightTree.GetVisualPosition(visualLine.LastDocumentLine.NextLine ?? visualLine.LastDocumentLine);
			if (!firstLinePos.IsClose(lastLinePos))
			{
				for (int i = visualLine.FirstDocumentLine.LineNumber + 1; i <= visualLine.LastDocumentLine.LineNumber; i++)
				{
					if (!heightTree.GetIsCollapsed(i))
						throw new InvalidOperationException("Line " + i + " was skipped by a VisualLineElementGenerator, but it is not collapsed.");
				}
				throw new InvalidOperationException("All lines collapsed but visual pos different - height tree inconsistency?");
			}
		}

		visualLine.RunTransformers(textSource, lineTransformersArray);

		//was previously: a loop of TextFormatter.FormatLine calls with per-row word-wrap
		//indentation (WordWrapIndentation/InheritWordWrapIndentation); the engine lays the whole
		//visual line out in ONE call and has no hanging indent, so those options have no effect
		//in this version and the wrap width is passed directly.
		float? wrapWidth = null;
		if (WordWrap && !double.IsInfinity(availableSize.Width))
			wrapWidth = (float)availableSize.Width;
		visualLine.Format(textSource, wrapWidth);
		heightTree.SetHeight(visualLine.FirstDocumentLine, visualLine.Height);
		return visualLine;
	}
	#endregion

	#region Arrange
	/// <summary>
	/// Arrange implementation.
	/// </summary>
	protected override Size ArrangeOverride(Size finalSize)
	{
		EnsureVisualLines();

		renderCanvas.Arrange(new Rect(new Point(0, 0), finalSize));

		if (document == null || allVisualLines.Count == 0)
		{
			renderCanvas.Invalidate();
			return finalSize;
		}

		// validate scroll position
		double newScrollOffsetX = scrollOffsetX;
		double newScrollOffsetY = scrollOffsetY;
		if (scrollOffsetX + finalSize.Width > scrollExtent.Width)
		{
			newScrollOffsetX = Math.Max(0, scrollExtent.Width - finalSize.Width);
		}
		if (scrollOffsetY + finalSize.Height > scrollExtent.Height)
		{
			newScrollOffsetY = Math.Max(0, scrollExtent.Height - finalSize.Height);
		}
		if (SetScrollData(scrollViewport, scrollExtent, newScrollOffsetX, newScrollOffsetY))
			InvalidateMeasure();

		if (visibleVisualLines != null)
		{
			//was previously: walked each row's WPF text-run spans to find inline objects; the port
			//positions each registered run from its owning visual line and element cells.
			foreach (InlineObjectRun inlineObject in inlineObjects)
			{
				VisualLine? ownerLine = inlineObject.VisualLine;
				if (ownerLine == null || ownerLine.IsDisposed || !visibleVisualLines.Contains(ownerLine))
				{
					// Pending removal or scrolled out of view: park the element off-screen
					// (every child must be arranged; upstream relied on clipping instead).
					inlineObject.Element.Arrange(new Rect(new Point(-100000, -100000), inlineObject.DesiredSize));
					continue;
				}
				Point pos = ownerLine.GetVisualPosition(inlineObject.VisualColumn, VisualYPosition.LineTop);
				inlineObject.Element.Arrange(new Rect(new Point(pos.X - scrollOffsetX, pos.Y - scrollOffsetY), inlineObject.DesiredSize));
			}
		}
		//was previously: InvalidateCursorIfMouseWithinTextView() re-evaluated the mouse cursor
		//shape; per-element cursor shaping is not part of this port.

		renderCanvas.Invalidate();
		return finalSize;
	}
	#endregion

	#region Render
	/// <summary>
	/// Gets the list of background renderers.
	/// </summary>
	public IList<IBackgroundRenderer> BackgroundRenderers
	{
		get { return backgroundRenderers; }
	}

	void BackgroundRenderer_Added(IBackgroundRenderer renderer)
	{
		ConnectToTextView(renderer);
		InvalidateLayer(renderer.Layer);
	}

	void BackgroundRenderer_Removed(IBackgroundRenderer renderer)
	{
		DisconnectFromTextView(renderer);
		InvalidateLayer(renderer.Layer);
	}

	/// <summary>
	/// Gets the display scale factor of the render surface (1.0 = 96 dpi). Used by
	/// <see cref="PixelSnapHelpers"/> to align drawing on whole device pixels.
	/// </summary>
	internal double RenderScale
	{
		get { return renderCanvas.Scale; }
	}

	//was previously: split across TextView.OnRender (background renderers + merged element
	//background geometry, with 3-pixel rounded corners), the Layer children's OnRender (per-layer
	//renderers) and TextLayer (the visual-line visuals). One pass paints everything in layer
	//order; element backgrounds are flat rectangles drawn by VisualLine.Draw.
	void RenderCanvasPaint(SKCanvas canvas, SKSize size)
	{
		var lines = visibleVisualLines;
		if (lines == null)
			return; // not measured yet (or lines invalidated); the pending measure will repaint

		canvas.ClipRect(SKRect.Create(0, 0, size.Width, size.Height));

		RenderBackground(canvas, KnownLayer.Background);
		RenderBackground(canvas, KnownLayer.Selection);
		RenderBackground(canvas, KnownLayer.Text);
		foreach (VisualLine visualLine in lines)
		{
			visualLine.Draw(canvas, new SKPoint(
				(float)-scrollOffsetX,
				(float)(visualLine.VisualTop - scrollOffsetY)));
		}
		RenderBackground(canvas, KnownLayer.Caret);
	}

	/// <summary>
	/// Draws all background renderers registered for the specified layer onto the canvas of the
	/// current paint pass.
	/// </summary>
	internal void RenderBackground(SKCanvas canvas, KnownLayer layer)
	{
		foreach (IBackgroundRenderer bg in backgroundRenderers)
		{
			if (bg.Layer == layer)
			{
				bg.Draw(this, canvas);
			}
		}
	}
	#endregion

	#region Scrolling
	//was previously: the IScrollInfo implementation (Line/Page/MouseWheel Up-Down-Left-Right,
	//ScrollOwner, CanVertically/HorizontallyScroll). The port owns offset, extent and viewport
	//directly; the editor template syncs explicit ScrollBar controls to this surface, and the
	//vertical-scrolling gate is gone (vertical scrolling is always allowed).

	/// <summary>
	/// Size of the document, in pixels.
	/// </summary>
	Size scrollExtent;

	/// <summary>
	/// Offset of the scroll position.
	/// </summary>
	double scrollOffsetX, scrollOffsetY;

	/// <summary>
	/// Size of the viewport.
	/// </summary>
	Size scrollViewport;

	void ClearScrollData()
	{
		SetScrollData(new Size(), new Size(), 0, 0);
	}

	bool SetScrollData(Size viewport, Size extent, double offsetX, double offsetY)
	{
		if (!(viewport.IsClose(this.scrollViewport)
			  && extent.IsClose(this.scrollExtent)
			  && offsetX.IsClose(this.scrollOffsetX)
			  && offsetY.IsClose(this.scrollOffsetY)))
		{
			this.scrollViewport = viewport;
			this.scrollExtent = extent;
			ApplyScrollOffset(offsetX, offsetY);
			return true;
		}
		return false;
	}

	void ApplyScrollOffset(double offsetX, double offsetY)
	{
		//was previously: SetScrollOffset(Vector), which zeroed each axis when the corresponding
		//IScrollInfo.Can*Scroll flag was false; here the horizontal axis is pinned by WordWrap
		//and the vertical axis is always scrollable.
		if (WordWrap)
			offsetX = 0;

		if (!scrollOffsetX.IsClose(offsetX) || !scrollOffsetY.IsClose(offsetY))
		{
			scrollOffsetX = offsetX;
			scrollOffsetY = offsetY;
			ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Gets the width of the scrollable document area (the width of the longest visible line,
	/// plus a small amount that keeps the caret reachable at the end of the longest line).
	/// </summary>
	public double ExtentWidth
	{
		get { return scrollExtent.Width; }
	}

	/// <summary>
	/// Gets the height of the scrollable document area. This is <see cref="DocumentHeight"/>,
	/// extended past the end when <see cref="AdvancedTextEditOptions.AllowScrollBelowDocument"/>
	/// is enabled.
	/// </summary>
	public double ExtentHeight
	{
		get { return scrollExtent.Height; }
	}

	/// <summary>
	/// Gets the width of the viewport (the width the view was last measured with).
	/// </summary>
	public double ViewportWidth
	{
		get { return scrollViewport.Width; }
	}

	/// <summary>
	/// Gets the height of the viewport (the height the view was last measured with).
	/// </summary>
	public double ViewportHeight
	{
		get { return scrollViewport.Height; }
	}

	/// <summary>
	/// Gets the horizontal scroll offset.
	/// </summary>
	public double HorizontalOffset
	{
		get { return scrollOffsetX; }
	}

	/// <summary>
	/// Gets the vertical scroll offset.
	/// </summary>
	public double VerticalOffset
	{
		get { return scrollOffsetY; }
	}

	/// <summary>
	/// Occurs when the scroll offset has changed.
	/// </summary>
	public event EventHandler? ScrollOffsetChanged;

	static double ValidateVisualOffset(double offset)
	{
		if (double.IsNaN(offset))
			throw new ArgumentException("offset must not be NaN");
		if (offset < 0)
			return 0;
		else
			return offset;
	}

	/// <summary>
	/// Sets the horizontal scroll offset. Negative values are clamped to 0; offsets past the
	/// extent are clamped on the next layout pass.
	/// </summary>
	public void SetHorizontalOffset(double offset)
	{
		offset = ValidateVisualOffset(offset);
		if (!scrollOffsetX.IsClose(offset))
		{
			ApplyScrollOffset(offset, scrollOffsetY);
			// Horizontal scrolling repositions inline objects and repaints; the visual lines stay valid.
			InvalidateArrange();
			renderCanvas.Invalidate();
		}
	}

	/// <summary>
	/// Sets the vertical scroll offset. Negative values are clamped to 0; offsets past the
	/// extent are clamped on the next layout pass.
	/// </summary>
	public void SetVerticalOffset(double offset)
	{
		offset = ValidateVisualOffset(offset);
		if (!scrollOffsetY.IsClose(offset))
		{
			ApplyScrollOffset(scrollOffsetX, offset);
			// Vertical scrolling changes the visible line range - re-measure.
			InvalidateMeasure();
		}
	}

	/// <summary>
	/// Sets both scroll offsets at once. Negative values are clamped to 0; offsets past the
	/// extent are clamped on the next layout pass.
	/// </summary>
	public void SetScrollOffset(double horizontalOffset, double verticalOffset)
	{
		horizontalOffset = ValidateVisualOffset(horizontalOffset);
		verticalOffset = ValidateVisualOffset(verticalOffset);
		bool horizontalChanged = !scrollOffsetX.IsClose(horizontalOffset);
		bool verticalChanged = !scrollOffsetY.IsClose(verticalOffset);
		if (horizontalChanged || verticalChanged)
		{
			ApplyScrollOffset(horizontalOffset, verticalOffset);
			if (verticalChanged)
			{
				InvalidateMeasure();
			}
			else
			{
				InvalidateArrange();
				renderCanvas.Invalidate();
			}
		}
	}

	/// <summary>
	/// Scrolls the text view so that the specified rectangle gets visible.
	/// </summary>
	/// <param name="rectangle">
	/// The rectangle, in device-independent pixels relative to the top left corner of the document.
	/// </param>
	public virtual void MakeVisible(Rect rectangle)
	{
		Rect visibleRectangle = new Rect(scrollOffsetX, scrollOffsetY,
										 scrollViewport.Width, scrollViewport.Height);
		double newScrollOffsetX = scrollOffsetX;
		double newScrollOffsetY = scrollOffsetY;
		if (rectangle.Left < visibleRectangle.Left)
		{
			if (rectangle.Right > visibleRectangle.Right)
			{
				newScrollOffsetX = rectangle.Left + rectangle.Width / 2;
			}
			else
			{
				newScrollOffsetX = rectangle.Left;
			}
		}
		else if (rectangle.Right > visibleRectangle.Right)
		{
			newScrollOffsetX = rectangle.Right - scrollViewport.Width;
		}
		if (rectangle.Top < visibleRectangle.Top)
		{
			if (rectangle.Bottom > visibleRectangle.Bottom)
			{
				newScrollOffsetY = rectangle.Top + rectangle.Height / 2;
			}
			else
			{
				newScrollOffsetY = rectangle.Top;
			}
		}
		else if (rectangle.Bottom > visibleRectangle.Bottom)
		{
			newScrollOffsetY = rectangle.Bottom - scrollViewport.Height;
		}
		newScrollOffsetX = ValidateVisualOffset(newScrollOffsetX);
		newScrollOffsetY = ValidateVisualOffset(newScrollOffsetY);
		if (!scrollOffsetX.IsClose(newScrollOffsetX) || !scrollOffsetY.IsClose(newScrollOffsetY))
		{
			ApplyScrollOffset(newScrollOffsetX, newScrollOffsetY);
			InvalidateMeasure();
		}
	}
	#endregion

	#region Default text metrics and font properties
	bool defaultTextMetricsValid;
	double wideSpaceWidth; // Width of an 'x'. Used as basis for the tab width, and for scrolling.
	double defaultLineHeight; // Height of a line containing 'x'. Used for scrolling.
	double defaultBaseline; // Baseline of a line containing 'x'. Used for TextTop/TextBottom calculation.

	string? fontFamily;
	double fontSize = 12.0;
	FontWeight fontWeight = FontWeights.Normal;
	FontStyle fontStyle = FontStyle.Normal;
	FontStretch fontStretch = FontStretch.Normal;
	Brush? foreground;

	//was previously: the font came from the inherited Control.FontFamily/FontSize/... dependency
	//properties and Control.Foreground; a Panel inherits none of these, so the text area/editor
	//control pushes its values into these internal properties.

	/// <summary>
	/// The font family name the text is rendered with, or null for the platform default family.
	/// Set by the owning text area/editor control.
	/// </summary>
	internal string? FontFamily
	{
		get { return fontFamily; }
		set
		{
			if (fontFamily != value)
			{
				fontFamily = value;
				OnFontChanged();
			}
		}
	}

	/// <summary>
	/// The em size, in device-independent pixels. Set by the owning text area/editor control.
	/// </summary>
	internal double FontSize
	{
		get { return fontSize; }
		set
		{
			if (fontSize != value)
			{
				fontSize = value;
				OnFontChanged();
			}
		}
	}

	/// <summary>
	/// The font weight. Set by the owning text area/editor control.
	/// </summary>
	internal FontWeight FontWeight
	{
		get { return fontWeight; }
		set
		{
			if (!fontWeight.Equals(value))
			{
				fontWeight = value;
				OnFontChanged();
			}
		}
	}

	/// <summary>
	/// The font style. Set by the owning text area/editor control.
	/// </summary>
	internal FontStyle FontStyle
	{
		get { return fontStyle; }
		set
		{
			if (fontStyle != value)
			{
				fontStyle = value;
				OnFontChanged();
			}
		}
	}

	/// <summary>
	/// The font stretch. Set by the owning text area/editor control.
	/// </summary>
	internal FontStretch FontStretch
	{
		get { return fontStretch; }
		set
		{
			if (fontStretch != value)
			{
				fontStretch = value;
				OnFontChanged();
			}
		}
	}

	/// <summary>
	/// The default text brush, or null to fall back to the drawing pass's default color.
	/// Set by the owning text area/editor control.
	/// </summary>
	internal Brush? Foreground
	{
		get { return foreground; }
		set
		{
			if (foreground != value)
			{
				foreground = value;
				Redraw();
			}
		}
	}

	void OnFontChanged()
	{
		//was previously: font property changes recreated the WPF text formatter and the cached
		//elements; the engine is static and there is no cache, so only the metrics and the
		//visual lines need refreshing.
		InvalidateDefaultTextMetrics();
		Redraw();
	}

	/// <summary>
	/// Gets the width of a 'wide space' (the space width used for calculating the tab size).
	/// </summary>
	/// <remarks>
	/// This is the width of an 'x' in the current font.
	/// We do not measure the width of an actual space as that would lead to tiny tabs in
	/// some proportional fonts.
	/// For monospaced fonts, this property will return the expected value, as 'x' and ' ' have the same width.
	/// </remarks>
	public double WideSpaceWidth
	{
		get
		{
			CalculateDefaultTextMetrics();
			return wideSpaceWidth;
		}
	}

	/// <summary>
	/// Gets the default line height. This is the height of an empty line or a line containing regular text.
	/// Lines that include formatted text or custom UI elements may have a different line height.
	/// </summary>
	public double DefaultLineHeight
	{
		get
		{
			CalculateDefaultTextMetrics();
			return defaultLineHeight;
		}
	}

	/// <summary>
	/// Gets the default baseline position. This is the difference between <see cref="VisualYPosition.TextTop"/>
	/// and <see cref="VisualYPosition.Baseline"/> for a line containing regular text.
	/// Lines that include formatted text or custom UI elements may have a different baseline.
	/// </summary>
	public double DefaultBaseline
	{
		get
		{
			CalculateDefaultTextMetrics();
			return defaultBaseline;
		}
	}

	void InvalidateDefaultTextMetrics()
	{
		defaultTextMetricsValid = false;
		if (heightTree != null)
		{
			// calculate immediately so that height tree gets updated
			CalculateDefaultTextMetrics();
		}
	}

	void CalculateDefaultTextMetrics()
	{
		if (defaultTextMetricsValid)
			return;
		defaultTextMetricsValid = true;
		//was previously: FormatLine("x") through the WPF formatter, with fallback constants when
		//no formatter existed (no document); the engine façade is always available, so the
		//fallback branch is gone.
		GlobalTextRunProperties global = CreateGlobalTextRunProperties();
		var run = new TextRunDescriptor(
			"x",
			global.FontFamily,
			(float)global.FontSize,
			VisualLineElementTextRunProperties.ToTextFontWeight(global.FontWeight),
			VisualLineElementTextRunProperties.ToTextFontStyle(global.FontStyle),
			VisualLineElementTextRunProperties.ToTextFontStretch(global.FontStretch));
		using (TextLayoutResult layout = TextLayoutEngine.Layout(new[] { run }))
		{
			wideSpaceWidth = Math.Max(1, layout.Size.Width);
			if (layout.LineCount > 0)
			{
				TextLineMetrics metrics = layout.GetLineMetrics(0);
				defaultBaseline = Math.Max(1, metrics.BaselineOffset);
				defaultLineHeight = Math.Max(1, metrics.Height);
			}
			else
			{
				defaultBaseline = Math.Max(1, fontSize);
				defaultLineHeight = Math.Max(1, layout.LineHeight);
			}
		}
		// Update heightTree.DefaultLineHeight, if a document is loaded.
		if (heightTree != null)
			heightTree.DefaultLineHeight = defaultLineHeight;
	}
	#endregion

	#region Visual element pointer handling
	//was previously: OnMouseDown/OnMouseUp overrides; Panel exposes no pointer override seam in
	//this framework, so the constructor subscribes the pointer events instead. OnQueryCursor
	//(per-element cursor shaping) has no counterpart and is dropped.
	void TextViewPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!e.Handled)
		{
			EnsureVisualLines();
			Point position = e.GetCurrentPoint(this).Position;
			VisualLineElement? element = GetVisualLineElementFromPosition(
				new Point(position.X + scrollOffsetX, position.Y + scrollOffsetY));
			element?.OnPointerPressed(e);
		}
	}

	void TextViewPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (!e.Handled)
		{
			EnsureVisualLines();
			Point position = e.GetCurrentPoint(this).Position;
			VisualLineElement? element = GetVisualLineElementFromPosition(
				new Point(position.X + scrollOffsetX, position.Y + scrollOffsetY));
			element?.OnPointerReleased(e);
		}
	}
	#endregion

	#region Getting elements from Visual Position
	/// <summary>
	/// Gets the visual line at the specified document position (relative to start of document).
	/// Returns null if there is no visual line for the position (e.g. the position is outside the visible
	/// text area).
	/// </summary>
	public VisualLine? GetVisualLineFromVisualTop(double visualTop)
	{
		// TODO: change this method to also work outside the visible range -
		// required to make GetPosition work as expected!
		EnsureVisualLines();
		foreach (VisualLine vl in this.VisualLines)
		{
			if (visualTop < vl.VisualTop)
				continue;
			if (visualTop < vl.VisualTop + vl.Height)
				return vl;
		}
		return null;
	}

	/// <summary>
	/// Gets the visual top position (relative to start of document) from a document line number.
	/// </summary>
	public double GetVisualTopByDocumentLine(int line)
	{
		VerifyAccess();
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		return heightTree.GetVisualPosition(heightTree.GetLineByNumber(line));
	}

	VisualLineElement? GetVisualLineElementFromPosition(Point visualPosition)
	{
		VisualLine? vl = GetVisualLineFromVisualTop(visualPosition.Y);
		if (vl != null)
		{
			int column = vl.GetVisualColumnFloor(visualPosition);
			foreach (VisualLineElement element in vl.Elements)
			{
				if (element.VisualColumn + element.VisualLength <= column)
					continue;
				return element;
			}
		}
		return null;
	}
	#endregion

	#region Visual Position <-> TextViewPosition
	/// <summary>
	/// Gets the visual position from a text view position.
	/// </summary>
	/// <param name="position">The text view position.</param>
	/// <param name="yPositionMode">The mode how to retrieve the Y position.</param>
	/// <returns>The position in device-independent pixels relative
	/// to the top left corner of the document.</returns>
	public Point GetVisualPosition(TextViewPosition position, VisualYPosition yPositionMode)
	{
		VerifyAccess();
		TextDocument document = this.document ?? throw ThrowUtil.NoDocumentAssigned();
		DocumentLine documentLine = document.GetLineByNumber(position.Line);
		VisualLine visualLine = GetOrConstructVisualLine(documentLine);
		int visualColumn = position.VisualColumn;
		if (visualColumn < 0)
		{
			int offset = documentLine.Offset + position.Column - 1;
			visualColumn = visualLine.GetVisualColumn(offset - visualLine.FirstDocumentLine.Offset);
		}
		return visualLine.GetVisualPosition(visualColumn, position.IsAtEndOfLine, yPositionMode);
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is within a character, it is rounded to the next character boundary.
	/// </summary>
	/// <param name="visualPosition">The position in device-independent pixels relative
	/// to the top left corner of the document.</param>
	/// <returns>The logical position, or null if the position is outside the document.</returns>
	public TextViewPosition? GetPosition(Point visualPosition)
	{
		VerifyAccess();
		if (this.document == null)
			throw ThrowUtil.NoDocumentAssigned();
		VisualLine? line = GetVisualLineFromVisualTop(visualPosition.Y);
		if (line == null)
			return null;
		return line.GetTextViewPosition(visualPosition, Options.EnableVirtualSpace);
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is inside a character, the position in front of the character is returned.
	/// </summary>
	/// <param name="visualPosition">The position in device-independent pixels relative
	/// to the top left corner of the document.</param>
	/// <returns>The logical position, or null if the position is outside the document.</returns>
	public TextViewPosition? GetPositionFloor(Point visualPosition)
	{
		VerifyAccess();
		if (this.document == null)
			throw ThrowUtil.NoDocumentAssigned();
		VisualLine? line = GetVisualLineFromVisualTop(visualPosition.Y);
		if (line == null)
			return null;
		return line.GetTextViewPositionFloor(visualPosition, Options.EnableVirtualSpace);
	}
	#endregion

	#region Service Provider
	readonly ServiceContainer services = new ServiceContainer();

	/// <summary>
	/// Gets a service container used to associate services with the text view.
	/// </summary>
	/// <remarks>
	/// This container does not provide document services -
	/// use <c>TextView.GetService()</c> instead of <c>TextView.Services.GetService()</c> to ensure
	/// that document services can be found as well.
	/// </remarks>
	public ServiceContainer Services
	{
		get { return services; }
	}

	/// <summary>
	/// Retrieves a service from the text view.
	/// If the service is not found in the <see cref="Services"/> container,
	/// this method will also look for it in the current document's service provider.
	/// </summary>
	public virtual object? GetService(Type serviceType)
	{
		object? instance = services.GetService(serviceType);
		if (instance == null && document != null)
		{
			instance = document.ServiceProvider.GetService(serviceType);
		}
		return instance;
	}

	void ConnectToTextView(object obj)
	{
		ITextViewConnect? c = obj as ITextViewConnect;
		if (c != null)
			c.AddToTextView(this);
	}

	void DisconnectFromTextView(object obj)
	{
		ITextViewConnect? c = obj as ITextViewConnect;
		if (c != null)
			c.RemoveFromTextView(this);
	}
	#endregion

	#region MouseHover
	//was previously: four RoutedEvents (tunneling Preview* + bubbling pairs) re-raised from the
	//hover logic with Handled propagation; the port raises plain .NET events in the same order.

	/// <summary>
	/// Occurs when the mouse has hovered over a fixed location for some time.
	/// Raised immediately before <see cref="MouseHover"/>.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? PreviewMouseHover;

	/// <summary>
	/// Occurs when the mouse has hovered over a fixed location for some time.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHover;

	/// <summary>
	/// Occurs when the mouse had previously hovered but now started moving again.
	/// Raised immediately before <see cref="MouseHoverStopped"/>.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? PreviewMouseHoverStopped;

	/// <summary>
	/// Occurs when the mouse had previously hovered but now started moving again.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHoverStopped;
	#endregion

	#region Height tree / collapsing
	/// <summary>
	/// Collapses lines for the purpose of scrolling. <see cref="DocumentLine"/>s marked as collapsed will be hidden
	/// and not used to start the generation of a <see cref="VisualLine"/>.
	/// </summary>
	/// <remarks>
	/// This method is meant for <see cref="VisualLineElementGenerator"/>s that cause <see cref="VisualLine"/>s to span
	/// multiple <see cref="DocumentLine"/>s. Do not call it without providing a corresponding
	/// <see cref="VisualLineElementGenerator"/>.
	///
	/// Note that if you want a VisualLineElement to span from line N to line M, then you need to collapse only the lines
	/// N+1 to M. Do not collapse line N itself.
	///
	/// When you no longer need the section to be collapsed, call <see cref="CollapsedLineSection.Uncollapse()"/> on the
	/// <see cref="CollapsedLineSection"/> returned from this method.
	/// </remarks>
	public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end)
	{
		VerifyAccess();
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		return heightTree.CollapseText(start, end);
	}

	/// <summary>
	/// Gets the height of the document.
	/// </summary>
	public double DocumentHeight
	{
		get
		{
			// return 0 if there is no document = no heightTree
			return heightTree != null ? heightTree.TotalHeight : 0;
		}
	}

	/// <summary>
	/// Gets the document line at the specified visual position.
	/// </summary>
	public DocumentLine GetDocumentLineByVisualTop(double visualTop)
	{
		VerifyAccess();
		HeightTree heightTree = this.heightTree ?? throw ThrowUtil.NoDocumentAssigned();
		return heightTree.GetLineByVisualPosition(visualTop);
	}
	#endregion

	#region Column ruler / current line highlight properties
	/// <summary>
	/// The brush used to draw the column ruler.
	/// <seealso cref="AdvancedTextEditOptions.ShowColumnRuler"/>
	/// </summary>
	//was previously: the ColumnRulerPen dependency property (a frozen WPF Pen); re-expressed as a
	//brush + thickness pair per the port's drawing rules.
	public static readonly DependencyProperty ColumnRulerBrushProperty =
		DependencyProperty.Register(nameof(ColumnRulerBrush), typeof(Brush), typeof(TextView),
									new PropertyMetadata(CreateSolidBrush(255, 211, 211, 211), OnColumnRulerChanged));

	/// <summary>
	/// The thickness used to draw the column ruler.
	/// <seealso cref="AdvancedTextEditOptions.ShowColumnRuler"/>
	/// </summary>
	public static readonly DependencyProperty ColumnRulerThicknessProperty =
		DependencyProperty.Register(nameof(ColumnRulerThickness), typeof(double), typeof(TextView),
									new PropertyMetadata(1.0, OnColumnRulerChanged));

	/// <summary>
	/// Gets/Sets the brush used to draw the column ruler.
	/// <seealso cref="AdvancedTextEditOptions.ShowColumnRuler"/>
	/// </summary>
	public Brush? ColumnRulerBrush
	{
		get { return (Brush?)GetValue(ColumnRulerBrushProperty); }
		set { SetValue(ColumnRulerBrushProperty, value); }
	}

	/// <summary>
	/// Gets/Sets the thickness used to draw the column ruler.
	/// <seealso cref="AdvancedTextEditOptions.ShowColumnRuler"/>
	/// </summary>
	public double ColumnRulerThickness
	{
		get { return (double)GetValue(ColumnRulerThicknessProperty); }
		set { SetValue(ColumnRulerThicknessProperty, value); }
	}

	static void OnColumnRulerChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		TextView textView = (TextView)dp;
		textView.columnRulerRenderer.SetRuler(textView.Options.ColumnRulerPosition, textView.ColumnRulerBrush, textView.ColumnRulerThickness);
	}

	/// <summary>
	/// The <see cref="CurrentLineBackground"/> property.
	/// </summary>
	public static readonly DependencyProperty CurrentLineBackgroundProperty =
		DependencyProperty.Register(nameof(CurrentLineBackground), typeof(Brush), typeof(TextView),
									new PropertyMetadata(null, OnCurrentLineBackgroundChanged));

	/// <summary>
	/// Gets/Sets the background brush used by current line highlighter.
	/// </summary>
	public Brush? CurrentLineBackground
	{
		get { return (Brush?)GetValue(CurrentLineBackgroundProperty); }
		set { SetValue(CurrentLineBackgroundProperty, value); }
	}

	static void OnCurrentLineBackgroundChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		TextView textView = (TextView)dp;
		textView.currentLineHighlightRenderer.BackgroundBrush = textView.CurrentLineBackground;
	}

	/// <summary>
	/// The <see cref="CurrentLineBorderBrush"/> property.
	/// </summary>
	//was previously: the CurrentLineBorder dependency property (a WPF Pen); re-expressed as a
	//brush + thickness pair per the port's drawing rules.
	public static readonly DependencyProperty CurrentLineBorderBrushProperty =
		DependencyProperty.Register(nameof(CurrentLineBorderBrush), typeof(Brush), typeof(TextView),
									new PropertyMetadata(null, OnCurrentLineBorderChanged));

	/// <summary>
	/// The <see cref="CurrentLineBorderThickness"/> property.
	/// </summary>
	public static readonly DependencyProperty CurrentLineBorderThicknessProperty =
		DependencyProperty.Register(nameof(CurrentLineBorderThickness), typeof(double), typeof(TextView),
									new PropertyMetadata(1.0, OnCurrentLineBorderChanged));

	/// <summary>
	/// Gets/Sets the brush used for the border around the current line.
	/// </summary>
	public Brush? CurrentLineBorderBrush
	{
		get { return (Brush?)GetValue(CurrentLineBorderBrushProperty); }
		set { SetValue(CurrentLineBorderBrushProperty, value); }
	}

	/// <summary>
	/// Gets/Sets the thickness of the border around the current line.
	/// </summary>
	public double CurrentLineBorderThickness
	{
		get { return (double)GetValue(CurrentLineBorderThicknessProperty); }
		set { SetValue(CurrentLineBorderThicknessProperty, value); }
	}

	static void OnCurrentLineBorderChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		TextView textView = (TextView)dp;
		textView.currentLineHighlightRenderer.BorderBrush = textView.CurrentLineBorderBrush;
		textView.currentLineHighlightRenderer.BorderThickness = textView.CurrentLineBorderThickness;
	}

	/// <summary>
	/// Gets/Sets highlighted line number.
	/// </summary>
	public int HighlightedLine
	{
		get { return this.currentLineHighlightRenderer.Line; }
		set { this.currentLineHighlightRenderer.Line = value; }
	}
	#endregion

	/// <summary>
	/// Empty line selection width.
	/// </summary>
	public virtual double EmptyLineSelectionWidth
	{
		get { return 1; }
	}
}
