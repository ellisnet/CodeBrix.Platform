using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The base class of every icon a tool bar item can show.
/// </summary>
/// <remarks>
/// <para>
/// It derives from <see cref="IconSource"/>, so the same icon object also works anywhere the
/// framework takes an icon source - an <c>IconSourceElement</c>, or an <c>AppBarButton.Icon</c> in
/// a WinUI-shaped command bar - and one icon story therefore covers both vocabularies.
/// </para>
/// <para>
/// The concrete kinds are supplied by this add-in: an SVG source rendered through the platform's
/// SVG route, and a raster source for PNG and the other formats the platform's image decoder
/// reads. Both re-render when the theme or the display scale changes, which is why an icon is a
/// source object rather than a fixed bitmap.
/// </para>
/// </remarks>
public abstract class ToolIconSource : IconSource
{
	/// <summary>Initializes the base state shared by every tool bar icon source.</summary>
	protected ToolIconSource()
	{
	}
}
