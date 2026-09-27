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
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Reqnroll;
using SilverAssertions;
using Xunit;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps of the grouped lists: a ListView or GridView bound to a grouped collection view with a GroupStyle, the
/// group headers it shows (ListViewHeaderItem / GridViewHeaderItem), and scrolling one of its items into view. The item
/// steps of <see cref="ItemsSteps"/> (tapping, selection, content) work on a grouped list unchanged: an item is still
/// counted from one across all the groups.
/// </summary>
[Binding]
public sealed class GroupedListSteps
{
	/// <summary>The header template every grouped list uses: the group's name as a text.</summary>
	public const string HeaderTemplateXaml =
		"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"
		+ "<TextBlock Text='{Binding Name}' /></DataTemplate>";

	/// <summary>How long a list is left alone after it was scrolled by code, before anyone looks at it.</summary>
	public static readonly TimeSpan ScrollSettleDelay = TimeSpan.FromMilliseconds(300);

	/// <summary>
	/// How far (in logical pixels) a header's top may be from where a requirement says it is - a header is positioned in
	/// whole-layout units, the requirement in device pixels.
	/// </summary>
	public const int EdgeTolerance = 2;

	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public GroupedListSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

	// ---------------------------------------------------------------- building

	/// <summary>Shows a ListView or GridView bound to a grouped collection view, with a GroupStyle whose header shows the group name.</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The name the scenario refers to the list by.</param>
	/// <param name="width">The list's width.</param>
	/// <param name="height">The list's height.</param>
	/// <param name="groups">A Group/Items table; Items is a comma-separated list (empty for an empty group).</param>
	/// <returns>A task that completes once the list is showing.</returns>
	[Given("the application shows a grouped {word} named {string} {int} by {int} with the groups:")]
	public Task Given_a_grouped_list(string kind, string name, int width, int height, DataTable groups) =>
		ShowGroupedAsync(kind, name, width, height, groups, GroupStyleKind.Headers);

	/// <summary>As the grouped list above, with a GroupStyle that hides empty groups (HidesIfEmpty).</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The name the scenario refers to the list by.</param>
	/// <param name="width">The list's width.</param>
	/// <param name="height">The list's height.</param>
	/// <param name="groups">A Group/Items table.</param>
	/// <returns>A task that completes once the list is showing.</returns>
	[Given("the application shows a grouped {word} named {string} {int} by {int} that hides empty groups with the groups:")]
	public Task Given_a_grouped_list_hiding_empty_groups(string kind, string name, int width, int height, DataTable groups) =>
		ShowGroupedAsync(kind, name, width, height, groups, GroupStyleKind.HidesIfEmpty);

	/// <summary>As the grouped list above, but with NO GroupStyle: the source is grouped, the list asked for no headers.</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The name the scenario refers to the list by.</param>
	/// <param name="width">The list's width.</param>
	/// <param name="height">The list's height.</param>
	/// <param name="groups">A Group/Items table.</param>
	/// <returns>A task that completes once the list is showing.</returns>
	[Given("the application shows a {word} named {string} {int} by {int} bound to the groups without a GroupStyle:")]
	public Task Given_a_list_bound_to_groups_without_a_GroupStyle(string kind, string name, int width, int height, DataTable groups) =>
		ShowGroupedAsync(kind, name, width, height, groups, GroupStyleKind.None);

	// ---------------------------------------------------------------- scrolling

