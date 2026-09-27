using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Reqnroll;
using SilverAssertions;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Xunit;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps of the ListBox scenarios: a ListBox built from a list of labels, taps on its items, keys injected into it
/// (the keyboard navigation and selection a ListBox does), and what it selected and painted. Every pattern names the
/// ListBox, so no other coverage group's sentence can match one of them.
/// </summary>
[Binding]
public sealed class ListBoxSteps
{
	/// <summary>How long an item is given to settle into the look its new state gives it.</summary>
	public static readonly TimeSpan ItemStateSettleDelay = TimeSpan.FromMilliseconds(500);

	/// <summary>How long the panel is left alone before a tap.</summary>
	public static readonly TimeSpan BeforeTapDelay = TimeSpan.FromMilliseconds(250);

	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public ListBoxSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

	/// <summary>Teaches the element factory the ListBox and its SelectionMode.</summary>
	[BeforeTestRun(Order = 14)]
	public static void Register_the_ListBox()
	{
		ElementFactory.RegisterKind("ListBox", BuildListBox);
		ElementFactory.RegisterProperty<ListBox>("SelectionMode", SetSelectionMode);
	}

	// ---------------------------------------------------------------- input

	/// <summary>Taps the centre of one item of a ListBox.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes once the tap has been delivered and the item has settled.</returns>
	[When("item {int} of the ListBox {string} is tapped")]
	public async Task When_item_is_tapped(int index, string name)
	{
		var bounds = await DeviceRect.OfAsync(await ContainerAsync(name, index).ConfigureAwait(false)).ConfigureAwait(false);
		var (x, y) = bounds.Center;
		await SettleAsync(BeforeTapDelay).ConfigureAwait(false);
		TestTargetFixture.Session.Tap(x, y);
		await SettleAsync(ItemStateSettleDelay).ConfigureAwait(false);
	}

	/// <summary>Injects one key (down and up) with Shift held around it.</summary>
	/// <param name="key">The key, as a VirtualKey name.</param>
	/// <returns>A task that completes once the keys have been injected and the item has settled.</returns>
	[When("the key {string} is injected with the Shift key held down")]
	public async Task When_the_key_is_injected_with_Shift(string key)
	{
		var virtualKey = Enum.Parse<VirtualKey>(key);
		await InjectAsync(
			Down(VirtualKey.Shift), Down(virtualKey), Up(virtualKey), Up(VirtualKey.Shift)).ConfigureAwait(false);
	}

