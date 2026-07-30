#nullable enable

using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/GlobalTextRunProperties.cs in the AvalonEdit repo
//(MIT), where it was an internal WPF TextRunProperties subclass holding a Typeface. This framework
//has no TextRunProperties base and the text engine resolves fonts from a family name plus discrete
//weight/style/stretch values, so the class is now a plain public property bag: the Typeface field
//became FontFamily/FontWeight/FontStyle/FontStretch, and FontRenderingEmSize became FontSize.
//It is public because ITextRunConstructionContext exposes it; only the view (same assembly) sets it.

/// <summary>
/// The default text run properties of a text view: the font, size, colors and culture every
/// <see cref="VisualLineElement"/> starts from before transformers restyle it.
/// </summary>
public sealed class GlobalTextRunProperties
{
	internal GlobalTextRunProperties()
	{
	}

	/// <summary>
	/// Gets the font family name to resolve, or null for the platform default family.
	/// </summary>
	public string? FontFamily { get; internal set; }

	/// <summary>
	/// Gets the em size, in device-independent pixels.
	/// </summary>
	public double FontSize { get; internal set; } = 12.0;

	/// <summary>
	/// Gets the font weight.
	/// </summary>
	public FontWeight FontWeight { get; internal set; } = FontWeights.Normal;

	/// <summary>
	/// Gets the font style.
	/// </summary>
	public FontStyle FontStyle { get; internal set; } = FontStyle.Normal;

	/// <summary>
	/// Gets the font stretch.
	/// </summary>
	public FontStretch FontStretch { get; internal set; } = FontStretch.Normal;

	/// <summary>
	/// Gets the default foreground brush, or null to fall back to the drawing pass's default color.
	/// </summary>
	public Brush? ForegroundBrush { get; internal set; }

	/// <summary>
	/// Gets the default background brush, or null for no background.
	/// </summary>
	public Brush? BackgroundBrush { get; internal set; }

	/// <summary>
	/// Gets the culture used for culture-sensitive text operations.
	/// </summary>
	public CultureInfo CultureInfo { get; internal set; } = CultureInfo.CurrentCulture;
}
