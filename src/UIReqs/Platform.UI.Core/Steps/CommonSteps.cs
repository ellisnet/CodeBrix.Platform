using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Reqnroll;
using SilverAssertions;
using Windows.UI;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps every coverage group shares: showing something, changing it, capturing a frame,
/// tapping, and the assertions about what a region looks like. A later group reuses these
/// rather than writing a near-duplicate sentence, because two step definitions that both match
/// one sentence make the scenario fail as ambiguous.
/// </summary>
[Binding]
public sealed class CommonSteps
{
	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public CommonSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

	// ------------------------------------------------------------- content

	/// <summary>Shows an empty panel.</summary>
	/// <returns>A task that completes once the panel is empty.</returns>
	[Given("the application shows nothing")]
	public async Task Given_the_application_shows_nothing()
	{
		ElementRegistry.Clear();
		await TestTargetFixture.ClearContentAsync().ConfigureAwait(false);
	}

	/// <summary>Shows one element at its natural size.</summary>
	/// <param name="kind">The element kind, as ElementFactory spells it.</param>
	/// <param name="name">The name the scenario refers to it by.</param>
	/// <returns>A task that completes once the element is showing.</returns>
	[Given("the application shows a {word} named {string}")]
	public async Task Given_the_application_shows_a_named(string kind, string name) =>
		await ShowAsync(kind, name, null, null, null).ConfigureAwait(false);

	/// <summary>Shows one element at a fixed size.</summary>
	/// <param name="kind">The element kind.</param>
	/// <param name="name">The name the scenario refers to it by.</param>
	/// <param name="width">The width in logical pixels, which at scale 1.0 are device pixels.</param>
	/// <param name="height">The height in logical pixels.</param>
	/// <returns>A task that completes once the element is showing.</returns>
	[Given("the application shows a {word} named {string} {int} by {int}")]
	public async Task Given_the_application_shows_a_named_sized(string kind, string name, int width, int height) =>
		await ShowAsync(kind, name, width, height, null).ConfigureAwait(false);

	/// <summary>Shows one element at a fixed size, painted a colour.</summary>
	/// <param name="kind">The element kind.</param>
	/// <param name="name">The name the scenario refers to it by.</param>
	/// <param name="width">The width in logical pixels.</param>
	/// <param name="height">The height in logical pixels.</param>
	/// <param name="background">The colour to paint it.</param>
	/// <returns>A task that completes once the element is showing.</returns>
	[Given("the application shows a {word} named {string} {int} by {int} with Background {string}")]
	public async Task Given_the_application_shows_a_named_sized_with_background(
		string kind, string name, int width, int height, Color background) =>
		await ShowAsync(kind, name, width, height, background).ConfigureAwait(false);

	/// <summary>Shows one element with the properties a table lists.</summary>
	/// <param name="kind">The element kind.</param>
	/// <param name="name">The name the scenario refers to it by.</param>
	/// <param name="properties">A Property/Value table.</param>
	/// <returns>A task that completes once the element is showing.</returns>
	[Given("the application shows a {word} named {string} with:")]
	public async Task Given_the_application_shows_a_named_with_table(string kind, string name, DataTable properties)
	{
		var element = await ElementFactory.CreateAsync(kind, name, ReadProperties(properties)).ConfigureAwait(false);
		await TestTargetFixture.SetContentAsync(element).ConfigureAwait(false);
	}

	/// <summary>Changes one property of an element that is already showing.</summary>
	/// <param name="property">The property to set.</param>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="value">The value to set it to.</param>
	/// <returns>A task that completes once the UI thread has applied the change.</returns>
	[Given("the {word} of {string} is set to {string}")]
	[When("the {word} of {string} is set to {string}")]
	public async Task When_the_property_of_is_set_to(string property, string name, string value)
	{
		_scenarioContext[LastChangeKey] = $"{property} of \"{name}\" -> {value}";
		await ElementFactory.ApplyAsync(name, property, value).ConfigureAwait(false);
	}

	// -------------------------------------------------------------- frames

