================================================================================
AGENT-README: CodeBrix.Platform.AdvancedTextEdit
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
A full code/text editor control (AdvancedTextEdit : Control) for every
CodeBrix.Platform head - Windows (Win32 and Skia-on-WPF), Linux (X11, Wayland,
FrameBuffer) and macOS. It has the editing model of a professional code editor:

  - a rope-backed document (TextDocument) with text anchors, line objects and
    grouped undo/redo;
  - XSHD-driven syntax highlighting with 21 built-in definitions (C#, C++, XML,
    HTML, JavaScript, JSON, Python, ...);
  - code folding with pluggable strategies (XML folding ships in the box);
  - a code-completion popup and insight (tool-tip style) popups;
  - an in-editor search panel (Ctrl+F / F3 / Shift+F3 / Escape);
  - text snippets with interactive replaceable fields;
  - smart indentation (default copy-previous-line; C# brace-aware);
  - line numbers, word wrap, current-line highlight, column ruler, visible
    whitespace, hyperlinks, and rectangular (Alt) selection.

Rendering is virtualized and driven by the family's single text engine (the
CodeBrix.Platform.TextLayout.ApacheLicenseForever package), so it stays
responsive on very large documents and matches TextBlock shaping exactly.
Target: .NET 10 or later.

PROVENANCE: a port of the AvalonEdit editor component (icsharpcode/AvalonEdit,
MIT) from WPF to this framework. The upstream namespaces
ICSharpCode.AvalonEdit[.Document|.Editing|.Rendering|.Highlighting|...] map
1:1 onto CodeBrix.Platform.UI.AdvancedTextEdit[.Document|.Editing|...]; three
types were renamed: TextEditor -> AdvancedTextEdit, TextEditorOptions ->
AdvancedTextEditOptions, AvalonEditCommands -> AdvancedTextEditCommands. WPF
command routing became the editor's own EditorCommand/KeyBinding system, the
completion "windows" became in-app popups, and rendering targets SkiaSharp
(IBackgroundRenderer draws on an SKCanvas). Do NOT use upstream namespaces or
write upstream signatures from memory - they differ in the places listed below.

INSTALLATION
============
PackageId:  CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever

    dotnet add package CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever

Reference it (without a version attribute) from the shared .Core project of a
CodeBrix.Platform application - the same project that references
CodeBrix.Platform.ApacheLicenseForever - so every head picks it up.

NuGet dependencies (flow in automatically):
  CodeBrix.Platform.ApacheLicenseForever            the core UI framework
  CodeBrix.Platform.TextLayout.ApacheLicenseForever  the shared text engine
  SkiaSharp                                          drawing types on the surface

License: Apache-2.0 (the package). The ported AvalonEdit sources are MIT; the
notice ships in the package's THIRD-PARTY-NOTICES.txt.
Requirements: none beyond the framework - pure managed code, no native
libraries; works on all six heads including the FrameBuffer head.

KEY NAMESPACES / USINGS
=======================
XAML (the form used by the working demo; the "using:" form used elsewhere in
the family also resolves):

    xmlns:advtxt="clr-namespace:CodeBrix.Platform.UI.AdvancedTextEdit;assembly=CodeBrix.Platform.UI.AdvancedTextEdit"
    xmlns:advtxt="using:CodeBrix.Platform.UI.AdvancedTextEdit"

Code:

    using CodeBrix.Platform.UI.AdvancedTextEdit;
        // AdvancedTextEdit, AdvancedTextEditOptions, AdvancedTextEditCommands,
        // TextViewPosition
    using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
        // TextDocument, DocumentLine, TextAnchor, ISegment, TextLocation,
        // UndoStack, TextSegment, TextSegmentCollection<T>
    using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
        // TextArea, Caret, Selection, RectangleSelection, EditorCommands,
        // KeyBinding, EditorCommandBinding
    using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
        // TextView, IBackgroundRenderer, IVisualLineTransformer,
        // DocumentColorizingTransformer, KnownLayer
    using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
        // HighlightingManager, IHighlightingDefinition, HighlightingColor,
        // DocumentHighlighter
    using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting.Xshd;
        // HighlightingLoader
    using CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;
        // CompletionWindow, ICompletionData, InsightWindow,
        // OverloadInsightWindow, IOverloadProvider
    using CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;
        // Snippet, SnippetTextElement, SnippetReplaceableTextElement, ...
    using CodeBrix.Platform.UI.AdvancedTextEdit.Folding;
        // FoldingManager, FoldingSection, NewFolding, XmlFoldingStrategy
    using CodeBrix.Platform.UI.AdvancedTextEdit.Search;
        // SearchPanel, SearchCommands, ISearchStrategy, SearchStrategyFactory
    using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation;
        // IIndentationStrategy, DefaultIndentationStrategy
    using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation.CSharp;
        // CSharpIndentationStrategy
    using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
        // Rope<T>, FileReader, ImmutableStack<T>, StringSegment

ARCHITECTURE AT A GLANCE
========================
Four layers, each reachable from the one above:

    AdvancedTextEdit  (Control)   the XAML control: scroll bars, Load/Save,
        |                         Text/Document/Options, SyntaxHighlighting
        +-- TextArea  (Control)   editing: Caret, Selection, input handlers,
              |                   command/key bindings, LeftMargins, TextEntered
              +-- TextView (Panel) rendering: visual lines, LineTransformers,
                    |             BackgroundRenderers, ElementGenerators, scroll
                    +-- TextDocument   the model: rope text, lines, anchors,
                                       UndoStack, change events

All three UI classes implement ITextEditorComponent (Document, DocumentChanged,
Options, OptionChanged, plus IServiceProvider). Feature installers (folding,
search) take the TextArea: Editor.TextArea.

CORE API REFERENCE: THE AdvancedTextEdit CONTROL
================================================
Class: AdvancedTextEdit : Control, ITextEditorComponent  (namespace root)
A new instance creates its own TextDocument and TextArea; defaults are
FontFamily "monospace", FontSize 13, white Background, black Foreground.

Properties (dependency properties unless noted)
    TextDocument Document                    the document shown (never null after
                                             construction; assign to swap files)
    string Text                              get/set whole text (plain property).
                                             The SETTER moves the caret to offset 0
                                             and CLEARS the undo stack.
    AdvancedTextEditOptions Options          editor options (see OPTIONS)
    TextArea TextArea                        the editing layer (plain, read-only)
    IHighlightingDefinition? SyntaxHighlighting  null = no coloring
    bool WordWrap                            default false; true hides the
                                             horizontal scroll bar
    bool IsReadOnly                          default false; setting it REPLACES
                                             TextArea.ReadOnlySectionProvider
    bool IsModified                          follows UndoStack.IsOriginalFile;
                                             cleared by Load/Save
    bool ShowLineNumbers                     default false
    Brush? LineNumbersForeground             default gray (128,128,128); also
                                             colors the dotted separator line
    Encoding? Encoding                       set by Load (auto-detected), used by
                                             Save; null = UTF-8 default
    ScrollBarVisibility HorizontalScrollBarVisibility / VerticalScrollBarVisibility
    string SelectedText                      get/set (plain)
    int CaretOffset                          get/set (plain)
    int SelectionStart / int SelectionLength get/set (plain)
    int LineCount                            (1 when no document)
    bool CanUndo / bool CanRedo
    double ExtentWidth/ExtentHeight, ViewportWidth/ViewportHeight,
           HorizontalOffset/VerticalOffset   scroll geometry (read-only)

Methods
    void Load(string fileName)      void Load(Stream stream)
        Reads the text, auto-detects the encoding (BOM-aware via
        Utils.FileReader.OpenStream), sets Encoding, sets IsModified=false.
    void Save(string fileName)      void Save(Stream stream)
        Writes with Encoding (or the StreamWriter default), flushes, does NOT
        close the stream, sets IsModified=false.
    void AppendText(string textData)
    void Clear()                                  (= Text = "")
    void Select(int start, int length)            throws ArgumentOutOfRange
    void SelectAll()  void Copy()  void Cut()  void Paste()  void Delete()
    bool Undo()       bool Redo()                 (false when nothing to do)
    void BeginChange()  void EndChange()          group document changes
    IDisposable DeclareChangeBlock()              using-friendly group
    void ScrollToLine(int line)
    void ScrollTo(int line, int column)
    void ScrollTo(int line, int column, VisualYPosition yPositionMode,
                  double referencedVerticalViewPortOffset, double minimumScrollFraction)
        All ScrollTo* calls REQUIRE that layout has run (the editor has a size).
    void ScrollToHome() / ScrollToEnd() / ScrollToHorizontalOffset(double) /
         ScrollToVerticalOffset(double)
    void LineUp/LineDown/LineLeft/LineRight/PageUp/PageDown/PageLeft/PageRight()
    TextViewPosition? GetPositionFromPoint(Point point)   point relative to the
        editor's top-left; null when outside the document
    new bool Focus(FocusState value)              forwards focus to the TextArea

