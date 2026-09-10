using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reqnroll;
using SilverAssertions;
using Windows.UI;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps the Layout scenarios speak: putting a child inside a container, saying where one
/// element sits relative to another, and looking at part of a region rather than all of it.
/// <para>
/// Geometry here is read from the visual tree with NO inset, because these steps talk about
/// edges touching and gaps between elements, where a pixel of inset would be a lie. The
/// appearance steps keep the harness's usual inset, which is what keeps edge antialiasing out
/// of an assertion about a fill.
/// </para>
/// </summary>
[Binding]
public sealed class LayoutSteps
{
	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public LayoutSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

	/// <summary>
	/// Teaches the element factory the panels, shapes and visual properties the Layout
	/// scenarios name. Registration is idempotent and goes through the factory's public seam,
	/// so two coverage groups can add to the vocabulary at the same time.
	/// </summary>
	[BeforeScenario(Order = 1)]
	public static void Register_the_layout_vocabulary() => LayoutVocabulary.Ensure();

	// ------------------------------------------------------------- containers

	/// <summary>
	/// Shows one element whose kind starts with a vowel, with the properties a table lists. The
	/// harness's own sentence says "a {kind}", which reads wrongly for an Ellipse; this is the
	/// same step with the other article, so a requirement can be written in English.
	/// </summary>
	/// <param name="kind">The element kind.</param>
	/// <param name="name">The name the scenario refers to it by.</param>
	/// <param name="properties">A Property/Value table.</param>
	/// <returns>A task that completes once the element is showing.</returns>
	[Given("the application shows an {word} named {string} with:")]
	public async Task Given_the_application_shows_an_named_with_table(string kind, string name,
		DataTable properties)
	{
		var element = await ElementFactory.CreateAsync(kind, name, ReadProperties(properties)).ConfigureAwait(false);
		await TestTargetFixture.SetContentAsync(element).ConfigureAwait(false);
	}

	/// <summary>Puts a new element inside a container that is already showing.</summary>
	/// <param name="parentName">The Gherkin name of the container.</param>
	/// <param name="kind">The kind of element to build.</param>
	/// <param name="childName">The name the scenario will refer to the child by.</param>
	/// <returns>A task that completes once the child is in the tree and laid out.</returns>
	[Given("the layout {string} holds a {word} named {string}")]
	public async Task Given_the_layout_holds_a_named(string parentName, string kind, string childName) =>
		await AddChildAsync(parentName, kind, childName, Array.Empty<KeyValuePair<string, string>>())
			.ConfigureAwait(false);

	/// <summary>Puts a new element of a fixed size and colour inside a container.</summary>
	/// <param name="parentName">The Gherkin name of the container.</param>
	/// <param name="kind">The kind of element to build.</param>
	/// <param name="childName">The name the scenario will refer to the child by.</param>
	/// <param name="width">The child's width in device pixels.</param>
	/// <param name="height">The child's height in device pixels.</param>
	/// <param name="background">The colour to paint it.</param>
	/// <returns>A task that completes once the child is in the tree and laid out.</returns>
	[Given("the layout {string} holds a {word} named {string} {int} by {int} with Background {string}")]
	public async Task Given_the_layout_holds_a_named_sized(string parentName, string kind, string childName,
		int width, int height, Color background) =>
		await AddChildAsync(parentName, kind, childName, new[]
		{
			new KeyValuePair<string, string>("Width", Number(width)),
			new KeyValuePair<string, string>("Height", Number(height)),
			new KeyValuePair<string, string>("Background", Colors.Describe(background)),
		}).ConfigureAwait(false);

	/// <summary>Puts a new element inside a container, with the properties a table lists.</summary>
	/// <param name="parentName">The Gherkin name of the container.</param>
	/// <param name="kind">The kind of element to build.</param>
	/// <param name="childName">The name the scenario will refer to the child by.</param>
	/// <param name="properties">A Property/Value table.</param>
	/// <returns>A task that completes once the child is in the tree and laid out.</returns>
	[Given("the layout {string} holds a {word} named {string} with:")]
	public async Task Given_the_layout_holds_a_named_with_table(string parentName, string kind, string childName,
		DataTable properties) =>
		await AddChildAsync(parentName, kind, childName, ReadProperties(properties)).ConfigureAwait(false);

