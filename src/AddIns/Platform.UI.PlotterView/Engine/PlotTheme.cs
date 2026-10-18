#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeBrix.Plotter;
using CodeBrix.Plotter.Axes;
using CodeBrix.Plotter.Legends;

namespace CodeBrix.Platform.UI.PlotterView.Engine;

/// <summary>
/// Makes a CodeBrix.Plotter model follow the view's light or dark theme for every colour the application has not set
/// (the PlotterView rule): the model's text colour, its plot-area border, each axis's line, tick and grid-line colours,
/// and the legends' hidden-series text. Every other text colour of the Plotter (titles, subtitles, axis labels and
/// titles, legend text and titles, minor ticks) defaults to <see cref="PlotterColors.Automatic"/>, which resolves through
/// the model's text colour or the axis's tick colour, so it follows the theme through them and is never written.
/// </summary>
/// <remarks>
/// <para>
/// NOT SET means: still the value CodeBrix.Plotter gave the property (read from a freshly created PlotModel, LinearAxis
/// and Legend), or the value this instance itself wrote there. Anything else is the application's colour and is never
/// touched, in either theme. The Plotter gives these properties concrete defaults (black, or translucent black for the
/// grid lines) rather than Automatic, so an application that explicitly sets one of them to exactly that default is
/// indistinguishable from one that left it alone.
/// </para>
/// <para>
/// The light theme is the Plotter's own look: under it this instance writes nothing except to put back a value it wrote
/// under the dark theme, so a chart that is never shown dark renders exactly as it always has. Under the dark theme each
/// not-set colour takes its light-on-dark twin: opaque black becomes the dark theme's foreground (white), and a
/// translucent black keeps its alpha in white.
/// </para>
/// <para>Not thread-safe: called under the model's SyncRoot from the paint.</para>
/// </remarks>
internal sealed class PlotTheme
{
	/// <summary>The dark theme's foreground (SystemBaseHighColor in the dark theme dictionary).</summary>
	internal static readonly PlotterColor DarkForeground = PlotterColors.White;

	private static readonly Lazy<Defaults> _defaults = new(() => new Defaults());

	// The colours this instance wrote, per element and property; an entry is dropped as soon as the value differs from
	// what was written (the application changed it), and the element's own value then wins.
	private readonly ConditionalWeakTable<object, Dictionary<string, PlotterColor>> _written = new();

	/// <summary>Whether the dark theme applies (false = the light theme, the Plotter's own colours).</summary>
	internal bool IsDark { get; set; }

	/// <summary>Applies the current theme to every not-set colour of <paramref name="model"/>, its axes and its legends.</summary>
	/// <param name="model">The model (after its Update, so default axes it created are included).</param>
	internal void Apply(PlotModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		var d = _defaults.Value;
		Slot(model, "TextColor", model.TextColor, d.TextColor, c => model.TextColor = c);
		Slot(model, "PlotAreaBorderColor", model.PlotAreaBorderColor, d.PlotAreaBorderColor, c => model.PlotAreaBorderColor = c);

		foreach (var axis in model.Axes)
		{
			Slot(axis, "AxislineColor", axis.AxislineColor, d.AxislineColor, c => axis.AxislineColor = c);
			Slot(axis, "TicklineColor", axis.TicklineColor, d.TicklineColor, c => axis.TicklineColor = c);
			Slot(axis, "MajorGridlineColor", axis.MajorGridlineColor, d.MajorGridlineColor, c => axis.MajorGridlineColor = c);
			Slot(axis, "MinorGridlineColor", axis.MinorGridlineColor, d.MinorGridlineColor, c => axis.MinorGridlineColor = c);
			Slot(axis, "ExtraGridlineColor", axis.ExtraGridlineColor, d.ExtraGridlineColor, c => axis.ExtraGridlineColor = c);
		}

		foreach (var legendBase in model.Legends)
		{
			if (legendBase is Legend legend)
			{
				Slot(legend, "SeriesInvisibleTextColor", legend.SeriesInvisibleTextColor, d.SeriesInvisibleTextColor, c => legend.SeriesInvisibleTextColor = c);
			}
		}
	}

	/// <summary>The colour a not-set property with the given Plotter default shows under the dark theme.</summary>
	/// <param name="lightDefault">The Plotter's default.</param>
	/// <returns>The dark twin: the dark foreground with the default's alpha when the default is black, opaque or
	/// translucent (the hidden-series legend text is translucent Automatic, which is 0,0,1); otherwise the default
	/// itself.</returns>
	internal static PlotterColor DarkTwin(PlotterColor lightDefault) =>
		lightDefault.A > 0 && lightDefault.R == 0 && lightDefault.G == 0 && lightDefault.B <= 1
			? PlotterColor.FromArgb(lightDefault.A, DarkForeground.R, DarkForeground.G, DarkForeground.B)
			: lightDefault;

	private void Slot(object element, string property, PlotterColor current, PlotterColor lightDefault, Action<PlotterColor> set)
	{
		var written = _written.GetOrCreateValue(element);
		var owned = written.TryGetValue(property, out var wrote) && wrote == current;
		if (!owned)
		{
			written.Remove(property);
		}

		if (IsDark)
		{
			var dark = DarkTwin(lightDefault);
			if (!owned && current == lightDefault && dark != lightDefault)
			{
				set(dark);
				written[property] = dark;
			}
		}
		else if (owned)
		{
			// Back to the Plotter's own colour; the element is the application's again.
			set(lightDefault);
			written.Remove(property);
		}
	}

	//The Plotter's own defaults, read from fresh instances so they follow the pinned CodeBrix.Plotter exactly.
	private sealed class Defaults
	{
		internal Defaults()
		{
			var model = new PlotModel();
			TextColor = model.TextColor;
			PlotAreaBorderColor = model.PlotAreaBorderColor;

			var axis = new LinearAxis();
			AxislineColor = axis.AxislineColor;
			TicklineColor = axis.TicklineColor;
			MajorGridlineColor = axis.MajorGridlineColor;
			MinorGridlineColor = axis.MinorGridlineColor;
			ExtraGridlineColor = axis.ExtraGridlineColor;

			SeriesInvisibleTextColor = new Legend().SeriesInvisibleTextColor;
		}

		internal PlotterColor TextColor { get; }
		internal PlotterColor PlotAreaBorderColor { get; }
		internal PlotterColor AxislineColor { get; }
		internal PlotterColor TicklineColor { get; }
		internal PlotterColor MajorGridlineColor { get; }
		internal PlotterColor MinorGridlineColor { get; }
		internal PlotterColor ExtraGridlineColor { get; }
		internal PlotterColor SeriesInvisibleTextColor { get; }
	}
}