Events
    event EventHandler? DocumentChanged          Document PROPERTY swapped
    event EventHandler? TextChanged              content changed
    event PropertyChangedEventHandler? OptionChanged
    event EventHandler<PointerRoutedEventArgs>? PreviewMouseHover, MouseHover,
          PreviewMouseHoverStopped, MouseHoverStopped   (hover = pointer rested)

TextViewPosition (struct, namespace root): Line, Column (1-based), VisualColumn,
IsAtEndOfLine, Location (TextLocation); ctors (line, column[, visualColumn]) and
(TextLocation[, visualColumn]).

DOCUMENT MODEL (namespace .Document)
====================================
TextDocument : IDocument, INotifyPropertyChanged  (sealed)
    ctors: TextDocument(), TextDocument(IEnumerable<char>), TextDocument(ITextSource)
    string Text { get; set; }        int TextLength        int LineCount  (O(1))
    string GetText(int offset, int length)    string GetText(ISegment segment)
    char GetCharAt(int offset)
    int IndexOf(char c, int startIndex, int count)
    int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
    int LastIndexOf(...)  int IndexOfAny(char[] anyOf, int startIndex, int count)
    void Insert(int offset, string text)
    void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
    void Remove(int offset, int length)       void Remove(ISegment segment)
    void Replace(int offset, int length, string text)
    void Replace(ISegment segment, string text)
    void Replace(int offset, int length, string text,
                 OffsetChangeMappingType offsetChangeMappingType)
        (ITextSource overloads of Insert/Replace also exist)
    IList<DocumentLine> Lines
    DocumentLine GetLineByNumber(int number)      DocumentLine GetLineByOffset(int offset)
    int GetOffset(TextLocation location)          int GetOffset(int line, int column)
    TextLocation GetLocation(int offset)
    TextAnchor CreateAnchor(int offset)
    UndoStack UndoStack { get; set; }             (settable: share one stack)
    IList<ILineTracker> LineTrackers
    void BeginUpdate()  void EndUpdate()  IDisposable RunUpdate()  bool IsInUpdate
    ITextSource CreateSnapshot()  ITextSource CreateSnapshot(int offset, int length)
    TextReader CreateReader()     void WriteTextTo(TextWriter writer)
    ITextSourceVersion Version    string? FileName (+ FileNameChanged)
    IServiceProvider ServiceProvider
    events: Changing, Changed (DocumentChangeEventArgs), TextChanged,
            UpdateStarted, UpdateFinished, PropertyChanged
    void VerifyAccess()   void SetOwnerThread(Thread? newOwner)

OFFSETS vs LOCATIONS: every API takes 0-based character OFFSETS (a position
between two characters, 0 <= offset <= TextLength). TextLocation(line, column)
is 1-based on both axes; TextLocation.Empty is (0,0). Convert with
GetOffset/GetLocation. ISegment is the (Offset, Length, EndOffset) triple with
EndOffset = Offset + Length; ISegmentExtensions.Contains(...) helps.

THREAD OWNERSHIP: a TextDocument is owned by the thread that created it and
throws InvalidOperationException from any other thread. To load on a worker:
create the document there, then SetOwnerThread(null) on the worker and
SetOwnerThread(Thread.CurrentThread) on the UI thread before assigning it to
Editor.Document.

BeginUpdate/EndUpdate (nestable; RunUpdate() returns a disposable): suspend
change events until the outermost EndUpdate and make the UndoStack record ONE
undo group for everything inside. Editor.BeginChange/EndChange/
DeclareChangeBlock are the same calls on the current document.

DocumentLine (sealed): int LineNumber (1-based, O(log n)), int Offset,
int EndOffset (before the delimiter), int Length (excludes the delimiter),
int TotalLength (includes it), int DelimiterLength (0/1/2), bool IsDeleted,
DocumentLine? NextLine, DocumentLine? PreviousLine. Line objects stay valid
across edits; after deletion IsDeleted is true and LineNumber/Offset throw.

TextAnchor : ITextAnchor (sealed; create via document.CreateAnchor(offset)):
int Offset, int Line, int Column, TextLocation Location,
AnchorMovementType MovementType (Default | BeforeInsertion | AfterInsertion),
bool SurviveDeletion (default false: deleting the text around the anchor
deletes the anchor; then Offset THROWS and IsDeleted is true), event Deleted.
Anchors are held by WEAK reference - keep your own reference or they are
collected. AnchorSegment(TextAnchor start, TextAnchor end) /
AnchorSegment(TextDocument, int offset, int length) is an ISegment made of two
anchors.

TextSegment : ISegment (StartOffset, EndOffset, Length; all settable) +
TextSegmentCollection<T> where T : TextSegment: a red/black tree that keeps
segments updated as the document changes (ctor TextSegmentCollection(TextDocument)
subscribes automatically) with FindSegmentsContaining(int offset),
FindOverlappingSegments(int offset, int length), FindFirstSegmentWithStartAfter(int),
GetNextSegment(T)/GetPreviousSegment(T), FirstSegment/LastSegment. Segments
shrink to length 0 when their text is deleted but are never removed.

UndoStack (sealed): bool CanUndo/CanRedo, void Undo()/Redo(), int SizeLimit
(top-level groups; default unlimited), bool IsOriginalFile,
void MarkAsOriginalFile(), void DiscardOriginalFileMarker(),
void StartUndoGroup([object? groupDescriptor]), void EndUndoGroup(),
void StartContinuedUndoGroup(object? groupDescriptor = null) (joins the
previous group), object? LastGroupDescriptor, void ClearAll(),
void ClearRedoStack(), void Push(IUndoableOperation) / PushOptional(...),
bool AcceptChanges (false while an undo runs), PropertyChanged.
Grouping rule: one Insert/Remove/Replace = one undo step; wrap several in
RunUpdate() (or StartUndoGroup/EndUndoGroup) to make them one step.

TextUtilities (static): GetWhitespaceAfter/Before(ITextSource, int offset),
GetLeadingWhitespace/GetTrailingWhitespace(TextDocument, DocumentLine),
GetNextCaretPosition(ITextSource, int offset, LogicalDirection, CaretPositioningMode),
GetCharacterClass(char), GetControlCharacterName(char).
DocumentTextWriter(IDocument document, int insertionOffset) : TextWriter writes
into the document. OffsetChangeMap / OffsetChangeMapEntry describe how offsets
moved in a change (DocumentChangeEventArgs.OffsetChangeMap, GetNewOffset(...)).

TEXT AREA, CARET AND SELECTION (namespace .Editing)
===================================================
TextArea : Control, ITextEditorComponent
    TextView TextView                 Caret Caret               Selection Selection {get;set;}
    TextDocument Document             AdvancedTextEditOptions Options
    event EventHandler? SelectionChanged
    event EventHandler<TextInputEventArgs>? TextEntering   (before insert;
          set e.Handled = true to VETO the typed text)
    event EventHandler<TextInputEventArgs>? TextEntered    (after insert)
    void PerformTextInput(string text)           programmatic typing
    void ClearSelection()
    IDisposable AllowCaretOutsideSelection()
    ObservableCollection<UIElement> LeftMargins  line-number / folding margins
    IReadOnlySectionProvider ReadOnlySectionProvider   (e.g. a
          TextSegmentReadOnlySectionProvider<T> for partial read-only regions)
    IIndentationStrategy? IndentationStrategy    default DefaultIndentationStrategy
    bool OverstrikeMode
    TextAreaDefaultInputHandler DefaultInputHandler   (.CaretNavigation,
          .Editing, .MouseSelection sub-handlers)
    ITextAreaInputHandler? ActiveInputHandler    (+ ActiveInputHandlerChanged)
    ICollection<EditorCommandBinding> CommandBindings
    ICollection<KeyBinding> InputBindings
    void PushStackedInputHandler(TextAreaStackedInputHandler) / PopStackedInputHandler(...)
    Brush? SelectionBrush, SelectionForeground, SelectionBorderBrush;
    double SelectionBorderThickness, SelectionCornerRadius
    MouseSelectionMode MouseSelectionMode
    events DataObjectCopying, DataObjectSettingData, DataObjectPasting
           (CancelCommand() to veto), TextCopied (TextEventArgs.Text)
    TextInputEventArgs: string Text; bool Handled