	/// <summary>Captures the frame that shows everything the scenario has done so far.</summary>
	/// <returns>A task that completes when the frame has arrived.</returns>
	[When("the frame is captured")]
	public async Task When_the_frame_is_captured() =>
		await ScenarioFrames.CaptureAsync(_scenarioContext, ScenarioFrames.CurrentFrameName).ConfigureAwait(false);

	/// <summary>Captures a frame and gives it a name, so a later step can compare against it.</summary>
	/// <param name="frameName">The name to give the frame.</param>
	/// <returns>A task that completes when the frame has arrived.</returns>
	[When("the frame is captured as {string}")]
	public async Task When_the_frame_is_captured_as(string frameName) =>
		await ScenarioFrames.CaptureAsync(_scenarioContext, frameName).ConfigureAwait(false);

	// --------------------------------------------------------------- input

	/// <summary>Taps the centre of an element, which is where a finger would land.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes once the tap has been delivered and the UI thread is idle.</returns>
	[When("{string} is tapped")]
	public async Task When_is_tapped(string name)
	{
		var element = ElementRegistry.Resolve(name);
		var bounds = await DeviceRect.OfAsync(element).ConfigureAwait(false);
		var (x, y) = bounds.Center;
		_scenarioContext[LastChangeKey] = string.Create(CultureInfo.InvariantCulture,
			$"tap on \"{name}\" at ({x},{y})");
		TestTargetFixture.Session.Tap(x, y);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	/// <summary>Taps one point of the panel.</summary>
	/// <param name="x">The x coordinate, in device pixels.</param>
	/// <param name="y">The y coordinate, in device pixels.</param>
	/// <returns>A task that completes once the tap has been delivered and the UI thread is idle.</returns>
	[When("the point {int}, {int} is tapped")]
	public async Task When_the_point_is_tapped(int x, int y)
	{
		_scenarioContext[LastChangeKey] = string.Create(CultureInfo.InvariantCulture, $"tap at ({x},{y})");
		TestTargetFixture.Session.Tap(x, y);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	// ---------------------------------------------- appearance, one frame

	/// <summary>Asserts that an element's region is painted one colour throughout.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour it must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} is uniformly {string}")]
	public async Task Then_the_region_of_is_uniformly(string name, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).IsUniformly(color);

	/// <summary>Asserts that a colour is present in an element's region.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour that must be present.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} contains {string}")]
	public async Task Then_the_region_of_contains(string name, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).Contains(color);

	/// <summary>Asserts that a colour covers at least a share of an element's region.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="percent">The share, as a percentage.</param>
	/// <param name="color">The colour that must be present.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} contains at least {float} percent {string}")]
	public async Task Then_the_region_of_contains_at_least_percent(string name, float percent, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).Contains(color, percent / 100.0);

	/// <summary>Asserts that a colour is absent from an element's region.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour that must be absent.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} does not contain {string}")]
	public async Task Then_the_region_of_does_not_contain(string name, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).DoesNotContain(color);

	/// <summary>Asserts what colour the ink inside an element's region is.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="color">The colour the ink must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ink color of {string} is {string}")]
	public async Task Then_the_ink_color_of_is(string name, Color color) =>
		(await RegionAsync(name).ConfigureAwait(false)).InkColorIs(color);

	/// <summary>Asserts that something was drawn inside an element's region.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} has ink")]
	public async Task Then_the_region_of_has_ink(string name) =>
		(await RegionAsync(name).ConfigureAwait(false)).HasInk();

	/// <summary>Asserts that an element's region is nothing but panel background.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} is blank")]
	public async Task Then_the_region_of_is_blank(string name) =>
		(await RegionAsync(name).ConfigureAwait(false)).IsBlank();

	/// <summary>Asserts that an element's region runs as a gradient between two colours.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="axis">"horizontal" or "vertical".</param>
	/// <param name="start">The colour at the start of the axis.</param>
	/// <param name="end">The colour at the end of the axis.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} runs as a {word} gradient from {string} to {string}")]
	public async Task Then_the_region_of_runs_as_a_gradient(string name, string axis, Color start, Color end) =>
		(await RegionAsync(name).ConfigureAwait(false))
			.GradientRunsFrom(start, end, GherkinValue.ToEnum<GradientAxis>(axis));

	// -------------------------------------------- appearance, named frames

	/// <summary>Asserts that an element's region in a named frame is painted one colour.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">The captured frame to look at.</param>
	/// <param name="color">The colour it must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} in frame {string} is uniformly {string}")]
	public async Task Then_the_region_of_in_frame_is_uniformly(string name, string frameName, Color color) =>
		(await RegionAsync(name, frameName).ConfigureAwait(false)).IsUniformly(color);

	/// <summary>Asserts that a colour is absent from an element's region in a named frame.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">The captured frame to look at.</param>
	/// <param name="color">The colour that must be absent.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} in frame {string} does not contain {string}")]
	public async Task Then_the_region_of_in_frame_does_not_contain(string name, string frameName, Color color) =>
		(await RegionAsync(name, frameName).ConfigureAwait(false)).DoesNotContain(color);

	/// <summary>Asserts what colour the ink of an element is in a named frame.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">The captured frame to look at.</param>
	/// <param name="color">The colour the ink must be.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ink color of {string} in frame {string} is {string}")]
	public async Task Then_the_ink_color_of_in_frame_is(string name, string frameName, Color color) =>
		(await RegionAsync(name, frameName).ConfigureAwait(false)).InkColorIs(color);

	/// <summary>Asserts that an element's region changed between two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} in frame {string} differs from frame {string}")]
	public async Task Then_the_region_of_in_frame_differs_from_frame(string name, string frameName, string otherFrameName)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		region.DiffersFrom(other);
	}

	/// <summary>Asserts that an element's region did not change between two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the region of {string} in frame {string} is unchanged from frame {string}")]
	public async Task Then_the_region_of_in_frame_is_unchanged_from_frame(string name, string frameName, string otherFrameName)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		region.SameAs(other);
	}

