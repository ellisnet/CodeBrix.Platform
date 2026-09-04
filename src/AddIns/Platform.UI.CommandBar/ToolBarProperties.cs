using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The bar-level presentation settings every tool bar item reads: icon size, label mode, label
/// position, and whether tooltips are shown.
/// </summary>
/// <remarks>
/// <para>
/// All four are INHERITED attached properties. Set one on a <see cref="ToolBarTray"/>, a
/// <see cref="ToolBar"/>, a <see cref="ToolBarGroup"/> - or on any element above them, up to the
/// page - and every item below picks it up; set it again on a single item and that item wins. That
/// is the whole point of the class: a bar says "24 pixel icons, no text" once, and a button that
/// must differ says so once, and no plumbing is written in between.
/// </para>
/// <para>
/// The attached form is the one to use above a bar - to drive a whole page of bars at once - and
/// the one to use for a per-item override. A <see cref="ToolBar"/> that offers the same settings
/// as ordinary properties does so by setting these attached values on itself, so the two forms
/// never disagree.
/// </para>
/// </remarks>
public static class ToolBarProperties
{
	/// <summary>The icon size a tool bar item uses when nothing nearer overrides it: 24 logical
	/// pixels.</summary>
	public const double DefaultIconSize = 24d;

	/// <summary>
	/// Identifies the ToolBarProperties.IconSize inherited attached property: the edge length, in
	/// logical pixels, of an item's icon.
	/// </summary>
	/// <remarks>
	/// An icon is rasterised at this size multiplied by the XamlRoot's rasterization scale, so it
	/// stays crisp at a fractional display scale rather than being drawn at 24 and stretched.
	/// </remarks>
	public static readonly DependencyProperty IconSizeProperty =
		DependencyProperty.RegisterAttached(
			"IconSize",
			typeof(double),
			typeof(ToolBarProperties),
			new FrameworkPropertyMetadata(DefaultIconSize, FrameworkPropertyMetadataOptions.Inherits));

	/// <summary>
	/// Identifies the ToolBarProperties.LabelMode inherited attached property: whether items show
	/// their icon, their text, or both.
	/// </summary>
	public static readonly DependencyProperty LabelModeProperty =
		DependencyProperty.RegisterAttached(
			"LabelMode",
			typeof(LabelMode),
			typeof(ToolBarProperties),
			new FrameworkPropertyMetadata(LabelMode.IconOnly, FrameworkPropertyMetadataOptions.Inherits));

	/// <summary>
	/// Identifies the ToolBarProperties.LabelPosition inherited attached property: where an item's
	/// text sits relative to its icon when both are shown.
	/// </summary>
	public static readonly DependencyProperty LabelPositionProperty =
		DependencyProperty.RegisterAttached(
			"LabelPosition",
			typeof(LabelPosition),
			typeof(ToolBarProperties),
			new FrameworkPropertyMetadata(LabelPosition.Right, FrameworkPropertyMetadataOptions.Inherits));

	/// <summary>
	/// Identifies the ToolBarProperties.ShowToolTips inherited attached property: whether items
	/// show the tooltip composed from their text and shortcut.
	/// </summary>
	/// <remarks>
	/// True by default, which is what makes an icon-only bar usable. Setting it false on a bar
	/// silences every item in that bar; setting it true again on one item brings that item's
	/// tooltip back. Turning tooltips off does NOT change the accessibility name - a screen reader
	/// still reads the item's text.
	/// </remarks>
	public static readonly DependencyProperty ShowToolTipsProperty =
		DependencyProperty.RegisterAttached(
			"ShowToolTips",
			typeof(bool),
			typeof(ToolBarProperties),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

	/// <summary>Reads the effective icon size for <paramref name="element"/>.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>The icon edge length in logical pixels.</returns>
	public static double GetIconSize(DependencyObject element)
		=> (double)element.GetValue(IconSizeProperty);

	/// <summary>Sets the icon size for <paramref name="element"/> and everything below it.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">The icon edge length in logical pixels.</param>
	public static void SetIconSize(DependencyObject element, double value)
		=> element.SetValue(IconSizeProperty, value);

	/// <summary>Reads the effective label mode for <paramref name="element"/>.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>Which of the icon and the text are shown.</returns>
	public static LabelMode GetLabelMode(DependencyObject element)
		=> (LabelMode)element.GetValue(LabelModeProperty);

	/// <summary>Sets the label mode for <paramref name="element"/> and everything below it.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">Which of the icon and the text to show.</param>
	public static void SetLabelMode(DependencyObject element, LabelMode value)
		=> element.SetValue(LabelModeProperty, value);

	/// <summary>Reads the effective label position for <paramref name="element"/>.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>Where the text sits relative to the icon.</returns>
	public static LabelPosition GetLabelPosition(DependencyObject element)
		=> (LabelPosition)element.GetValue(LabelPositionProperty);

	/// <summary>Sets the label position for <paramref name="element"/> and everything below
	/// it.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">Where the text should sit relative to the icon.</param>
	public static void SetLabelPosition(DependencyObject element, LabelPosition value)
		=> element.SetValue(LabelPositionProperty, value);

	/// <summary>Reads whether <paramref name="element"/> shows tooltips.</summary>
	/// <param name="element">The element to read the value from.</param>
	/// <returns>True when the composed tooltip is shown.</returns>
	public static bool GetShowToolTips(DependencyObject element)
		=> (bool)element.GetValue(ShowToolTipsProperty);

	/// <summary>Sets whether <paramref name="element"/> and everything below it show
	/// tooltips.</summary>
	/// <param name="element">The element to set the value on.</param>
	/// <param name="value">True to show the composed tooltip, false to suppress it.</param>
	public static void SetShowToolTips(DependencyObject element, bool value)
		=> element.SetValue(ShowToolTipsProperty, value);
}