Caret (sealed; Editor.TextArea.Caret)
    int Offset {get;set;}             TextLocation Location {get;set;}  (cheap)
    TextViewPosition Position {get;set;}   (validates the visual column - costs)
    int Line, int Column, int VisualColumn, bool IsInVirtualSpace
    double DesiredXPos                Brush? CaretBrush (null = Foreground)
    event EventHandler? PositionChanged
    void BringCaretToView()           void Show() / Hide()
    Rect CalculateCaretRectangle()    (document coordinates, DIPs)

Selection (abstract)
    static Selection Create(TextArea textArea, int startOffset, int endOffset)
    static Selection Create(TextArea textArea, ISegment segment)
    bool IsEmpty, bool IsMultiline, int Length
    TextViewPosition StartPosition / EndPosition
    ISegment? SurroundingSegment      (null when empty)
    IEnumerable<SelectionSegment> Segments   (one per line for box selection)
    string GetText()                  void ReplaceSelectionWithText(string newText)
    bool Contains(int offset)         string CreateHtmlFragment(HtmlOptions options)
    Selection SetEndpoint(TextViewPosition) / StartSelectionOrSetEndpoint(start, end)
    Selections are immutable values: ASSIGN the result to TextArea.Selection.

RectangleSelection : Selection (sealed)
    ctor RectangleSelection(TextArea textArea, TextViewPosition start, TextViewPosition end)
    static bool PerformRectangularPaste(TextArea textArea, TextViewPosition startPosition,
                                        string text, bool selectInsertedText)
    static EditorCommand BoxSelectLeftByCharacter / BoxSelectRightByCharacter /
           BoxSelectLeftByWord / BoxSelectRightByWord / BoxSelectUpByLine /
           BoxSelectDownByLine / BoxSelectToLineStart / BoxSelectToLineEnd
    User gestures: Alt+drag with the mouse, Alt+Shift+Arrow/Home/End (and
    Ctrl+Alt+Shift+Left/Right by word) - gated by Options.EnableRectangularSelection.
    Mouse: click+drag = normal, double-click+drag = whole words, triple-click =
    whole lines; MouseSelectionMode reports the current mode.

    // caret and selection from code
    Editor.TextArea.Caret.Offset = 42;
    Editor.TextArea.Selection = Selection.Create(Editor.TextArea, 10, 20);
    Editor.TextArea.SelectionChanged += (_, _) =>
        status.Text = Editor.TextArea.Selection.IsEmpty ? "" : Editor.SelectedText;
    Editor.TextArea.Caret.PositionChanged += (_, _) =>
        pos.Text = $"Ln {Editor.TextArea.Caret.Line}, Col {Editor.TextArea.Caret.Column}";

SYNTAX HIGHLIGHTING (namespaces .Highlighting, .Highlighting.Xshd)
==================================================================
HighlightingManager : IHighlightingDefinitionReferenceResolver
    static HighlightingManager Instance           process-wide, pre-loaded
    IHighlightingDefinition? GetDefinition(string name)
    IHighlightingDefinition? GetDefinitionByExtension(string extension)  (".cs";
          case-insensitive; null when unknown)
    ReadOnlyCollection<IHighlightingDefinition> HighlightingDefinitions (a copy)
    void RegisterHighlighting(string? name, string[]? extensions,
                              IHighlightingDefinition highlighting)
    void RegisterHighlighting(string? name, string[]? extensions,
                              Func<IHighlightingDefinition> lazyLoadedHighlighting)
    Registering an existing name REPLACES it; extensions are added/overwritten.

BUILT-IN DEFINITIONS (name -> extensions), all registered on Instance:
    "XmlDoc"      (none; referenced by other definitions)
    "C#"          .cs
    "JavaScript"  .js
    "HTML"        .htm .html
    "ASP/XHTML"   .asp .aspx .asax .asmx .ascx .master
    "Boo"         .boo
    "Coco"        .atg
    "CSS"         .css
    "C++"         .c .h .cc .cpp .hpp
    "Java"        .java
    "Patch"       .patch .diff
    "PowerShell"  .ps1 .psm1 .psd1
    "PHP"         .php
    "Python"      .py .pyw
    "TeX"         .tex
    "TSQL"        .sql
    "VB"          .vb
    "XML"         .xml .xsl .xslt .xsd .manifest .config .addin .xshd .wxs
                  .wxi .wxl .proj .csproj .vbproj .ilproj ... (plus more
                  MSBuild/XAML-style extensions)
    "MarkDown"    .md
    "MarkDownWithFontSize"  .md  (registered after MarkDown, so
                  GetDefinitionByExtension(".md") returns THIS one)
    "Json"        .json
Definitions are lazily parsed on first use. The built-in color schemes assume a
LIGHT surface (see PITFALLS).

IHighlightingDefinition: string? Name, HighlightingRuleSet MainRuleSet,
HighlightingRuleSet? GetNamedRuleSet(string), HighlightingColor? GetNamedColor(string),
IEnumerable<HighlightingColor> NamedHighlightingColors, IDictionary<string,string> Properties.
Definitions bind in XAML through HighlightingDefinitionTypeConverter (by name).

HighlightingColor: string? Name, HighlightingBrush? Foreground/Background,
FontWeight? FontWeight, FontStyle? FontStyle, int? FontSize, FontFamily? FontFamily,
bool? Underline, bool? Strikethrough; Freeze(), Clone(), MergeWith(HighlightingColor),
ToCss(). SimpleHighlightingBrush(Color) is the concrete brush.
Retheme a built-in definition at runtime by mutating its named colors:
    var def = HighlightingManager.Instance.GetDefinition("C#");
    def.GetNamedColor("Comment").Foreground = new SimpleHighlightingBrush(Colors.Gray);
    Editor.TextArea.TextView.Redraw();

CUSTOM XSHD DEFINITIONS - HighlightingLoader (static, .Highlighting.Xshd):
    static XshdSyntaxDefinition LoadXshd(XmlReader reader)
    static IHighlightingDefinition Load(XmlReader reader,
                                        IHighlightingDefinitionReferenceResolver? resolver)
    static IHighlightingDefinition Load(XshdSyntaxDefinition syntaxDefinition,
                                        IHighlightingDefinitionReferenceResolver? resolver)
Pass HighlightingManager.Instance as the resolver so <Import>/<Reference>
elements can find the built-in definitions. The Xshd namespace also exposes the
object model (XshdSyntaxDefinition, XshdRuleSet, XshdRule, XshdSpan, XshdKeywords,
XshdColor, XshdImport, XshdProperty, IXshdVisitor, SaveXshdVisitor) for building
or re-serializing definitions in code.

    IHighlightingDefinition custom;
    using (Stream s = GetType().Assembly.GetManifestResourceStream("MyApp.MyLang.xshd"))
    using (XmlReader reader = XmlReader.Create(s))
        custom = HighlightingLoader.Load(reader, HighlightingManager.Instance);
    HighlightingManager.Instance.RegisterHighlighting("MyLang", new[] { ".mylang" }, custom);
    Editor.SyntaxHighlighting = custom;

Highlighting without the editor: DocumentHighlighter(TextDocument document,
IHighlightingDefinition definition) : IHighlighter gives
HighlightedLine HighlightLine(int lineNumber) (Sections of HighlightedSection
{Offset, Length, Color}) with ToHtml(HtmlOptions?), ToRichText(),
ToRichTextModel(); HtmlClipboard.CreateHtmlFragment(IDocument, IHighlighter?,
ISegment?, HtmlOptions) builds a styled HTML fragment; RichText / RichTextModel
carry per-range HighlightingColor for text outside a document.
HighlightingColorizer(IHighlightingDefinition) : DocumentColorizingTransformer
is what SyntaxHighlighting installs (at index 0 of TextView.LineTransformers);
add it yourself to layer a second definition.

CUSTOM RENDERING (namespace .Rendering)
=======================================
Three extension points on Editor.TextArea.TextView:

    IList<IVisualLineTransformer> LineTransformers     restyle text runs
    IList<IBackgroundRenderer> BackgroundRenderers     draw on a layer
    IList<VisualLineElementGenerator> ElementGenerators replace text with
                                                       custom elements

DocumentColorizingTransformer (abstract) - the easy way to color text:
    protected abstract void ColorizeLine(DocumentLine line);
    protected void ChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action);
    protected ITextRunConstructionContext? CurrentContext   (only inside ColorizeLine)