	// --------------------------------------------------- positions in the tree

	/// <summary>Asserts that one element's bottom edge is another's top edge.</summary>
	/// <param name="upper">The element that must be on top.</param>
	/// <param name="lower">The element that must be below it.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} sits directly above {string}")]
	public async Task Then_sits_directly_above(string upper, string lower)
	{
		var first = await RectAsync(upper).ConfigureAwait(false);
		var second = await RectAsync(lower).ConfigureAwait(false);

		second.Y.Should().Be(first.Bottom,
			"\"{0}\" {1} must end exactly where \"{2}\" {3} begins", upper, first, lower, second);
	}

	/// <summary>Asserts that one element's right edge is another's left edge.</summary>
	/// <param name="left">The element that must be on the left.</param>
	/// <param name="right">The element that must be to its right.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} sits directly left of {string}")]
	public async Task Then_sits_directly_left_of(string left, string right)
	{
		var first = await RectAsync(left).ConfigureAwait(false);
		var second = await RectAsync(right).ConfigureAwait(false);

		second.X.Should().Be(first.Right,
			"\"{0}\" {1} must end exactly where \"{2}\" {3} begins", left, first, right, second);
	}

	/// <summary>Asserts how big the gap between two elements is, along the axis they are stacked on.</summary>
	/// <param name="gap">The gap in device pixels.</param>
	/// <param name="first">One element.</param>
	/// <param name="second">The element after it.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("there is a gap of {int} pixels between {string} and {string}")]
	public async Task Then_there_is_a_gap_of_pixels_between(int gap, string first, string second)
	{
		var one = await RectAsync(first).ConfigureAwait(false);
		var other = await RectAsync(second).ConfigureAwait(false);
		var vertical = other.Y >= one.Bottom;
		var actual = vertical ? other.Y - one.Bottom : other.X - one.Right;

		actual.Should().Be(gap,
			"the {0} gap between \"{1}\" {2} and \"{3}\" {4} was asserted",
			vertical ? "vertical" : "horizontal", first, one, second, other);
	}

	/// <summary>Asserts where one element's top left corner is inside another element.</summary>
	/// <param name="child">The element that was placed.</param>
	/// <param name="offsetX">How far right of the container's left edge it must be.</param>
	/// <param name="offsetY">How far below the container's top edge it must be.</param>
	/// <param name="parent">The container.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the top left of {string} is {int}, {int} inside {string}")]
	public async Task Then_the_top_left_of_is_inside(string child, int offsetX, int offsetY, string parent)
	{
		var inner = await RectAsync(child).ConfigureAwait(false);
		var outer = await RectAsync(parent).ConfigureAwait(false);

		Point(inner.X - outer.X, inner.Y - outer.Y).Should().Be(Point(offsetX, offsetY),
			"\"{0}\" {1} was placed inside \"{2}\" {3}", child, inner, parent, outer);
	}

	/// <summary>Asserts that an element sits the same distance in from all four edges of another.</summary>
	/// <param name="child">The element that was inset.</param>
	/// <param name="inset">How far in it must be, on every side.</param>
	/// <param name="parent">The container.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} is inset {int} pixels inside {string}")]
	public async Task Then_is_inset_pixels_inside(string child, int inset, string parent)
	{
		var inner = await RectAsync(child).ConfigureAwait(false);
		var outer = await RectAsync(parent).ConfigureAwait(false);
		var actual = Insets(inner, outer);

		actual.Should().Be(Insets(inset), "\"{0}\" {1} sits inside \"{2}\" {3}", child, inner, parent, outer);
	}

