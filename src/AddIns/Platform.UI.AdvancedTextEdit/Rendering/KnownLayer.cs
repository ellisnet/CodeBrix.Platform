#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/LayerPosition.cs in the AvalonEdit repo (MIT),
//which also contained the internal attached-property LayerPosition class used to sort custom
//UIElement layers into TextView.Layers. UIElement layer insertion is not part of this port -
//every known layer is a draw phase on the text view's single Skia surface, and
//IBackgroundRenderer is the extension point on all of them - so only the two enums were ported.

/// <summary>
/// An enumeration of well-known layers.
/// </summary>
public enum KnownLayer
{
	/// <summary>
	/// This layer is in the background.
	/// It is drawn directly on the text view's surface before all other layers.
	/// It is not possible to replace the background layer or insert new layers below it.
	/// </summary>
	/// <remarks>This layer is below the Selection layer.</remarks>
	Background,
	/// <summary>
	/// This layer contains the selection rectangle.
	/// </summary>
	/// <remarks>This layer is between the Background and the Text layers.</remarks>
	Selection,
	/// <summary>
	/// This layer contains the text and inline objects.
	/// </summary>
	/// <remarks>This layer is between the Selection and the Caret layers.</remarks>
	Text,
	/// <summary>
	/// This layer contains the blinking caret.
	/// </summary>
	/// <remarks>This layer is above the Text layer.</remarks>
	Caret
}

/// <summary>
/// Specifies where a new layer is inserted, in relation to an old layer.
/// </summary>
public enum LayerInsertionPosition
{
	/// <summary>
	/// The new layer is inserted below the specified layer.
	/// </summary>
	Below,
	/// <summary>
	/// The new layer replaces the specified layer.
	/// </summary>
	Replace,
	/// <summary>
	/// The new layer is inserted above the specified layer.
	/// </summary>
	Above
}