VisualLineElement exposes VisualLineElementTextRunProperties TextRunProperties
with SetForegroundBrush(Brush?), SetBackgroundBrush(Brush?), SetFontWeight(FontWeight),
SetFontStyle(FontStyle), SetFontSize(double), SetFontFamily(string?),
SetUnderline(bool), SetStrikethrough(bool), SetTextDecorations(TextDecorations),
plus Brush? BackgroundBrush on the element itself.

    sealed class TodoColorizer : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            string text = CurrentContext.Document.GetText(line);
            int start = 0, index;
            while ((index = text.IndexOf("TODO", start, StringComparison.Ordinal)) >= 0)
            {
                ChangeLinePart(line.Offset + index, line.Offset + index + 4, element =>
                {
                    element.TextRunProperties.SetFontWeight(FontWeights.Bold);
                    element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Colors.OrangeRed));
                });
                start = index + 4;
            }
        }
    }
    Editor.TextArea.TextView.LineTransformers.Add(new TodoColorizer());

IBackgroundRenderer: KnownLayer Layer { get; } and
void Draw(TextView textView, SKCanvas canvas). KnownLayer is Background |
Selection | Text | Caret (drawn in that order). The canvas is in
device-independent pixels; use BackgroundGeometryBuilder to turn segments into
geometry: static IEnumerable<Rect> GetRectsForSegment(TextView, ISegment,
bool extendToFullWidthAtLineEnd = false), or an instance with
AddSegment(TextView, ISegment) / AddRectangle(TextView, Rect), CornerRadius,
BorderThickness, AlignToWholePixels, then IReadOnlyList<SKRect> CreateRectangles()
or SKPath? CreatePath().

    sealed class LineMarker : IBackgroundRenderer
    {
        public int LineNumber { get; set; } = 3;
        public KnownLayer Layer => KnownLayer.Background;
        public void Draw(TextView textView, SKCanvas canvas)
        {
            if (textView.Document == null || LineNumber > textView.Document.LineCount) return;
            DocumentLine line = textView.Document.GetLineByNumber(LineNumber);
            using var paint = new SKPaint { Color = new SKColor(255, 235, 59, 90) };
            foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, line, true))
                canvas.DrawRect((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height, paint);
        }
    }
    Editor.TextArea.TextView.BackgroundRenderers.Add(new LineMarker());
    // call Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background) after
    // changing renderer state; Redraw() / Redraw(int offset, int length) /
    // Redraw(ISegment?) when the TEXT styling changed.

VisualLineElementGenerator (abstract): int GetFirstInterestedOffset(int startOffset)
and VisualLineElement? ConstructElement(int offset); return a
FormattedTextElement(string text, int documentLength) or an
InlineObjectElement(int documentLength, UIElement element) to replace a
document range with custom content. LinkElementGenerator (built in, enabled by
Options.EnableHyperlinks) makes URLs clickable; VisualLineLinkText raises
RequestNavigate (set e.Handled to suppress the default launch).
TextView also exposes: VisualLines, VisualLinesValid, EnsureVisualLines(),
GetVisualLine(int documentLineNumber), GetOrConstructVisualLine(DocumentLine),
GetVisualPosition(TextViewPosition, VisualYPosition), GetPosition(Point),
DefaultLineHeight, DefaultBaseline, WideSpaceWidth, DocumentHeight,
CollapseLines(DocumentLine start, DocumentLine end) : CollapsedLineSection,
brushes NonPrintableCharacterBrush, LinkTextForegroundBrush,
LinkTextBackgroundBrush (bool LinkTextUnderline), ColumnRulerBrush/-Thickness,
CurrentLineBackground, CurrentLineBorderBrush/-Thickness, events
VisualLinesChanged, VisualLineConstructionStarting, ScrollOffsetChanged.

CODE COMPLETION AND INSIGHT (namespace .CodeCompletion)
=======================================================
The "windows" are in-app popups anchored to the text area's XamlRoot, never
focused, so typing keeps flowing into the editor. Keys while open: Up/Down/
PageUp/PageDown/Home/End move in the list, Tab/Enter insert, Escape closes.

CompletionWindow(TextArea textArea) : CompletionWindowBase
    CompletionList CompletionList         .CompletionData : IList<ICompletionData>
    bool CloseAutomatically               default true (close on focus loss /
                                          caret leaving the range)
    bool CloseWhenCaretAtBeginning        default false
CompletionWindowBase: TextArea TextArea; int StartOffset {get;set;} /
int EndOffset {get;set;} (the typed range that filters the list; default =
the caret offset), bool IsOpen, object? Content, bool ExpectInsertionBeforeStart,
virtual void Show() (THROWS if the text area is not in a visual tree yet, or
if the window was already closed), void Close(), event EventHandler? Closed.
A closed window cannot be reopened - create a new one per session.
CompletionList: IList<ICompletionData> CompletionData, ICompletionData? SelectedItem,
bool IsFiltering (default true: substring filter; false = StartsWith
selection), UIElement? EmptyContent, void SelectItem(string text),
void RequestInsertion(EventArgs e), event InsertionRequested, event SelectionChanged,
CompletionListBox ListBox.

ICompletionData (implement this):
    ImageSource? Image { get; }      string Text { get; }     (filter key)
    object? Content { get; }         (string or UIElement shown in the list)
    object? Description { get; }     (string or UIElement shown beside it)
    double Priority { get; }
    void Complete(TextArea textArea, ISegment completionSegment,
                  EventArgs insertionRequestEventArgs);

    // minimal completion: open on "." and insert on any non-identifier key
    sealed class WordData : ICompletionData
    {
        public WordData(string text) { Text = text; }
        public ImageSource? Image => null;
        public string Text { get; }
        public object? Content => Text;
        public object? Description => "Inserts " + Text;
        public double Priority => 0;
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
            => textArea.Document.Replace(completionSegment, Text);
    }

    CompletionWindow? completionWindow;
    Editor.TextArea.TextEntered += (_, e) =>
    {
        if (e.Text != ".") return;
        completionWindow = new CompletionWindow(Editor.TextArea);
        IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
        data.Add(new WordData("Length"));
        data.Add(new WordData("ToString"));
        completionWindow.Closed += (_, _) => completionWindow = null;
        completionWindow.Show();
    };
    Editor.TextArea.TextEntering += (_, e) =>
    {
        if (e.Text.Length > 0 && completionWindow != null && !char.IsLetterOrDigit(e.Text[0]))
            completionWindow.CompletionList.RequestInsertion(EventArgs.Empty);
        // leave e.Handled false so the typed character is still inserted
    };

InsightWindow(TextArea textArea) : CompletionWindowBase - a tool-tip style
popup at the caret; set Content (a string becomes a wrapping TextBlock, or any
UIElement), then Show(). bool CloseAutomatically (default true).
OverloadInsightWindow(TextArea textArea) : InsightWindow shows an
OverloadViewer bound to IOverloadProvider? Provider; Up/Down cycle overloads
when Provider.Count > 1.

