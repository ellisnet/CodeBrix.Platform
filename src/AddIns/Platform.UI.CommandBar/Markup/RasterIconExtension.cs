using System;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The terse XAML form of a bitmap icon:
/// <c>Icon="{cb:RasterIcon Source=ms-appx:///Assets/open.png}"</c>.
/// </summary>
/// <remarks>
/// It produces a <see cref="RasterIconSource"/>. Use the full <c>&lt;cb:RasterIconSource /&gt;</c>
/// element form where a property has to be BOUND, since a markup extension is evaluated once, when
/// the XAML is parsed. A relative <see cref="Source"/> is read as <c>ms-appx:///</c>.
/// </remarks>
[MarkupExtensionReturnType(ReturnType = typeof(RasterIconSource))]
public sealed class RasterIconExtension : MarkupExtension
{
	/// <summary>The artwork's URI, absolute or relative to the application package.</summary>
	public string? Source { get; set; }

	/// <summary>The dark theme's artwork; optional.</summary>
	public string? Dark { get; set; }

	/// <summary>The colour to paint the image's alpha with; unset draws the image as it is.</summary>
	public Brush? Tint { get; set; }

	/// <summary>The icon's edge length in logical pixels; NaN takes the size from the bar.</summary>
	public double Size { get; set; } = double.NaN;

	/// <summary>Builds the icon source this extension describes.</summary>
	/// <returns>A <see cref="RasterIconSource"/>.</returns>
	protected override object ProvideValue() => CreateSource();

	/// <summary>The same source <see cref="ProvideValue"/> returns, reachable without the parser.</summary>
	/// <returns>A <see cref="RasterIconSource"/>.</returns>
	internal RasterIconSource CreateSource()
		=> new()
		{
			Source = IconUri.Parse(Source),
			Dark = IconUri.Parse(Dark),
			Tint = Tint,
			Size = Size,
		};
}
