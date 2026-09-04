using System;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The terse XAML form of an SVG icon source:
/// <c>Icon="{cb:SvgIconSource Source=ms-appx:///Assets/open.svg}"</c>.
/// </summary>
/// <remarks>
/// <para>
/// It produces an <see cref="SvgIconSource"/>, so everything the full element form can do is
/// available here too - a dark alternate, a tint, a tint mode and a size - written on one line. Use
/// the full <c>&lt;cb:SvgIconSource /&gt;</c> element form instead where a property has to be BOUND,
/// since a markup extension is evaluated once, when the XAML is parsed.
/// </para>
/// <para>
/// The name is the source's, not the element's, on purpose: this extension returns a SOURCE, and
/// <c>&lt;cb:SvgIcon /&gt;</c> is left free to mean the <see cref="SvgIcon"/> element it names.
/// </para>
/// <para>
/// A relative <see cref="Source"/> is read as <c>ms-appx:///</c>, so
/// <c>{cb:SvgIconSource Source=Assets/open.svg}</c> says the same thing as the absolute form.
/// </para>
/// </remarks>
[MarkupExtensionReturnType(ReturnType = typeof(SvgIconSource))]
public sealed class SvgIconSourceExtension : MarkupExtension
{
	/// <summary>The artwork's URI, absolute or relative to the application package.</summary>
	public string? Source { get; set; }

	/// <summary>The dark theme's artwork; optional.</summary>
	public string? Dark { get; set; }

	/// <summary>An SVG document written inline, used instead of <see cref="Source"/>.</summary>
	public string? Markup { get; set; }

	/// <summary>The colour to paint the artwork in; unset draws the file as drawn.</summary>
	public Brush? Tint { get; set; }

	/// <summary>How far <see cref="Tint"/> reaches into the artwork.</summary>
	public IconTintMode TintMode { get; set; } = IconTintMode.CurrentColorOnly;

	/// <summary>The icon's edge length in logical pixels; NaN takes the size from the bar.</summary>
	public double Size { get; set; } = double.NaN;

	/// <summary>Builds the icon source this extension describes.</summary>
	/// <returns>An <see cref="SvgIconSource"/>.</returns>
	protected override object ProvideValue() => CreateSource();

	/// <summary>The same source <see cref="ProvideValue"/> returns, reachable without the parser.</summary>
	/// <returns>An <see cref="SvgIconSource"/>.</returns>
	internal SvgIconSource CreateSource()
		=> new()
		{
			Source = IconUri.Parse(Source),
			Dark = IconUri.Parse(Dark),
			Markup = Markup,
			Tint = Tint,
			TintMode = TintMode,
			Size = Size,
		};
}