IOverloadProvider : INotifyPropertyChanged
    int SelectedIndex { get; set; }   int Count { get; }   string CurrentIndexText { get; }
    object? CurrentHeader { get; }    object? CurrentContent { get; }   (string or UIElement)

    sealed class Overloads : IOverloadProvider
    {
        readonly string[] sigs = { "Write(string value)", "Write(int value)" };
        int index;
        public event PropertyChangedEventHandler? PropertyChanged;
        public int SelectedIndex { get => index; set { index = value; Raise(); } }
        public int Count => sigs.Length;
        public string CurrentIndexText => $"{index + 1} of {Count}";
        public object? CurrentHeader => sigs[index];
        public object? CurrentContent => "Writes the value to the output.";
        void Raise()
        {
            foreach (string p in new[] { nameof(SelectedIndex), nameof(CurrentIndexText),
                                         nameof(CurrentHeader), nameof(CurrentContent) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
    }
    var insight = new OverloadInsightWindow(Editor.TextArea) { Provider = new Overloads() };
    insight.Show();   // Escape closes; or call insight.Close()

SNIPPETS (namespace .Snippets)
==============================
A Snippet is a tree of SnippetElement objects; Snippet.Insert(TextArea textArea)
inserts it at the caret (replacing the selection) inside one undo group and, if
it contains replaceable fields, enters INTERACTIVE MODE: Tab / Shift+Tab jump
between fields, typing into a field updates every bound copy live, Enter or
Escape ends the mode (DeactivateReason.ReturnPressed / EscapePressed).

    Snippet : SnippetContainerElement          IList<SnippetElement> Elements
    SnippetTextElement                         string? Text  (literal text)
    SnippetReplaceableTextElement : SnippetTextElement   an editable field
    SnippetBoundElement                        SnippetReplaceableTextElement? TargetElement;
                                               virtual string ConvertText(string input)
                                               (mirrors the target's text, transformed)
    SnippetCaretElement([bool setCaretOnlyIfTextIsSelected])  final caret spot
    SnippetSelectionElement                    int Indentation; re-inserts the
                                               text that was selected
    SnippetAnchorElement(string name)          named anchor for other elements
    InsertionContext(TextArea, int insertionPosition)  the insertion state:
        Document, SelectedText, Indentation, Tab, LineTerminator,
        InsertionPosition, StartPosition, InsertText(string),
        RegisterActiveElement(SnippetElement, IActiveElement),
        GetActiveElement(SnippetElement), ActiveElements, Deactivate(SnippetEventArgs?),
        events InsertionCompleted, Deactivated
    IActiveElement: OnInsertionCompleted(), Deactivate(SnippetEventArgs), IsEditable, Segment
    Derive from SnippetElement (abstract void Insert(InsertionContext context))
    for custom elements.

    // for (int i = 0; i < |count|; i++) { <caret> }  with "i" bound in 3 places
    var loopVar = new SnippetReplaceableTextElement { Text = "i" };
    var count   = new SnippetReplaceableTextElement { Text = "count" };
    var snippet = new Snippet
    {
        Elements =
        {
            new SnippetTextElement { Text = "for (int " }, loopVar,
            new SnippetTextElement { Text = " = 0; " },
            new SnippetBoundElement { TargetElement = loopVar },
            new SnippetTextElement { Text = " < " }, count,
            new SnippetTextElement { Text = "; " },
            new SnippetBoundElement { TargetElement = loopVar },
            new SnippetTextElement { Text = "++)\n{\n\t" },
            new SnippetCaretElement(),
            new SnippetTextElement { Text = "\n}" },
        }
    };
    snippet.Insert(Editor.TextArea);

FOLDING (namespace .Folding)
============================
FoldingManager
    static FoldingManager Install(TextArea textArea)   adds a FoldingMargin to
        LeftMargins and a FoldingElementGenerator to the TextView
    static void Uninstall(FoldingManager manager)
    FoldingSection CreateFolding(int startOffset, int endOffset)
    void RemoveFolding(FoldingSection fs)      void Clear()
    void UpdateFoldings(IEnumerable<NewFolding> newFoldings, int firstErrorOffset)
        (newFoldings SORTED by StartOffset; keeps IsFolded state of matching
        sections; firstErrorOffset = -1 when there was no parse error)
    IEnumerable<FoldingSection> AllFoldings
    FoldingSection? GetNextFolding(int startOffset)
    int GetNextFoldedFoldingStart(int startOffset)
    ReadOnlyCollection<FoldingSection> GetFoldingsAt(int startOffset)
    ReadOnlyCollection<FoldingSection> GetFoldingsContaining(int offset)
FoldingSection : TextSegment - bool IsFolded, string? Title (collapsed
placeholder text; null is shown as "..."), string TextContent, object? Tag.
NewFolding - int StartOffset, int EndOffset, string? Name, bool DefaultClosed,
bool IsDefinition; ctors NewFolding() and NewFolding(int start, int end).
XmlFoldingStrategy - void UpdateFoldings(FoldingManager manager, TextDocument document);
IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset);
bool ShowAttributesWhenFolded. There is no built-in brace strategy; the demo's
BraceFoldingStrategy (about 40 lines) is the template for one.
FoldingMargin brushes: FoldingMarkerBrush, FoldingMarkerBackgroundBrush,
SelectedFoldingMarkerBrush, SelectedFoldingMarkerBackgroundBrush.

    FoldingManager foldingManager = FoldingManager.Install(Editor.TextArea);
    var xmlFolding = new XmlFoldingStrategy();
    xmlFolding.UpdateFoldings(foldingManager, Editor.Document);   // re-run after edits
    // hand-made foldings:
    foldingManager.UpdateFoldings(new[] { new NewFolding(0, 120) { Name = "header" } }, -1);
    // BEFORE assigning a different Editor.Document:
    FoldingManager.Uninstall(foldingManager);

SEARCH PANEL (namespace .Search)
================================
NOT installed by default - Ctrl+F does nothing until you call:

    SearchPanel panel = SearchPanel.Install(Editor.TextArea);

Install adds a SearchInputHandler to the default input handler that binds
EditorCommands.Find (Ctrl+F: open, seeded with a single-line selection),
SearchCommands.FindNext (F3), SearchCommands.FindPrevious (Shift+F3) and
SearchCommands.CloseSearchPanel (Escape). The panel appears top-right over the
text and highlights every match through a background renderer.
SearchPanel members: bool UseRegex, bool MatchCase, bool WholeWords,
string SearchPattern, Brush? MarkerBrush, Brush? MarkerBorderBrush,
double MarkerBorderThickness, double MarkerCornerRadius, Localization Localization
(override its virtual *Text properties to translate the buttons/messages),
void Open() / Close() / Reactivate() / FindNext() / FindPrevious() / Uninstall(),
bool IsClosed, void RegisterCommands(ICollection<EditorCommandBinding>),
event SearchOptionsChanged (SearchOptionsChangedEventArgs: SearchPattern,
MatchCase, UseRegex, WholeWords).
Searching WITHOUT the panel: SearchStrategyFactory.Create(string searchPattern,
bool ignoreCase, bool matchWholeWords, SearchMode mode) (SearchMode.Normal |
RegEx | Wildcard) returns an ISearchStrategy with
IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
and ISearchResult? FindNext(...); ISearchResult : ISegment adds
string ReplaceWith(string replacement). An invalid pattern throws
SearchPatternException.

    ISearchStrategy strategy = SearchStrategyFactory.Create("foo", true, false, SearchMode.Normal);
    foreach (ISearchResult hit in strategy.FindAll(Editor.Document, 0, Editor.Document.TextLength))
        Editor.Document.Replace(hit.Offset, hit.Length, "bar");   // wrap in RunUpdate() for one undo step

INDENTATION (namespaces .Indentation, .Indentation.CSharp)
==========================================================
IIndentationStrategy: void IndentLine(TextDocument document, DocumentLine line)
(runs when Enter inserts a new line) and void IndentLines(TextDocument document,
int beginLine, int endLine) (runs for AdvancedTextEditCommands.IndentSelection,
Ctrl+I). Assign to Editor.TextArea.IndentationStrategy.
    DefaultIndentationStrategy   copies the previous line's leading whitespace;
                                 IndentLines does nothing. (the default)
    CSharpIndentationStrategy    brace-aware; ctor () or (AdvancedTextEditOptions)
                                 which takes IndentationString from the options;
                                 string IndentationString; also
                                 void Indent(IDocumentAccessor document, bool keepEmptyLines)
                                 with TextDocumentAccessor(TextDocument[, minLine, maxLine]).
Tab/Shift+Tab indent/unindent the selected lines using Options.IndentationString.

    Editor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(Editor.Options);

OPTIONS - AdvancedTextEditOptions (namespace root)
===================================================
INotifyPropertyChanged; every change re-renders. Editor.Options is shared with
the TextArea and TextView. Copy with new AdvancedTextEditOptions(options).
    property                              default   meaning
    bool ShowSpaces                       false     draw a dot for spaces
    bool ShowTabs                         false     draw an arrow for tabs
    bool ShowEndOfLine                    false     draw a paragraph mark
    bool ShowBoxForControlCharacters      true      boxed names for controls
    bool EnableHyperlinks                 true      clickable URLs
    bool EnableEmailHyperlinks            true      clickable mailto
    bool RequireControlModifierForHyperlinkClick  true
    int IndentationSize                   4         width of one indent (1..1000)
    bool ConvertTabsToSpaces              false     indent with spaces
    string IndentationString              (derived) "\t" or IndentationSize spaces
    string GetIndentationString(int column)
    bool CutCopyWholeLine                 true      empty selection = whole line
    bool AllowScrollBelowDocument         false
    double WordWrapIndentation            0         extra indent of wrapped lines
    bool InheritWordWrapIndentation       true
    bool EnableRectangularSelection       true      Alt box selection
    bool EnableVirtualSpace               false     caret past line end
    bool ShowColumnRuler                  false     vertical guide line
    int ColumnRulerPosition               80
    bool HighlightCurrentLine             false     tinted band on the caret line
                                                    (built-in default colors)
    bool HideCursorWhileTyping            true
    bool AllowToggleOverstrikeMode        false     lets Insert toggle overtype
    bool EnableTextDragDrop               true      PRESENT BUT INACTIVE (see below)
    bool EnableImeSupport                 true      PRESENT BUT INACTIVE (see below)

COMMANDS AND KEY BINDINGS (namespaces .Editing, root, .Search)
==============================================================
Commands are identity tokens (EditorCommand: string Name,
IReadOnlyList<KeyGesture> DefaultGestures); the default input handlers bind the
implementations. Execute from code through
((TextAreaInputHandler)Editor.TextArea.ActiveInputHandler).ExecuteCommand(cmd, null)
or the editor's Copy/Cut/Paste/Undo/... wrappers.

DEFAULT BINDINGS (all active out of the box; "Menu" modifier = Alt):
    EditorCommands (namespace .Editing)
      Copy               Ctrl+C, Ctrl+Insert      Cut          Ctrl+X, Shift+Delete
      Paste              Ctrl+V, Shift+Insert     SelectAll    Ctrl+A
      Undo               Ctrl+Z                   Redo         Ctrl+Y
      Delete             (no gesture; selection only)
      DeleteNextCharacter Delete                  DeleteNextWord     Ctrl+Delete
      Backspace          Back                     DeletePreviousWord Ctrl+Back
      EnterParagraphBreak Enter                   EnterLineBreak     Shift+Enter
      TabForward         Tab                      TabBackward        Shift+Tab
      MoveLeftByCharacter/MoveRightByCharacter    Left / Right   (+Shift selects)
      MoveLeftByWord/MoveRightByWord              Ctrl+Left / Ctrl+Right (+Shift)
      MoveUpByLine/MoveDownByLine                 Up / Down (+Shift)
      MoveUpByPage/MoveDownByPage                 PageUp / PageDown (+Shift)
      MoveToLineStart/MoveToLineEnd               Home / End (+Shift)
      MoveToDocumentStart/MoveToDocumentEnd       Ctrl+Home / Ctrl+End (+Shift)
      Find               Ctrl+F   (bound ONLY after SearchPanel.Install)
      ToggleInsert       Insert   (defined, NOT bound - see ToggleOverstrike)
    AdvancedTextEditCommands (namespace root)
      ToggleOverstrike   Insert   (no-op unless Options.AllowToggleOverstrikeMode)
      DeleteLine         Ctrl+D
      IndentSelection    Ctrl+I   (runs IndentationStrategy.IndentLines)
      RemoveLeadingWhitespace, RemoveTrailingWhitespace, ConvertToUppercase,
      ConvertToLowercase, ConvertToTitleCase, InvertCase, ConvertTabsToSpaces,
      ConvertSpacesToTabs, ConvertLeadingTabsToSpaces, ConvertLeadingSpacesToTabs
                         (bound, no default gesture - give them one or call
                         ExecuteCommand)
    RectangleSelection.BoxSelect*   Alt+Shift+Left/Right/Up/Down/Home/End,
                                    Ctrl+Alt+Shift+Left/Right
    SearchCommands (after SearchPanel.Install)
      FindNext F3    FindPrevious Shift+F3    CloseSearchPanel Escape

ADD YOUR OWN: KeyBinding(EditorCommand command, KeyGesture gesture[, object? commandParameter])
or KeyBinding(EditorCommand, VirtualKey key, VirtualKeyModifiers modifiers) into
TextArea.InputBindings, and EditorCommandBinding(EditorCommand command,
ExecutedEditorCommandEventHandler? executed[, CanExecuteEditorCommandEventHandler? canExecute])
into TextArea.CommandBindings. KeyGesture(VirtualKey Key, VirtualKeyModifiers Modifiers)
is a record struct with bool Matches(VirtualKey, VirtualKeyModifiers).

    var upper = new EditorCommand("MyUpper", new KeyGesture(VirtualKey.U, VirtualKeyModifiers.Control));
    Editor.TextArea.CommandBindings.Add(new EditorCommandBinding(upper,
        (s, e) => { Editor.SelectedText = Editor.SelectedText.ToUpperInvariant(); e.Handled = true; },
        (s, e) => { e.CanExecute = !Editor.TextArea.Selection.IsEmpty; e.Handled = true; }));
    Editor.TextArea.InputBindings.Add(new KeyBinding(upper, VirtualKey.U, VirtualKeyModifiers.Control));

For modal input (a picker that swallows keys), derive from
TextAreaStackedInputHandler (override bool OnPreviewKeyDown(VirtualKey, VirtualKeyModifiers))
and PushStackedInputHandler/PopStackedInputHandler it.

RENDERING FACTS
===============
  - Virtualized: only the visual lines intersecting the viewport are
    constructed; a height tree tracks line heights, so a 100,000-line document
    opens and scrolls like a small one.
  - One paint pass on a Skia canvas: Background-layer renderers, selection,
    each visual line's element backgrounds + text + decorations, then
    Caret-layer renderers. InvalidateLayer(KnownLayer) schedules a repaint;
    Redraw(...) rebuilds visual lines for a range.
  - Text is shaped and measured by the family's shared text engine (the
    TextLayout package), so glyphs, widths and line heights are identical to a
    TextBlock in the same font - the editor never falls back to a system font.
  - The editor manages its own scrolling with two ScrollBar controls synced to
    the TextView's scroll offsets; there is no ScrollViewer inside.
  - Fonts: FontFamily/FontSize/FontWeight/FontStyle set on the editor are
    pushed down to the TextArea and TextView. Use a monospace face for code
    (the default family name is "monospace").

OTHER PUBLIC TYPES (less common, one line each)
===============================================
    Document
    IDocumentLine : ISegment           interface DocumentLine implements (adds TotalLength, ...)
    TextChangeEventArgs                base of DocumentChangeEventArgs: Offset, RemovedText,
                                       RemovalLength, InsertedText, InsertionLength
    StringTextSource(string text[, ITextSourceVersion?])   ITextSource over a string
    RopeTextSource(Rope<char> rope[, ITextSourceVersion?]) ITextSource over a rope
    TextSourceVersionProvider          issues ITextSourceVersion values (document.Version)
    WeakLineTracker.Register(TextDocument, ILineTracker) / Deregister()   weak ILineTracker
    TextLocationConverter              TypeConverter for TextLocation ("line;column" strings)
    CharacterClass                     enum from TextUtilities.GetCharacterClass:
                                       Other | Whitespace | IdentifierPart | LineTerminator | CombiningMark
    Editing
    AbstractMargin : Panel, ITextViewConnect   base for LeftMargins items (TextView, Document)
    LineNumberMargin : AbstractMargin  the line-number margin (Brush? Foreground)
    DottedLineMargin.Create()          the dotted separator element; IsDottedLineMargin(UIElement)
    ExecutedEditorCommandEventArgs     Command, Parameter, Handled
    CanExecuteEditorCommandEventArgs   Command, Parameter, CanExecute, Handled
    DataObjectCopyingEventArgs         DataObject (DataPackage), IsDragDrop, CancelCommand()
    DataObjectSettingDataEventArgs     DataObject, Format, CancelCommand()
    DataObjectPastingEventArgs         DataObject (DataPackageView), IsDragDrop,
                                       FormatToApply, CancelCommand()
    CaretWeakEventManager              weak PositionChanged subscription helper
    Rendering
    VisualLine                         one rendered row group: FirstDocumentLine, LastDocumentLine,
                                       Elements, TextLines, Height, VisualTop, VisualLength,
                                       GetVisualColumn(...), GetRelativeOffset(int),
                                       GetTextViewPosition(...), GetVisualPosition(...)
    TextLineLayout                     one wrapped row: Top, Height, Baseline, Width,
                                       FirstVisualColumn, LastVisualColumn
    VisualLineText : VisualLineElement the default text element (ParentVisualLine)
    InlineObjectRun                    layout record of an InlineObjectElement (Element,
                                       DesiredSize, VisualColumn, VisualLine)
    GlobalTextRunProperties            the editor-wide font/brush/culture defaults
                                       (read-only from the outside)
    ColorizingTransformer              base of DocumentColorizingTransformer working in
                                       VISUAL columns: ChangeVisualElements(startVC, endVC, action)
    ITextViewConnect                   AddToTextView/RemoveFromTextView - implement on a
                                       transformer/renderer to learn when it is (un)installed
    LayerInsertionPosition             enum Below | Replace | Above
    MouseHoverLogic(UIElement target)  hover detection (MouseHover / MouseHoverStopped)
    RequestNavigateEventArgs           Uri, TargetName, Handled (VisualLineLinkText.RequestNavigate)
    VisualLineConstructionStartEventArgs  FirstLineInView (VisualLineConstructionStarting)
    VisualLinesInvalidException        thrown by TextView.VisualLines while lines are invalid
                                       (call EnsureVisualLines() first)
    TextViewWeakEventManager           weak DocumentChanged/VisualLinesChanged/ScrollOffsetChanged
    Highlighting
    HighlightingRule                   Regex? Regex, HighlightingColor? Color   (rule-set item)
    HighlightingSpan                   StartExpression, EndExpression, RuleSet, StartColor,
                                       SpanColor, EndColor, SpanColorIncludesStart/End
    HighlightingEngine(HighlightingRuleSet)   HighlightLine(IDocument, IDocumentLine),
                                       ScanLine(...), CurrentSpanStack - the raw engine
    RichTextColorizer(RichTextModel)   : DocumentColorizingTransformer - colors from a model
    HighlightingDefinitionInvalidException  thrown by the XSHD loader / manager for bad definitions
    XshdElement, XshdReference<T>, XshdRegexType (Default | IgnorePatternWhitespace)   XSHD model parts
    Snippets
    IReplaceableActiveElement : IActiveElement   string Text; event TextChanged
    AnchorElement : IActiveElement     the active element a SnippetAnchorElement registers
    Utils
    Rope<T> : IList<T>                 the persistent rope (Insert/Remove/GetRange/Concat ...)
    CharRope                           Rope<char> extensions: Create(string), InsertText, AddText,
                                       ToString(start, length), IndexOf(...)
    RopeTextReader(Rope<char>)         TextReader over a rope
    ImmutableStack<T>                  Empty, Push, Pop, Peek, PeekOrDefault, IsEmpty
    StringSegment(string text, int offset, int count)   a (Text, Offset, Count) slice
    Deque<T>, CompressingTreeList<T>, NullSafeCollection<T>   collection helpers
    PixelSnapHelpers                   GetPixelSize(TextView), PixelAlign(double, double),
                                       PixelAlign(Rect, Size)
    IWeakEventListener, WeakEventManagerBase<TManager, TEventSource>, PropertyChangedWeakEventManager,
    AdvancedTextEditWeakEventManager, TextDocumentWeakEventManager
                                       the weak-event plumbing (usable for long-lived
                                       listeners on short-lived editors)

COMPLETE EXAMPLES
=================
1) XAML + code-behind: load a file, highlight by extension, save, status line

    <!-- Views/MainPage.xaml (inside the shared .UI project) -->
    <Page x:Class="MyApp.Views.MainPage"
          xmlns="clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:advtxt="clr-namespace:CodeBrix.Platform.UI.AdvancedTextEdit;assembly=CodeBrix.Platform.UI.AdvancedTextEdit">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <advtxt:AdvancedTextEdit x:Name="Editor"
                FontFamily="monospace" FontSize="13"
                ShowLineNumbers="True" WordWrap="False" />
            <TextBlock x:Name="Status" Grid.Row="1" Margin="8,4" />
        </Grid>
    </Page>

    // Views/MainPage.xaml.cs
    using CodeBrix.Platform.UI.AdvancedTextEdit;
    using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
    using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation.CSharp;
    using CodeBrix.Platform.UI.AdvancedTextEdit.Search;
    using Microsoft.UI.Xaml.Controls;
    using System.IO;

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            SearchPanel.Install(Editor.TextArea);                       // Ctrl+F
            Editor.Options.HighlightCurrentLine = true;
            Editor.Options.ConvertTabsToSpaces = true;
            Editor.TextArea.Caret.PositionChanged += (_, _) =>
                Status.Text = $"Ln {Editor.TextArea.Caret.Line}, Col {Editor.TextArea.Caret.Column}"
                            + (Editor.IsModified ? "  *" : "");
        }

        public void OpenFile(string path)
        {
            Editor.Load(path);                                          // encoding auto-detected
            Editor.SyntaxHighlighting =
                HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path));
            Editor.TextArea.IndentationStrategy = Editor.SyntaxHighlighting?.Name == "C#"
                ? new CSharpIndentationStrategy(Editor.Options)
                : new CodeBrix.Platform.UI.AdvancedTextEdit.Indentation.DefaultIndentationStrategy();
            Editor.ScrollToHome();
        }

        public void SaveFile(string path) => Editor.Save(path);        // uses Editor.Encoding
    }

