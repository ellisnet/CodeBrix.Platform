using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The chevron at the end of a bar that opens the flyout holding the items which did not fit.
/// </summary>
/// <remarks>
/// <para>
/// A bar creates one of these for itself when <see cref="ToolBar.OverflowMode"/> is
/// <see cref="OverflowMode.Chevron"/>, and hides it again as soon as everything fits. It is not
/// something an application adds to a bar; it is public because it is part of the bar's template
/// vocabulary and carries its own default style.
/// </para>
/// <para>
/// The chevron is drawn as a path in the button's foreground brush rather than as a glyph from a
/// symbol font, so it appears on a head with no symbol font installed and follows a theme change
/// with the rest of the bar.
/// </para>
/// </remarks>
public partial class ToolBarOverflowButton : ButtonBase
{
	/// <summary>Initializes a new chevron button.</summary>
	public ToolBarOverflowButton()
	{
		DefaultStyleKey = typeof(ToolBarOverflowButton);
	}
}
