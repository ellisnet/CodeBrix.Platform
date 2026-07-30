#nullable enable

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/IVisualLineTransformer.cs in the AvalonEdit
//repo (MIT).

/// <summary>
/// Allows transforming visual line elements.
/// </summary>
public interface IVisualLineTransformer
{
	/// <summary>
	/// Applies the transformation to the specified list of visual line elements.
	/// </summary>
	void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements);
}