	/// <summary>Asserts that an element's ink sits in the same place in two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="frameName">One frame.</param>
	/// <param name="otherFrameName">The other frame.</param>
	/// <param name="tolerance">How many pixels each edge may move.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ink bounds of {string} in frames {string} and {string} agree within {int} pixel(s)")]
	public async Task Then_the_ink_bounds_of_in_frames_agree_within(string name, string frameName,
		string otherFrameName, int tolerance)
	{
		var region = await RegionAsync(name, frameName).ConfigureAwait(false);
		var other = await RegionAsync(name, otherFrameName).ConfigureAwait(false);
		var bounds = region.InkBounds();
		var otherBounds = other.InkBounds();
		var difference = bounds.LargestEdgeDifference(otherBounds);

		difference.Should().BeLessThanOrEqualTo(tolerance,
			"the ink of \"{0}\" must not move: it is {1} in frame \"{2}\" and {3} in frame \"{4}\"",
			name, bounds, frameName, otherBounds, otherFrameName);
	}

	/// <summary>Asserts that only one element's rectangle repainted between two captured frames.</summary>
	/// <param name="name">The Gherkin name of the element that was allowed to change.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("nothing outside {string} changed between frames {string} and {string}")]
	public async Task Then_nothing_outside_changed_between_frames(string name, string frameName, string otherFrameName)
	{
		var element = ElementRegistry.Resolve(name);
		var bounds = await DeviceRect.OfAsync(element, inset: -1).ConfigureAwait(false);
		var frame = ScenarioFrames.Get(_scenarioContext, frameName);
		var other = ScenarioFrames.Get(_scenarioContext, otherFrameName);
		frame.UnchangedOutside(other, bounds);
	}

	// ------------------------------------------------------ tree and events