	/// <summary>Scrolls a list with ScrollIntoView (default alignment) so that one of its items shows.</summary>
	/// <param name="index">The item, counted from one across all the groups.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <returns>A task that completes once the list has come to rest.</returns>
	[When("item {int} of the {word} {string} is scrolled into view")]
	public async Task When_item_is_scrolled_into_view(int index, string kind, string name)
	{
		_ = kind;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var list = List(name);
			list.ScrollIntoView(list.Items[index - 1]);
		}).ConfigureAwait(false);

		await SettleAsync().ConfigureAwait(false);
	}

	/// <summary>Moves a list's scroll position down by a number of pixels, without animation.</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="pixels">How far to scroll.</param>
	/// <returns>A task that completes once the list has come to rest.</returns>
	[When("the {word} {string} is scrolled down by {int} pixels")]
	public async Task When_the_list_is_scrolled_down(string kind, string name, int pixels)
	{
		_ = kind;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var scroller = ItemsElements.ScrollViewerOf(List(name))
				?? throw new NotSupportedException($"\"{name}\" has no scroll viewer in its template.");
			scroller.ChangeView(null, scroller.VerticalOffset + pixels, null, disableAnimation: true);
		}).ConfigureAwait(false);

		await SettleAsync().ConfigureAwait(false);
	}

	// ---------------------------------------------------------------- headers

	/// <summary>Asserts how many group headers a list shows (realized, visible header containers).</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="count">How many headers it must show.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} {string} shows {int} group headers")]
	public async Task Then_the_list_shows_group_headers(string kind, string name, int count)
	{
		var texts = await HeaderTextsAsync(kind, name).ConfigureAwait(false);
		texts.Count.Should().Be(count, "the group headers \"{0}\" shows were counted; it shows [{1}]", name, string.Join(", ", texts));
	}

	/// <summary>Asserts the text a group header shows (the group header template's TextBlock).</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="text">The text it must show.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} shows {string}")]
	public async Task Then_group_header_shows(int index, string kind, string name, string text)
	{
		var texts = await HeaderTextsAsync(kind, name).ConfigureAwait(false);
		texts.Count.Should().BeGreaterThanOrEqualTo(index, "\"{0}\" must show group header {1}; it shows [{2}]", name, index, string.Join(", ", texts));
		texts[index - 1].Should().Be(text, "group header {0} of \"{1}\" was asserted", index, name);
	}

	/// <summary>Asserts that a group header was drawn.</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} has ink")]
	public async Task Then_group_header_has_ink(int index, string kind, string name)
	{
		var header = await HeaderAsync(kind, name, index).ConfigureAwait(false);
		var bounds = await DeviceRect.OfAsync(header).ConfigureAwait(false);
		new Region(ScenarioFrames.Get(_scenarioContext, ScenarioFrames.CurrentFrameName), bounds,
			string.Create(CultureInfo.InvariantCulture, $"group header {index} of \"{name}\"")).HasInk();
	}

	/// <summary>Asserts that a group header sits above an item (its bottom is at or above the item's top).</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} is above item {int}")]
	public async Task Then_group_header_is_above_item(int index, string kind, string name, int item)
	{
		var header = await DeviceRect.OfAsync(await HeaderAsync(kind, name, index).ConfigureAwait(false)).ConfigureAwait(false);
		var container = await DeviceRect.OfAsync(await ContainerAsync(name, item).ConfigureAwait(false)).ConfigureAwait(false);
		header.Bottom.Should().BeLessThanOrEqualTo(container.Y + EdgeTolerance,
			"group header {0} of \"{1}\" must sit above item {2}: they are at {3} and {4}", index, name, item, header, container);
	}

	/// <summary>Asserts that the group header showing a text sits above an item.</summary>
	/// <param name="text">The header's text.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the group header {string} of the {word} {string} is above item {int}")]
	public async Task Then_the_group_header_is_above_item(string text, string kind, string name, int item)
	{
		var header = await DeviceRect.OfAsync(await HeaderNamedAsync(kind, name, text).ConfigureAwait(false)).ConfigureAwait(false);
		var container = await DeviceRect.OfAsync(await ContainerAsync(name, item).ConfigureAwait(false)).ConfigureAwait(false);
		header.Bottom.Should().BeLessThanOrEqualTo(container.Y + EdgeTolerance,
			"the group header \"{0}\" of \"{1}\" must sit above item {2}: they are at {3} and {4}", text, name, item, header, container);
	}

	/// <summary>Asserts that an item sits above a group header (the item's bottom is at or above the header's top).</summary>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the {word} {string} is above group header {int}")]
	public async Task Then_item_is_above_group_header(int item, string kind, string name, int index)
	{
		var header = await DeviceRect.OfAsync(await HeaderAsync(kind, name, index).ConfigureAwait(false)).ConfigureAwait(false);
		var container = await DeviceRect.OfAsync(await ContainerAsync(name, item).ConfigureAwait(false)).ConfigureAwait(false);
		container.Bottom.Should().BeLessThanOrEqualTo(header.Y + EdgeTolerance,
			"item {0} of \"{1}\" must sit above group header {2}: they are at {3} and {4}", item, name, index, container, header);
	}

	/// <summary>Asserts that the group header showing a text sits at the top edge of the list's viewport (a sticky header).</summary>
	/// <param name="text">The header's text.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the group header {string} of the {word} {string} sits at the top of its viewport")]
	public async Task Then_the_group_header_sits_at_the_top(string text, string kind, string name)
	{
		var headerRect = await DeviceRect.OfAsync(await HeaderNamedAsync(kind, name, text).ConfigureAwait(false)).ConfigureAwait(false);

		ScrollViewer scroller = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() => scroller = ItemsElements.ScrollViewerOf(List(name))
			?? throw new NotSupportedException($"\"{name}\" has no scroll viewer in its template.")).ConfigureAwait(false);
		var viewport = await DeviceRect.OfAsync(scroller).ConfigureAwait(false);

		Math.Abs(headerRect.Y - viewport.Y).Should().BeLessThanOrEqualTo(EdgeTolerance,
			"the group header \"{0}\" of \"{1}\" must sit at the top of the viewport: it is at {2}, the viewport at {3}",
			text, name, headerRect, viewport);
	}

	/// <summary>Asserts that an item is entirely inside the list's viewport (and not under a sticky header).</summary>
	/// <param name="index">The item, counted from one across all the groups.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the {word} {string} is inside its viewport")]
	public async Task Then_item_is_inside_its_viewport(int index, string kind, string name)
	{
		_ = kind;
		var container = await DeviceRect.OfAsync(await ContainerAsync(name, index).ConfigureAwait(false)).ConfigureAwait(false);
		ScrollViewer scroller = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() => scroller = ItemsElements.ScrollViewerOf(List(name))
			?? throw new NotSupportedException($"\"{name}\" has no scroll viewer in its template.")).ConfigureAwait(false);
		var viewport = await DeviceRect.OfAsync(scroller).ConfigureAwait(false);

		container.Y.Should().BeGreaterThanOrEqualTo(viewport.Y - EdgeTolerance,
			"item {0} of \"{1}\" must be inside the viewport: it is at {2}, the viewport at {3}", index, name, container, viewport);
		container.Bottom.Should().BeLessThanOrEqualTo(viewport.Bottom + EdgeTolerance,
			"item {0} of \"{1}\" must be inside the viewport: it is at {2}, the viewport at {3}", index, name, container, viewport);
	}

	/// <summary>Asserts that the list has scrolled away from its top.</summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {word} {string} is no longer scrolled to its top")]
	public async Task Then_the_list_is_no_longer_at_its_top(string kind, string name)
	{
		_ = kind;
		var offset = 0.0;
		await TestTargetFixture.RunOnUIThreadAsync(() => offset = ItemsElements.ScrollViewerOf(List(name))?.VerticalOffset ?? 0)
			.ConfigureAwait(false);
		offset.Should().BeGreaterThan(0, "\"{0}\" must have scrolled", name);
	}

	// ---------------------------------------------------------------- helpers

	private enum GroupStyleKind
	{
		None,
		Headers,
		HidesIfEmpty,
	}

	private static async Task ShowGroupedAsync(string kind, string name, int width, int height, DataTable table, GroupStyleKind style)
	{
		ArgumentNullException.ThrowIfNull(table);

		var rows = table.Rows
			.Select(row => (Group: row["Group"], Items: table.Header.Contains("Items") ? row["Items"] : string.Empty))
			.ToArray();

		var element = await ElementFactory.CreateAsync(kind, name, new[]
		{
			new KeyValuePair<string, string>("Width", width.ToString(CultureInfo.InvariantCulture)),
			new KeyValuePair<string, string>("Height", height.ToString(CultureInfo.InvariantCulture)),
		}).ConfigureAwait(false);

		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var list = (ListViewBase) element;
			var groups = rows.Select(row => new NamedGroup(row.Group, ItemsElements.ReadList(row.Items))).ToList();
			var source = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
			list.ItemsSource = source.View;

			if (style != GroupStyleKind.None)
			{
				list.GroupStyle.Add(new GroupStyle
				{
					HeaderTemplate = (DataTemplate) XamlReader.Load(HeaderTemplateXaml),
					HidesIfEmpty = style == GroupStyleKind.HidesIfEmpty,
				});
			}
		}).ConfigureAwait(false);

		await TestTargetFixture.SetContentAsync(element).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
		EventRecorder.Clear();
	}

	private static ListViewBase List(string name) =>
		ElementRegistry.Resolve(name) as ListViewBase
			?? throw new NotSupportedException($"\"{name}\" is not a ListView or a GridView.");

	/// <summary>The visible group headers of a list, top to bottom (then left to right), with their texts.</summary>
	private static async Task<IReadOnlyList<(FrameworkElement Header, string Text)>> HeadersAsync(string kind, string name)
	{
		IReadOnlyList<(FrameworkElement, string)> headers = Array.Empty<(FrameworkElement, string)>();
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var list = List(name);
			IEnumerable<FrameworkElement> found = kind switch
			{
				"ListView" => VisualTreeSearch.FindDescendants<ListViewHeaderItem>(list),
				"GridView" => VisualTreeSearch.FindDescendants<GridViewHeaderItem>(list),
				_ => throw new NotSupportedException($"A {kind} shows no group headers the harness knows."),
			};

			headers = found
				.Where(IsShown)
				.Select(header => (Header: header, Position: header.TransformToVisual(list).TransformPoint(default)))
				.OrderBy(entry => entry.Position.Y)
				.ThenBy(entry => entry.Position.X)
				.Select(entry => (entry.Header, VisualTreeSearch.FindDescendant<TextBlock>(entry.Header)?.Text ?? string.Empty))
				.ToArray();
		}).ConfigureAwait(false);

		return headers;
	}

	private static async Task<IReadOnlyList<string>> HeaderTextsAsync(string kind, string name) =>
		(await HeadersAsync(kind, name).ConfigureAwait(false)).Select(entry => entry.Text).ToArray();

	private static async Task<FrameworkElement> HeaderAsync(string kind, string name, int index)
	{
		var headers = await HeadersAsync(kind, name).ConfigureAwait(false);
		if (index < 1 || index > headers.Count)
		{
			throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
				$"\"{name}\" shows {headers.Count} group headers, so it has no group header {index}."));
		}

		return headers[index - 1].Header;
	}

	private static async Task<FrameworkElement> HeaderNamedAsync(string kind, string name, string text)
	{
		var headers = await HeadersAsync(kind, name).ConfigureAwait(false);
		return headers.FirstOrDefault(h => h.Text == text).Header
			?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
				$"\"{name}\" shows no group header \"{text}\"; it shows [{string.Join(", ", headers.Select(h => h.Text))}]."));
	}

	/// <summary>A header that is in the tree and not collapsed (a pooled header container is collapsed).</summary>
	private static bool IsShown(FrameworkElement element)
	{
		for (DependencyObject? current = element; current is not null; current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current))
		{
			if (current is UIElement { Visibility: Visibility.Collapsed })
			{
				return false;
			}
		}

		return element.ActualHeight > 0 && element.ActualWidth > 0;
	}

	private static async Task<FrameworkElement> ContainerAsync(string name, int index)
	{
		FrameworkElement container = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
			container = List(name).ContainerFromIndex(index - 1) as FrameworkElement
				?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
					$"\"{name}\" has generated no container for item {index}."))).ConfigureAwait(false);
		return container;
	}

	private static async Task SettleAsync()
	{
		await Task.Delay(ScrollSettleDelay, TestContext.Current.CancellationToken).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	/// <summary>A group of the grouped lists: its items, and the name its header shows.</summary>
	[Bindable]
	public sealed class NamedGroup : List<string>
	{
		/// <summary>Creates the group.</summary>
		/// <param name="name">The name its header shows.</param>
		/// <param name="items">Its items.</param>
		public NamedGroup(string name, IEnumerable<string> items)
			: base(items) => Name = name;

		/// <summary>Gets the name the group's header shows.</summary>
		public string Name { get; }
	}
}