2) Document-level editing with anchors and one undo step

    using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

    TextDocument doc = Editor.Document;
    TextAnchor mark = doc.CreateAnchor(doc.GetOffset(3, 1));          // start of line 3
    mark.SurviveDeletion = true;
    using (doc.RunUpdate())                                            // one undo group
    {
        doc.Insert(0, "// generated\n");
        DocumentLine last = doc.GetLineByNumber(doc.LineCount);
        doc.Replace(last.Offset, last.Length, last.Length == 0 ? "// end" : doc.GetText(last) + " // end");
    }
    int lineThreeNow = mark.Line;                                      // anchor followed the insert
    Editor.TextArea.Caret.Offset = mark.Offset;
    Editor.TextArea.Caret.BringCaretToView();
    doc.UndoStack.Undo();                                              // reverts both edits at once

3) Programmatic search-and-replace, read-only regions, and a custom colorizer
   are shown under SEARCH PANEL, TEXT AREA and CUSTOM RENDERING above; code
   completion, overload insight, snippets and folding under their own headings.

MINIMUM VIABLE PROJECT
======================
Add the package to the shared .Core class library of a CodeBrix.Platform app
(the heads reference .Core and compile the shared .UI XAML):

    <!-- MyApp.Core/MyApp.Core.csproj (excerpt) -->
    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever" />
    </ItemGroup>