	/// <summary>Asserts which edge of a container an element was aligned to.</summary>
	/// <param name="child">The element that was aligned.</param>
	/// <param name="edge">left, right, top, bottom or centre.</param>
	/// <param name="parent">The container.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} is aligned to the {word} of {string}")]
	public async Task Then_is_aligned_to_the_of(string child, string edge, string parent)
	{
		var inner = await RectAsync(child).ConfigureAwait(false);
		var outer = await RectAsync(parent).ConfigureAwait(false);
		var (actual, expected) = edge.ToUpperInvariant() switch
		{
			"LEFT" => (inner.X, outer.X),
			"RIGHT" => (inner.Right, outer.Right),
			"TOP" => (inner.Y, outer.Y),
			"BOTTOM" => (inner.Bottom, outer.Bottom),
			"CENTRE" or "CENTER" => (inner.X - outer.X, outer.Right - inner.Right),
			_ => throw new FormatException(
				$"\"{edge}\" is not an edge. Write left, right, top, bottom or centre."),
		};

		actual.Should().Be(expected,
			"\"{0}\" {1} must be aligned to the {2} of \"{3}\" {4}", child, inner, edge, parent, outer);
	}

	// ------------------------------------------------------- part of a region

	/// <summary>Asserts that the four corners of an element's region are the panel background.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the corner pixels of {string} are the panel background")]
	public async Task Then_the_corner_pixels_of_are_the_panel_background(string name) =>
		(await RegionAsync(name).ConfigureAwait(false)).CornerPixelsAre(CanvasAssert.Background);

	/// <summary>Asserts what colour the four corners of an element's region are.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour every corner must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the corner pixels of {string} are {string}")]
	public async Task Then_the_corner_pixels_of_are(string name, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).CornerPixelsAre(color);

	/// <summary>Asserts that half of an element's region is painted one colour.</summary>
	/// <param name="half">left, right, top or bottom.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour it must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} half of the region of {string} is uniformly {string}")]
	public async Task Then_the_half_of_the_region_of_is_uniformly(string half, string name, Color color) =>
		Half(await RegionAsync(name).ConfigureAwait(false), half).IsUniformly(color);

	/// <summary>Asserts that half of an element's region is nothing but panel background.</summary>
	/// <param name="half">left, right, top or bottom.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} half of the region of {string} is blank")]
	public async Task Then_the_half_of_the_region_of_is_blank(string half, string name) =>
		Half(await RegionAsync(name).ConfigureAwait(false), half).IsBlank();

	/// <summary>Asserts that a colour covers at least a share of half of an element's region.</summary>
	/// <param name="half">left, right, top or bottom.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="percent">The share, as a percentage.</param>
	/// <param name="color">The colour that must be present.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} half of the region of {string} contains at least {float} percent {string}")]
	public async Task Then_the_half_of_the_region_of_contains_at_least_percent(string half, string name,
		float percent, Color color) =>
		Half(await RegionAsync(name).ConfigureAwait(false), half).Contains(color, percent / 100.0);

	/// <summary>Asserts that a colour covers at most a share of half of an element's region.</summary>
	/// <param name="half">left, right, top or bottom.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="percent">The share, as a percentage.</param>
	/// <param name="color">The colour that must be scarce.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} half of the region of {string} contains at most {float} percent {string}")]
	public async Task Then_the_half_of_the_region_of_contains_at_most_percent(string half, string name,
		float percent, Color color) =>
		Half(await RegionAsync(name).ConfigureAwait(false), half).DoesNotContain(color, percent / 100.0);

	/// <summary>Asserts that something was drawn in half of an element's region.</summary>
	/// <param name="half">left, right, top or bottom.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} half of the region of {string} has ink")]
	public async Task Then_the_half_of_the_region_of_has_ink(string half, string name) =>
		Half(await RegionAsync(name).ConfigureAwait(false), half).HasInk();

	// ------------------------------------------------------- ink across frames

