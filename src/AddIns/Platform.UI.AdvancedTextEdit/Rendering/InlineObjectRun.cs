#nullable enable

using System;
using System.Text;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/InlineObjectRun.cs in the AvalonEdit repo (MIT).
//The WPF TextEmbeddedObject protocol is gone: the element measures its hosted UIElement during
//BuildLayoutText and contributes enough no-break spaces to reserve the cells (padding the layout
//so the engine cannot wrap inside the object), then registers an InlineObjectRun with the text
//view, which arranges the UIElement over those cells. InlineObjectRun is now a plain record of
//that registration (element, desired size, position) instead of a TextRun; its VisualColumn and
//OwnerElement members are new, added so the view can place the child without re-deriving them.

/// <summary>
/// A inline UIElement in the document.
/// </summary>
public class InlineObjectElement : VisualLineElement
{
	/// <summary>
	/// Gets the inline element that is displayed.
	/// </summary>
	public UIElement Element { get; private set; }

	/// <summary>
	/// Creates a new InlineObjectElement.
	/// </summary>
	/// <param name="documentLength">The length of the element in the document. Must be non-negative.</param>
	/// <param name="element">The element to display.</param>
	public InlineObjectElement(int documentLength, UIElement element)
		: base(1, documentLength)
	{
		if (element == null)
			throw new ArgumentNullException(nameof(element));
		this.Element = element;
	}

	/// <inheritdoc/>
	public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
	{
		if (layoutText == null)
			throw new ArgumentNullException(nameof(layoutText));
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		Size desiredSize = Element.DesiredSize;

		double spaceWidth = context.TextView.WideSpaceWidth;
		int cells = 1;
		if (spaceWidth > 0 && desiredSize.Width > 0)
			cells = Math.Max(1, (int)Math.Ceiling(desiredSize.Width / spaceWidth));

		var run = new InlineObjectRun(Element, this)
		{
			DesiredSize = desiredSize,
			VisualColumn = this.VisualColumn,
			VisualLine = context.VisualLine,
		};
		context.VisualLine.hasInlineObjects = true;
		context.TextView.AddInlineObject(run);

		// No-break spaces reserve the cells and keep the engine from wrapping inside the object.
		string padding = new string('\u00A0', cells);
		layoutText.Append(padding);
		return CreateTextRunDescriptor(padding);
	}
}

/// <summary>
/// The registration of one inline UIElement with the text view: which element to show, how big it
/// wants to be, and where in the text it sits. The text view arranges the element over the cells
/// its visual line element reserved in the layout.
/// </summary>
public sealed class InlineObjectRun
{
	/// <summary>
	/// Creates a new InlineObjectRun instance.
	/// </summary>
	/// <param name="element">The <see cref="UIElement"/> to display.</param>
	/// <param name="ownerElement">The visual line element that hosts the UIElement.</param>
	public InlineObjectRun(UIElement element, InlineObjectElement ownerElement)
	{
		this.Element = element ?? throw new ArgumentNullException(nameof(element));
		this.OwnerElement = ownerElement ?? throw new ArgumentNullException(nameof(ownerElement));
	}

	/// <summary>
	/// Gets the element displayed by the InlineObjectRun.
	/// </summary>
	public UIElement Element { get; }

	/// <summary>
	/// Gets the visual line element that hosts the UIElement.
	/// </summary>
	public InlineObjectElement OwnerElement { get; }

	/// <summary>
	/// Gets the size the hosted element measured to when the run was created.
	/// </summary>
	public Size DesiredSize { get; internal set; }

	/// <summary>
	/// Gets the visual column at which the hosted element starts.
	/// </summary>
	public int VisualColumn { get; internal set; }

	/// <summary>
	/// Gets the VisualLine that contains this object. This property is only available after the object
	/// was added to the text view.
	/// </summary>
	public VisualLine? VisualLine { get; internal set; }
}