Then the XAML and code-behind from COMPLETE EXAMPLES 1. Nothing else is
required: no theme resources, no native libraries, no initialization call.
Give the control a bounded size (a star-sized Grid row/column or explicit
Width/Height) - it sizes its viewport from the space it is given.

PERFORMANCE TIPS
================
  - Batch edits in doc.RunUpdate() / Editor.DeclareChangeBlock(): one
    re-layout, one undo step, one TextChanged.
  - Prefer Caret.Location / Caret.Offset over Caret.Position; Position
    validates the visual column (constructs the visual line).
  - Folding strategies re-parse the whole document: run them on a timer or
    after idle (the demo uses a 2-second DispatcherTimer), not on every key.
  - Colorizers run per visible line on every visual-line rebuild - keep
    ColorizeLine cheap (no regex compilation inside it; cache brushes).
  - Background renderers should draw only what intersects the viewport; use
    BackgroundGeometryBuilder.GetRectsForSegment (it already clips to the
    visible visual lines).
  - Setting Text replaces the whole rope; for appending output use
    AppendText / doc.Insert(doc.TextLength, ...).
  - HighlightingManager.HighlightingDefinitions returns a COPY on every call;
    fetch it once for ComboBoxes.
  - Turning WordWrap on for very long lines makes ScrollTo/caret math more
    expensive; keep it off for log-style files.
  - Load large files on a worker thread into a TextDocument, then
    SetOwnerThread to the UI thread and assign Editor.Document.