	/// <summary>Asserts how far an element's ink moved between two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element whose region is watched.</param>
	/// <param name="deltaX">How far right the ink must have moved.</param>
	/// <param name="deltaY">How far down the ink must have moved.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ink of {string} moved by {int}, {int} pixels between frames {string} and {string}")]
	public async Task Then_the_ink_of_moved_by_pixels_between_frames(string name, int deltaX, int deltaY,
		string otherFrameName, string frameName)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		region.InkMovedBy(other, deltaX, deltaY);
	}

	/// <summary>Asserts what an element's ink was scaled by between two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element whose region is watched.</param>
	/// <param name="scaleX">The factor the ink's width must have grown by.</param>
	/// <param name="scaleY">The factor the ink's height must have grown by.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <param name="frameName">The later frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ink of {string} scaled by {float} and {float} between frames {string} and {string}")]
	public async Task Then_the_ink_of_scaled_by_between_frames(string name, float scaleX, float scaleY,
		string otherFrameName, string frameName)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		region.InkScaledBy(other, scaleX, scaleY);
	}

	/// <summary>Asserts that an element's region holds more ink than it did in an earlier frame.</summary>
	/// <param name="name">The Gherkin name of the element whose region is watched.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} in frame {string} holds more ink than in frame {string}")]
	public async Task Then_the_region_of_in_frame_holds_more_ink_than_in_frame(string name, string frameName,
		string otherFrameName)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		region.HasMoreInkThan(other);
	}

	// --------------------------------------------------------------- inner

	private Task<Region> RegionAsync(string elementName, string frameName = ScenarioFrames.CurrentFrameName) =>
		ScenarioFrames.RegionAsync(_scenarioContext, elementName, frameName);

	private static Region Half(Region region, string half) => half.ToUpperInvariant() switch
	{
		"LEFT" => region.LeftHalf(),
		"RIGHT" => region.RightHalf(),
		"TOP" => region.TopHalf(),
		"BOTTOM" => region.BottomHalf(),
		_ => throw new FormatException($"\"{half}\" is not a half. Write left, right, top or bottom."),
	};

	private static async Task<DeviceRect> RectAsync(string name) =>
		await DeviceRect.OfAsync(ElementRegistry.Resolve(name), inset: 0).ConfigureAwait(false);

	private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

	private static string Point(int x, int y) => string.Create(CultureInfo.InvariantCulture, $"({x},{y})");

	private static string Insets(DeviceRect inner, DeviceRect outer) => string.Create(CultureInfo.InvariantCulture,
		$"left {inner.X - outer.X}, top {inner.Y - outer.Y}, right {outer.Right - inner.Right}, bottom {outer.Bottom - inner.Bottom}");

	private static string Insets(int inset) => string.Create(CultureInfo.InvariantCulture,
		$"left {inset}, top {inset}, right {inset}, bottom {inset}");

	private static async Task AddChildAsync(string parentName, string kind, string childName,
		IEnumerable<KeyValuePair<string, string>> properties)
	{
		// A child of a container fills its slot unless the scenario says otherwise: the factory
		// centres a stand-alone element, which is the wrong default inside a Grid cell.
		var settings = new List<KeyValuePair<string, string>>
		{
			new("HorizontalAlignment", "Stretch"),
			new("VerticalAlignment", "Stretch"),
		};
		settings.AddRange(properties);

		var child = await ElementFactory.CreateAsync(kind, childName, settings).ConfigureAwait(false);
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var parent = ElementRegistry.Resolve(parentName);
			switch (parent)
			{
				case Panel panel:
					panel.Children.Add(child);
					break;
				case Border border:
					border.Child = child;
					break;
				case ContentControl content:
					content.Content = child;
					break;
				default:
					throw new NotSupportedException(
						$"A {parent.GetType().Name} named \"{parentName}\" cannot hold a child.");
			}

			VirtualApplication.Instance.Root.UpdateLayout();
		}).ConfigureAwait(false);
	}

	private static IEnumerable<KeyValuePair<string, string>> ReadProperties(DataTable table)
	{
		ArgumentNullException.ThrowIfNull(table);

		return table.Rows.Select(row => new KeyValuePair<string, string>(
			row[table.Header.First()],
			row[table.Header.Last()]));
	}
}