	/// <summary>Asserts what text an element holds. Text content is a fact about the tree.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="expected">The text it must hold.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the Text of {string} is {string}")]
	public async Task Then_the_Text_of_is(string name, string expected)
	{
		var actual = string.Empty;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			actual = ElementRegistry.Resolve(name) switch
			{
				TextBlock text => text.Text,
				TextBox box => box.Text,
				_ => throw new NotSupportedException($"\"{name}\" has no Text the harness can read."),
			};
		}).ConfigureAwait(false);

		actual.Should().Be(expected, "the Text of \"{0}\" was asserted", name);
	}

	/// <summary>Asserts an element's laid-out size, read from the visual tree.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="width">The expected width in device pixels.</param>
	/// <param name="height">The expected height in device pixels.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} is {int} by {int} device pixels")]
	public async Task Then_is_by_device_pixels(string name, int width, int height)
	{
		var actualWidth = 0;
		var actualHeight = 0;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var element = ElementRegistry.Resolve(name);
			actualWidth = (int) Math.Round(element.ActualWidth);
			actualHeight = (int) Math.Round(element.ActualHeight);
		}).ConfigureAwait(false);

		actualWidth.Should().Be(width, "the laid-out width of \"{0}\" was asserted", name);
		actualHeight.Should().Be(height, "the laid-out height of \"{0}\" was asserted", name);
	}

	/// <summary>Asserts that nothing of that name is anywhere in the tree or the registry.</summary>
	/// <param name="name">The Gherkin name that must not resolve.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("no element named {string} exists")]
	public async Task Then_no_element_named_exists(string name)
	{
		var resolves = false;
		await TestTargetFixture.RunOnUIThreadAsync(() => resolves = ElementRegistry.TryResolve(name, out _))
			.ConfigureAwait(false);

		resolves.Should().BeFalse(
			"nothing named \"{0}\" may be left on the panel; the registry holds [{1}] and the tree holds [{2}]",
			name,
			string.Join(", ", ElementRegistry.Names),
			string.Join(", ", VirtualApplication.Instance.NamedElements()));
	}

	/// <summary>Asserts that an element raised Click exactly once.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	[Then("the Click of {string} was raised once")]
	public void Then_the_Click_of_was_raised_once(string name) => AssertEventCount(name, "Click", 1);

	/// <summary>Asserts how often an element raised Click.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	/// <param name="times">How often it must have been raised.</param>
	[Then("the Click of {string} was raised {int} times")]
	public void Then_the_Click_of_was_raised_times(string name, int times) => AssertEventCount(name, "Click", times);

	/// <summary>Asserts that an element never raised Click.</summary>
	/// <param name="name">The Gherkin name of the element.</param>
	[Then("the Click of {string} was not raised")]
	public void Then_the_Click_of_was_not_raised(string name) => AssertEventCount(name, "Click", 0);

	// --------------------------------------------------------------- inner

	private const string LastChangeKey = "uireqs.lastChange";

	private void AssertEventCount(string name, string eventName, int expected) =>
		EventRecorder.Count(name, eventName).Should().Be(expected,
			"the {0} of \"{1}\" was asserted; the scenario recorded [{2}]",
			eventName, name, string.Join(", ", EventRecorder.Recorded));

	private Task<Region> RegionAsync(string elementName, string frameName = ScenarioFrames.CurrentFrameName) =>
		ScenarioFrames.RegionAsync(_scenarioContext, elementName, frameName);

	private async Task ShowAsync(string kind, string name, int? width, int? height, Color? background)
	{
		var properties = new List<KeyValuePair<string, string>>();
		if (width is { } w)
		{
			properties.Add(new KeyValuePair<string, string>("Width", w.ToString(CultureInfo.InvariantCulture)));
		}

		if (height is { } h)
		{
			properties.Add(new KeyValuePair<string, string>("Height", h.ToString(CultureInfo.InvariantCulture)));
		}

		if (background is { } color)
		{
			properties.Add(new KeyValuePair<string, string>("Background", Colors.Describe(color)));
		}

		var element = await ElementFactory.CreateAsync(kind, name, properties).ConfigureAwait(false);
		await TestTargetFixture.SetContentAsync(element).ConfigureAwait(false);
	}

	private static IEnumerable<KeyValuePair<string, string>> ReadProperties(DataTable table)
	{
		ArgumentNullException.ThrowIfNull(table);

		return table.Rows.Select(row => new KeyValuePair<string, string>(
			row[table.Header.First()],
			row[table.Header.Last()]));
	}
}
