using System;
using System.Globalization;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Reqnroll;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The geometry steps (WPE1-12): a Path whose Data is an EllipseGeometry or a LineGeometry, declared in XAML (so the
/// XAML reader creates the geometry and sets its properties, as an application's markup does), and a change to one of
/// the geometry's own properties after the Path is showing. Every pattern names the geometry kind, so no other coverage
/// group's sentence can match one of them.
/// </summary>
[Binding]
public sealed class GeometrySteps
{
	/// <summary>Shows a Path whose Data is an EllipseGeometry, filled with a colour.</summary>
	/// <param name="name">The name the scenario refers to the Path by.</param>
	/// <param name="centerX">The x of the ellipse's Center.</param>
	/// <param name="centerY">The y of the ellipse's Center.</param>
	/// <param name="radiusX">The ellipse's RadiusX.</param>
	/// <param name="radiusY">The ellipse's RadiusY.</param>
	/// <param name="fill">The colour name the Path is filled with.</param>
	/// <returns>A task that completes once the Path is showing.</returns>
	[Given("the application shows a Path named {string} whose Data is an EllipseGeometry centred at {int},{int} with radii {int} and {int} filled {string}")]
	public async Task Given_a_Path_with_an_EllipseGeometry(string name, int centerX, int centerY, int radiusX, int radiusY, string fill) =>
		await ShowPathAsync(name, string.Create(CultureInfo.InvariantCulture,
			$"""<EllipseGeometry Center="{centerX},{centerY}" RadiusX="{radiusX}" RadiusY="{radiusY}" />"""),
			$"""Fill="{fill}" """).ConfigureAwait(false);

	/// <summary>Shows a Path whose Data is a LineGeometry, stroked with a colour.</summary>
	/// <param name="name">The name the scenario refers to the Path by.</param>
	/// <param name="startX">The x of the line's StartPoint.</param>
	/// <param name="startY">The y of the line's StartPoint.</param>
	/// <param name="endX">The x of the line's EndPoint.</param>
	/// <param name="endY">The y of the line's EndPoint.</param>
	/// <param name="stroke">The colour name of the Path's Stroke.</param>
	/// <param name="thickness">The Path's StrokeThickness.</param>
	/// <returns>A task that completes once the Path is showing.</returns>
	[Given("the application shows a Path named {string} whose Data is a LineGeometry from {int},{int} to {int},{int} stroked {string} {int} thick")]
	public async Task Given_a_Path_with_a_LineGeometry(string name, int startX, int startY, int endX, int endY, string stroke, int thickness) =>
		await ShowPathAsync(name, string.Create(CultureInfo.InvariantCulture,
			$"""<LineGeometry StartPoint="{startX},{startY}" EndPoint="{endX},{endY}" />"""),
			string.Create(CultureInfo.InvariantCulture, $"""Stroke="{stroke}" StrokeThickness="{thickness}" """)).ConfigureAwait(false);

	/// <summary>
	/// Changes one property of the geometry (not of the Path) that a Path shows: RadiusX / RadiusY take a number,
	/// Center / StartPoint / EndPoint take "x,y".
	/// </summary>
	/// <param name="property">The geometry property.</param>
	/// <param name="name">The name of the Path.</param>
	/// <param name="value">The new value.</param>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	[When("the {word} of the geometry of the Path {string} is set to {string}")]
	public async Task When_a_geometry_property_is_set(string property, string name, string value) =>
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var data = ((Path)ElementRegistry.Resolve(name)).Data;
			switch (data, property)
			{
				case (EllipseGeometry ellipse, nameof(EllipseGeometry.RadiusX)):
					ellipse.RadiusX = double.Parse(value, CultureInfo.InvariantCulture);
					break;
				case (EllipseGeometry ellipse, nameof(EllipseGeometry.RadiusY)):
					ellipse.RadiusY = double.Parse(value, CultureInfo.InvariantCulture);
					break;
				case (EllipseGeometry ellipse, nameof(EllipseGeometry.Center)):
					ellipse.Center = ParsePoint(value);
					break;
				case (LineGeometry line, nameof(LineGeometry.StartPoint)):
					line.StartPoint = ParsePoint(value);
					break;
				case (LineGeometry line, nameof(LineGeometry.EndPoint)):
					line.EndPoint = ParsePoint(value);
					break;
				default:
					throw new ArgumentException($"The geometry of \"{name}\" ({data?.GetType().Name}) has no settable {property} here.");
			}
		}).ConfigureAwait(false);

	private static async Task ShowPathAsync(string name, string geometryXaml, string pathAttributes)
	{
		Grid page = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			page = (Grid)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				$"""
				<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				  <Path {pathAttributes} HorizontalAlignment="Center" VerticalAlignment="Center">
				    <Path.Data>
				      {geometryXaml}
				    </Path.Data>
				  </Path>
				</Grid>
				""");
			ElementRegistry.Register(name, (FrameworkElement)page.Children[0]);
		}).ConfigureAwait(false);

		await TestTargetFixture.SetContentAsync(page).ConfigureAwait(false);
	}

	private static Point ParsePoint(string value)
	{
		var parts = value.Split(',');
		return new Point(
			double.Parse(parts[0], CultureInfo.InvariantCulture),
			double.Parse(parts[1], CultureInfo.InvariantCulture));
	}
}
