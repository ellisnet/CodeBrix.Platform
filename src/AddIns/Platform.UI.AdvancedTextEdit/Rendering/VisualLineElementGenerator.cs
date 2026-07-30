#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineElementGenerator.cs in the AvalonEdit
//repo (MIT). The internal IBuiltinElementGenerator interface now takes this port's
//AdvancedTextEditOptions (upstream: TextEditorOptions); CurrentContext is honestly nullable
//because it is only set between StartGeneration and FinishGeneration.

/// <summary>
/// Abstract base class for generators that produce new visual line elements.
/// </summary>
public abstract class VisualLineElementGenerator
{
	/// <summary>
	/// Gets the text run construction context. Only non-null between
	/// <see cref="StartGeneration"/> and <see cref="FinishGeneration"/> calls.
	/// </summary>
	protected ITextRunConstructionContext? CurrentContext { get; private set; }

	/// <summary>
	/// Initializes the generator for the <see cref="ITextRunConstructionContext"/>
	/// </summary>
	public virtual void StartGeneration(ITextRunConstructionContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		this.CurrentContext = context;
	}

	/// <summary>
	/// De-initializes the generator.
	/// </summary>
	public virtual void FinishGeneration()
	{
		this.CurrentContext = null;
	}

	/// <summary>
	/// Should only be used by VisualLine.ConstructVisualElements.
	/// </summary>
	internal int cachedInterest;

	/// <summary>
	/// Gets the first offset >= startOffset where the generator wants to construct an element.
	/// Return -1 to signal no interest.
	/// </summary>
	public abstract int GetFirstInterestedOffset(int startOffset);

	/// <summary>
	/// Constructs an element at the specified offset.
	/// May return null if no element should be constructed.
	/// </summary>
	/// <remarks>
	/// Avoid signalling interest and then building no element by returning null - doing so
	/// causes the generated <see cref="VisualLineText"/> elements to be unnecessarily split
	/// at the position where you signalled interest.
	/// </remarks>
	public abstract VisualLineElement? ConstructElement(int offset);
}

/// <summary>
/// Implemented by the built-in element generators so the text view can push option changes to them.
/// </summary>
internal interface IBuiltinElementGenerator
{
	void FetchOptions(AdvancedTextEditOptions options);
}