	/// <summary>Scrolls a ListBox with ScrollIntoView so that one of its items shows.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes once the ListBox has come to rest.</returns>
	[When("the ListBox {string} scrolls item {int} into view")]
	public async Task When_the_ListBox_scrolls_item_into_view(string name, int index)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var box = Box(name);
			box.ScrollIntoView(box.Items[index - 1]);
		}).ConfigureAwait(false);
		await SettleAsync(ItemStateSettleDelay).ConfigureAwait(false);
	}

	// ---------------------------------------------------------------- facts

	/// <summary>Asserts how many items a ListBox holds.</summary>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="count">How many items it must hold.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ListBox {string} holds {int} items")]
	public async Task Then_holds_items(string name, int count)
	{
		var actual = -1;
		await TestTargetFixture.RunOnUIThreadAsync(() => actual = Box(name).Items.Count).ConfigureAwait(false);
		actual.Should().Be(count, "the number of items \"{0}\" holds was asserted", name);
	}

	/// <summary>Asserts a ListBox's SelectedIndex.</summary>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="index">The expected index, counted from zero as the property does.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the SelectedIndex of the ListBox {string} is {int}")]
	public async Task Then_the_SelectedIndex_is(string name, int index)
	{
		var actual = int.MinValue;
		await TestTargetFixture.RunOnUIThreadAsync(() => actual = Box(name).SelectedIndex).ConfigureAwait(false);
		actual.Should().Be(index, "the SelectedIndex of \"{0}\" was asserted", name);
	}

	/// <summary>Asserts which items a ListBox has selected (its SelectedItems, in any order).</summary>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="items">The selected items, comma separated; empty for none.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the ListBox {string} has selected {string}")]
	public async Task Then_has_selected(string name, string items)
	{
		var selected = Array.Empty<string>();
		await TestTargetFixture.RunOnUIThreadAsync(() =>
			selected = Box(name).SelectedItems.Select(item => item?.ToString() ?? "?").OrderBy(s => s, StringComparer.Ordinal).ToArray())
			.ConfigureAwait(false);

		var expected = ItemsElements.ReadList(items).OrderBy(s => s, StringComparer.Ordinal).ToArray();
		string.Join(", ", selected).Should().Be(string.Join(", ", expected), "the SelectedItems of \"{0}\" were asserted", name);
	}

	/// <summary>Asserts that one item's container is selected.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} is selected")]
	public async Task Then_item_is_selected(int index, string name) =>
		(await IsSelectedAsync(name, index).ConfigureAwait(false)).Should().BeTrue("item {0} of \"{1}\" must be selected", index, name);

	/// <summary>Asserts that one item's container is not selected.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} is not selected")]
	public async Task Then_item_is_not_selected(int index, string name) =>
		(await IsSelectedAsync(name, index).ConfigureAwait(false)).Should().BeFalse("item {0} of \"{1}\" must not be selected", index, name);

	/// <summary>Asserts which item of a ListBox has the keyboard focus.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} has the focus")]
	public async Task Then_item_has_the_focus(int index, string name)
	{
		var focused = -1;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var box = Box(name);
			if (box.XamlRoot is { } root && FocusManager.GetFocusedElement(root) is ListBoxItem item)
			{
				focused = box.IndexFromContainer(item);
			}
		}).ConfigureAwait(false);

		(focused + 1).Should().Be(index, "the focused item of \"{0}\" was asserted", name);
	}

	/// <summary>Asserts how often a ListBox reported that its selection changed.</summary>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="times">How often it must have been raised.</param>
	[Then("the SelectionChanged of the ListBox {string} was raised {int} times")]
	public void Then_the_SelectionChanged_was_raised(string name, int times) =>
		EventRecorder.Count(name, ItemsElements.SelectionChangedEvent).Should()
			.Be(times, "the SelectionChanged count of \"{0}\" was asserted", name);

	/// <summary>Asserts the text an item carries.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="text">The text the item must carry.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} carries the text {string}")]
	public async Task Then_item_carries_the_text(int index, string name, string text)
	{
		var container = await ContainerAsync(name, index).ConfigureAwait(false);
		var content = string.Empty;
		await TestTargetFixture.RunOnUIThreadAsync(() => content = (container as ContentControl)?.Content?.ToString() ?? string.Empty)
			.ConfigureAwait(false);
		content.Should().Be(text, "the content of item {0} of \"{1}\" was asserted", index, name);
	}

	// ---------------------------------------------------------------- pixels

	/// <summary>Asserts that an item was drawn.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} has ink")]
	public async Task Then_item_has_ink(int index, string name) =>
		(await ItemRegionAsync(name, index, ScenarioFrames.CurrentFrameName).ConfigureAwait(false)).HasInk();

	/// <summary>Asserts that an item looks different in one frame from another (its selection look came or went).</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} in frame {string} differs from frame {string}")]
	public async Task Then_item_in_frame_differs(int index, string name, string frameName, string otherFrameName)
	{
		var region = await ItemRegionAsync(name, index, frameName).ConfigureAwait(false);
		var other = await ItemRegionAsync(name, index, otherFrameName).ConfigureAwait(false);
		region.DiffersFrom(other);
	}

	/// <summary>Asserts that an item looks the same in one frame as in another.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <param name="frameName">The later frame.</param>
	/// <param name="otherFrameName">The earlier frame.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} in frame {string} is unchanged from frame {string}")]
	public async Task Then_item_in_frame_is_unchanged(int index, string name, string frameName, string otherFrameName)
	{
		var region = await ItemRegionAsync(name, index, frameName).ConfigureAwait(false);
		var other = await ItemRegionAsync(name, index, otherFrameName).ConfigureAwait(false);
		region.SameAs(other);
	}

	/// <summary>Asserts that an item lies entirely inside the ListBox's viewport.</summary>
	/// <param name="index">The item, counted from one.</param>
	/// <param name="name">The Gherkin name of the ListBox.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the ListBox {string} lies inside its viewport")]
	public async Task Then_item_is_inside_its_viewport(int index, string name)
	{
		var container = await DeviceRect.OfAsync(await ContainerAsync(name, index).ConfigureAwait(false)).ConfigureAwait(false);
		ScrollViewer scroller = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() => scroller = ItemsElements.ScrollViewerOf(Box(name))
			?? throw new NotSupportedException($"\"{name}\" has no scroll viewer in its template.")).ConfigureAwait(false);
		var viewport = await DeviceRect.OfAsync(scroller).ConfigureAwait(false);

		container.Y.Should().BeGreaterThanOrEqualTo(viewport.Y - GroupedListSteps.EdgeTolerance,
			"item {0} of \"{1}\" must be inside the viewport: it is at {2}, the viewport at {3}", index, name, container, viewport);
		container.Bottom.Should().BeLessThanOrEqualTo(viewport.Bottom + GroupedListSteps.EdgeTolerance,
			"item {0} of \"{1}\" must be inside the viewport: it is at {2}, the viewport at {3}", index, name, container, viewport);
	}

	// ---------------------------------------------------------------- helpers

	private static FrameworkElement BuildListBox()
	{
		var box = new ListBox();
		ItemsElements.RecordSelectionChanges(box);
		return box;
	}

	private static void SetSelectionMode(ListBox box, string value) =>
		box.SelectionMode = GherkinValue.ToEnum<SelectionMode>(value);

	private static ListBox Box(string name) =>
		ElementRegistry.Resolve(name) as ListBox ?? throw new NotSupportedException($"\"{name}\" is not a ListBox.");

	private static async Task<FrameworkElement> ContainerAsync(string name, int index)
	{
		FrameworkElement container = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
			container = Box(name).ContainerFromIndex(index - 1) as FrameworkElement
				?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
					$"\"{name}\" has generated no container for item {index}."))).ConfigureAwait(false);
		return container;
	}

	private static async Task<bool> IsSelectedAsync(string name, int index)
	{
		var container = await ContainerAsync(name, index).ConfigureAwait(false);
		var selected = false;
		await TestTargetFixture.RunOnUIThreadAsync(() => selected = container is SelectorItem { IsSelected: true }).ConfigureAwait(false);
		return selected;
	}

	private async Task<Region> ItemRegionAsync(string name, int index, string frameName)
	{
		var container = await ContainerAsync(name, index).ConfigureAwait(false);
		var bounds = await DeviceRect.OfAsync(container).ConfigureAwait(false);
		return new Region(ScenarioFrames.Get(_scenarioContext, frameName), bounds,
			string.Create(CultureInfo.InvariantCulture, $"item {index} of \"{name}\" in frame \"{frameName}\""));
	}

	private static async Task InjectAsync(params InjectedInputKeyboardInfo[] input)
	{
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var injector = InputInjector.TryCreate()
				?? throw new InvalidOperationException("The UI thread has no input injector target.");
			injector.InjectKeyboardInput(input);
		}).ConfigureAwait(false);
		await SettleAsync(ItemStateSettleDelay).ConfigureAwait(false);
	}

	private static InjectedInputKeyboardInfo Down(VirtualKey key) => new() { VirtualKey = (ushort)key };

	private static InjectedInputKeyboardInfo Up(VirtualKey key) =>
		new() { VirtualKey = (ushort)key, KeyOptions = InjectedInputKeyOptions.KeyUp };

	private static async Task SettleAsync(TimeSpan delay)
	{
		await Task.Delay(delay, TestContext.Current.CancellationToken).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}
}