COMMON PITFALLS TO AVOID
========================
  - The editor manages its own scrolling and virtualization - do NOT wrap it in
    a ScrollViewer; give it a bounded height/width (a star row, not Auto).
  - Highlighting definitions are looked up by NAME or EXTENSION via
    HighlightingManager.Instance; the result is null for unknown ones, and
    null on SyntaxHighlighting silently disables coloring. GetDefinition("CSharp")
    is null - the name is "C#"; extensions include the dot (".cs").
  - The search panel is not installed by default: Ctrl+F/F3 do nothing until
    SearchPanel.Install(Editor.TextArea).
  - Editor.Text = "..." resets the caret to 0 and CLEARS the undo stack. Use
    Document.Replace(...) when the user should be able to undo.
  - TextDocument is single-thread-owned: any access from another thread
    throws InvalidOperationException (SetOwnerThread hands it over).
  - ScrollTo/ScrollToLine need a laid-out editor; calling them from a
    constructor or before the page is loaded does nothing useful. Defer to
    Loaded or the dispatcher.
  - CompletionWindow.Show() throws if the TextArea is not yet in a visual tree,
    and a Closed window cannot be shown again - construct a new window per
    completion session (the Closed event is where you drop your reference).
  - FoldingManager is bound to the TextArea's CURRENT document: call
    FoldingManager.Uninstall(manager) before assigning a new Editor.Document,
    then Install again.
  - Defaults are a white surface with black text and the built-in XSHD colors
    assume a light background; in a dark app set Background/Foreground on the
    editor AND retheme the definition's named colors (or ship your own XSHD).
  - IsReadOnly = true replaces TextArea.ReadOnlySectionProvider; if you
    installed a TextSegmentReadOnlySectionProvider, toggling IsReadOnly
    discards it.
  - Options.EnableTextDragDrop and Options.EnableImeSupport are accepted for
    compatibility but the features are not available in this version.
  - EditorCommands.Delete has no gesture and only runs with a selection; the
    Del key is DeleteNextCharacter. EditorCommands.ToggleInsert is defined but
    not bound; Insert maps to AdvancedTextEditCommands.ToggleOverstrike, which
    is inert unless Options.AllowToggleOverstrikeMode is true.
  - HighlightingManager is process-wide: RegisterHighlighting with an existing
    name replaces it for every editor in the app (guard against re-registering
    when pages are recreated).
  - TextAnchor is weakly referenced; store it in a field, not a local you drop.
    A deleted anchor (SurviveDeletion false) throws on Offset - check IsDeleted.
  - Selection objects are immutable: Selection.Create(...) returns a new one
    that you must ASSIGN to TextArea.Selection.
  - In TextEntering do not set e.Handled = true unless you really want to
    swallow the typed character (the completion pattern relies on leaving it).
  - "MarkDownWithFontSize" is registered after "MarkDown" for ".md", so
    GetDefinitionByExtension(".md") returns the font-size variant.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No IME composition (CJK input methods) and no drag-and-drop of selected
    text in this version, although the two option flags exist.
  - No built-in language services: no C#/other parsers, no semantic
    completion, no error squiggles - ICompletionData/IOverloadProvider and the
    rendering hooks are the seams for your own.
  - No brace-folding strategy in the box (only XmlFoldingStrategy); no
    indentation strategy beyond DefaultIndentationStrategy and
    CSharpIndentationStrategy.
  - No find-and-replace UI (the SearchPanel finds only; replace through
    ISearchStrategy + Document.Replace).
  - No printing, no minimap, no multiple carets, no split views, no UI
    automation peer, no themes/resource dictionaries to restyle the chrome (the
    editor, search panel and completion popups build their visuals in code and
    expose brush properties instead).
  - No XAML-declared key bindings or commands; bindings are added from code.
  - The XSHD format is the only highlighting definition format (no TextMate
    grammars).

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/AdvancedTextEditDemo
      The full editor demo for all six heads (.LinuxX11, .LinuxWayland,
      .LinuxFrameBuffer, .MacOS, .Win32Skia, .WinWpfSkia): open/save through
      the file pickers, cut/copy/paste/undo/redo, word wrap / line numbers /
      show-end-of-line toggles, a highlighting ComboBox over
      HighlightingManager.Instance.HighlightingDefinitions, a custom XSHD
      (Assets/CustomHighlighting.xshd registered as "Custom Highlighting" for
      .cool), dot-triggered code completion (CodeCompletion/MyCompletionData.cs),
      SearchPanel.Install, XmlFoldingStrategy + a BraceFoldingStrategy
      (Folding/BraceFoldingStrategy.cs) refreshed by a timer, C# smart
      indentation, and a reflection-driven property pane over the editor,
      its TextArea and its Options.
      Page: src/AdvancedTextEditDemo.UI/Views/MainPage.xaml(.cs)
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.AdvancedTextEdit.Tests
      Unit tests that double as API examples: Document/ (TextAnchorTest,
      UndoStackTests, LineManagerTests, TextSegmentTreeTest, ChangeTrackingTest,
      TextUtilitiesTests, CollapsingTests, HeightTests), Editing/
      (TextSegmentReadOnlySectionTests), Highlighting/ (HighlightingManagerTests,
      XmlHighlightingDefinitionTests, DeserializationTests, RichTextTests,
      HighlightedLineMergeTests), Search/ (FindTests), Utils/ (RopeTests,
      CaretNavigationTests, IndentationStringTests, CompressingTreeListTests).

QUICK REFERENCE CARD
====================
    // control
    Editor.Load(path); Editor.Save(path);           // Load(Stream)/Save(Stream) too
    Editor.Text  Editor.Document  Editor.Options  Editor.TextArea
    Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
    Editor.ShowLineNumbers = true; Editor.WordWrap = false; Editor.IsReadOnly = false;
    Editor.CaretOffset  Editor.SelectedText  Editor.Select(start, length)  Editor.SelectAll();
    Editor.Undo(); Editor.Redo(); Editor.CanUndo  Editor.IsModified  Editor.Encoding
    Editor.ScrollToLine(n); Editor.ScrollTo(line, col);      // after layout
    Editor.TextChanged  Editor.DocumentChanged  Editor.OptionChanged  Editor.MouseHover
    // document (0-based offsets; TextLocation/lines/columns 1-based)
    doc.Insert(o, s); doc.Remove(o, len); doc.Replace(o, len, s); doc.GetText(o, len);
    doc.GetLineByNumber(n)/.GetLineByOffset(o)  line.Offset/.Length/.EndOffset/.LineNumber
    doc.GetOffset(line, col)  doc.GetLocation(offset)  doc.LineCount  doc.TextLength
    using (doc.RunUpdate()) { ... }                 // one undo group, batched events
    TextAnchor a = doc.CreateAnchor(o); a.Offset; a.MovementType; a.SurviveDeletion;
    doc.UndoStack.Undo()/Redo()/CanUndo/SizeLimit/MarkAsOriginalFile()
    doc.Changed += (s, e) => ...  // DocumentChangeEventArgs: Offset, InsertionLength,
                                  // RemovalLength, InsertedText/RemovedText (ITextSource)
    doc.SetOwnerThread(thread);                     // cross-thread hand-over
    // text area
    ta.Caret.Offset / .Line / .Column / .Location / .PositionChanged / .BringCaretToView()
    ta.Selection = Selection.Create(ta, start, end); ta.Selection.GetText(); ta.SelectionChanged
    ta.TextEntering (veto with e.Handled)  ta.TextEntered  ta.PerformTextInput("x")
    ta.IndentationStrategy = new CSharpIndentationStrategy(Editor.Options);
    ta.ReadOnlySectionProvider = new TextSegmentReadOnlySectionProvider<TextSegment>(doc);
    ta.CommandBindings.Add(new EditorCommandBinding(cmd, executed, canExecute));
    ta.InputBindings.Add(new KeyBinding(cmd, VirtualKey.K, VirtualKeyModifiers.Control));
    // highlighting
    HighlightingManager.Instance.GetDefinition("C#") / .GetDefinitionByExtension(".xml")
    HighlightingManager.Instance.RegisterHighlighting(name, new[]{".ext"}, definition);
    HighlightingLoader.Load(XmlReader.Create(stream), HighlightingManager.Instance)
    definition.GetNamedColor("Comment").Foreground = new SimpleHighlightingBrush(color);
    new DocumentHighlighter(doc, definition).HighlightLine(n).ToHtml()
    // rendering hooks (Editor.TextArea.TextView)
    tv.LineTransformers.Add(new MyColorizer());     // : DocumentColorizingTransformer
    tv.BackgroundRenderers.Add(new MyRenderer());   // : IBackgroundRenderer (Layer, Draw(tv, SKCanvas))
    tv.ElementGenerators.Add(new MyGenerator());    // : VisualLineElementGenerator
    tv.Redraw(); tv.InvalidateLayer(KnownLayer.Background); tv.CurrentLineBackground = brush;
    // completion / insight
    var w = new CompletionWindow(ta); w.CompletionList.CompletionData.Add(data);
    w.Closed += ...; w.Show();                     // new window per session
    w.CompletionList.RequestInsertion(EventArgs.Empty);   // from TextEntering
    new InsightWindow(ta) { Content = "text or UIElement" }.Show();
    new OverloadInsightWindow(ta) { Provider = overloads }.Show();
    // snippets
    new Snippet { Elements = { new SnippetTextElement { Text = "..." }, replaceable,
                  new SnippetBoundElement { TargetElement = replaceable }, new SnippetCaretElement() } }
        .Insert(ta);                               // Tab/Shift+Tab fields, Enter/Esc end
    // folding
    var fm = FoldingManager.Install(ta); new XmlFoldingStrategy().UpdateFoldings(fm, doc);
    fm.UpdateFoldings(newFoldings, -1); fm.CreateFolding(s, e); FoldingManager.Uninstall(fm);
    // search
    var panel = SearchPanel.Install(ta);           // Ctrl+F, F3, Shift+F3, Esc
    panel.SearchPattern = "x"; panel.Open(); panel.FindNext(); panel.Close();
    SearchStrategyFactory.Create(pattern, ignoreCase, wholeWords, SearchMode.RegEx)
        .FindAll(doc, 0, doc.TextLength)
    // options
    Editor.Options.IndentationSize = 4; .ConvertTabsToSpaces = true; .HighlightCurrentLine = true;
    Editor.Options.ShowSpaces/.ShowTabs/.ShowEndOfLine/.ShowColumnRuler/.ColumnRulerPosition
    Editor.Options.EnableRectangularSelection/.EnableVirtualSpace/.CutCopyWholeLine
    // commands
    EditorCommands.Copy/Cut/Paste/SelectAll/Undo/Redo/Find/...
    AdvancedTextEditCommands.DeleteLine/IndentSelection/ConvertToUppercase/...
    ((TextAreaInputHandler)ta.ActiveInputHandler)
        .ExecuteCommand(AdvancedTextEditCommands.ConvertToUppercase, null);
