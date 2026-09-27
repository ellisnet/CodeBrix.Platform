using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Reqnroll;
using SilverAssertions;
using Windows.Foundation;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps of the grouped-list layout options: a grouped ListView or GridView whose ItemsStackPanel sets
/// GroupHeaderPlacement and GroupPadding, and where its group headers and items sit. Positions are compared in the
/// list's own logical coordinates (relative to its items panel), so a requirement can name a padding in pixels.
/// The other grouped-list sentences (headers above items, scrolling) are <see cref="GroupedListSteps"/>'.
/// </summary>
[Binding]
public sealed class GroupLayoutSteps
{
	/// <summary>How far (in logical pixels) an edge may be from where a requirement puts it.</summary>
	public const double Tolerance = 1.0;

	/// <summary>
	/// Shows a ListView or GridView bound to a grouped collection view with a GroupStyle, whose items panel is an
	/// ItemsStackPanel with the given GroupHeaderPlacement and GroupPadding.
	/// </summary>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The name the scenario refers to the list by.</param>
	/// <param name="width">The list's width.</param>
	/// <param name="height">The list's height.</param>
	/// <param name="placement">Top or Left.</param>
	/// <param name="padding">The GroupPadding, as XAML writes a Thickness ("left,top,right,bottom").</param>
	/// <param name="groups">A Group/Items table; Items is a comma-separated list.</param>
	/// <returns>A task that completes once the list is showing.</returns>
	[Given("the application shows a grouped {word} named {string} {int} by {int} with GroupHeaderPlacement {word} and GroupPadding {string} with the groups:")]
	public async Task Given_a_grouped_list_with_layout(string kind, string name, int width, int height, string placement, string padding, DataTable groups)
	{
		ArgumentNullException.ThrowIfNull(groups);

		var rows = groups.Rows.Select(row => (Group: row["Group"], Items: row["Items"])).ToArray();

		var element = await ElementFactory.CreateAsync(kind, name, new[]
		{
			new KeyValuePair<string, string>("Width", width.ToString(CultureInfo.InvariantCulture)),
			new KeyValuePair<string, string>("Height", height.ToString(CultureInfo.InvariantCulture)),
		}).ConfigureAwait(false);

		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var list = (ListViewBase)element;
			list.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"
				+ $"<ItemsStackPanel GroupHeaderPlacement='{placement}' GroupPadding='{padding}' /></ItemsPanelTemplate>");

			var source = rows.Select(row => new GroupedListSteps.NamedGroup(row.Group, ItemsElements.ReadList(row.Items))).ToList();
			list.ItemsSource = new CollectionViewSource { IsSourceGrouped = true, Source = source }.View;
			list.GroupStyle.Add(new GroupStyle { HeaderTemplate = (DataTemplate)XamlReader.Load(GroupedListSteps.HeaderTemplateXaml) });
		}).ConfigureAwait(false);

		await TestTargetFixture.SetContentAsync(element).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
		EventRecorder.Clear();
	}

	/// <summary>Asserts that a group header sits to the left of an item (its right edge at or before the item's left edge).</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} is to the left of item {int}")]
	public async Task Then_group_header_is_left_of_item(int index, string kind, string name, int item)
	{
		var (header, container, _) = await LayoutAsync(kind, name, index, item).ConfigureAwait(false);
		header.Right.Should().BeLessThanOrEqualTo(container.Left + Tolerance,
			"group header {0} of \"{1}\" must sit to the left of item {2}: they are at {3} and {4}", index, name, item, header, container);
	}

	/// <summary>Asserts that a group header's top is level with an item's top.</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} is level with item {int}")]
	public async Task Then_group_header_is_level_with_item(int index, string kind, string name, int item)
	{
		var (header, container, _) = await LayoutAsync(kind, name, index, item).ConfigureAwait(false);
		Math.Abs(header.Top - container.Top).Should().BeLessThanOrEqualTo(Tolerance,
			"group header {0} of \"{1}\" must be level with item {2}: they are at {3} and {4}", index, name, item, header, container);
	}

	/// <summary>Asserts where a group header's top is, from the top of the list's items panel.</summary>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="pixels">The distance in logical pixels.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("group header {int} of the {word} {string} starts {int} pixels from the top of the list")]
	public async Task Then_group_header_starts_from_the_top(int index, string kind, string name, int pixels)
	{
		var (header, _, _) = await LayoutAsync(kind, name, index, 1).ConfigureAwait(false);
		Math.Abs(header.Top - pixels).Should().BeLessThanOrEqualTo(Tolerance,
			"group header {0} of \"{1}\" must start {2} pixels from the top of the list: it is at {3}", index, name, pixels, header);
	}

	/// <summary>Asserts an item's insets from the left edge of the list's items panel and the right edge of its viewport.</summary>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="left">The inset from the left edge, in logical pixels.</param>
	/// <param name="right">The inset from the right edge, in logical pixels.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} of the {word} {string} is inset {int} pixels on the left and {int} pixels on the right")]
	public async Task Then_item_is_inset(int item, string kind, string name, int left, int right)
	{
		var (_, container, viewportRight) = await LayoutAsync(kind, name, 1, item).ConfigureAwait(false);
		Math.Abs(container.Left - left).Should().BeLessThanOrEqualTo(Tolerance,
			"item {0} of \"{1}\" must be inset {2} pixels on the left: it is at {3}", item, name, left, container);
		Math.Abs(viewportRight - container.Right - right).Should().BeLessThanOrEqualTo(Tolerance,
			"item {0} of \"{1}\" must be inset {2} pixels on the right: it is at {3}, the viewport ends at {4}", item, name, right, container, viewportRight);
	}

	/// <summary>Asserts the gap between the bottom of an item and the top of a group header below it.</summary>
	/// <param name="item">The item, counted from one across all the groups.</param>
	/// <param name="index">The header, counted from one, top to bottom.</param>
	/// <param name="kind">ListView or GridView.</param>
	/// <param name="name">The Gherkin name of the list.</param>
	/// <param name="pixels">The gap in logical pixels.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("item {int} and group header {int} of the {word} {string} are {int} pixels apart")]
	public async Task Then_item_and_header_are_apart(int item, int index, string kind, string name, int pixels)
	{
		var (header, container, _) = await LayoutAsync(kind, name, index, item).ConfigureAwait(false);
		Math.Abs(header.Top - container.Bottom - pixels).Should().BeLessThanOrEqualTo(Tolerance,
			"item {0} and group header {1} of \"{2}\" must be {3} pixels apart: they are at {4} and {5}", item, index, name, pixels, container, header);
	}

	// ---------------------------------------------------------------- helpers

	/// <summary>A header's and an item's bounds in the items panel's coordinates, and the right edge of the viewport there.</summary>
	private static async Task<(Rect Header, Rect Item, double ViewportRight)> LayoutAsync(string kind, string name, int header, int item)
	{
		(Rect, Rect, double) result = default;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var list = ElementRegistry.Resolve(name) as ListViewBase
				?? throw new NotSupportedException($"\"{name}\" is not a ListView or a GridView.");
			var panel = list.ItemsPanelRoot ?? throw new InvalidOperationException($"\"{name}\" has no items panel yet.");

			IEnumerable<FrameworkElement> headers = kind switch
			{
				"ListView" => VisualTreeSearch.FindDescendants<ListViewHeaderItem>(list),
				"GridView" => VisualTreeSearch.FindDescendants<GridViewHeaderItem>(list),
				_ => throw new NotSupportedException($"A {kind} shows no group headers the harness knows."),
			};

			var shown = headers
				.Where(h => h.Visibility == Visibility.Visible && h.ActualWidth > 0 && h.ActualHeight > 0)
				.Select(h => BoundsIn(h, panel))
				.OrderBy(r => r.Top)
				.ThenBy(r => r.Left)
				.ToArray();
			if (header < 1 || header > shown.Length)
			{
				throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
					$"\"{name}\" shows {shown.Length} group headers, so it has no group header {header}."));
			}

			var container = list.ContainerFromIndex(item - 1) as FrameworkElement
				?? throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"\"{name}\" has generated no container for item {item}."));

			// The breadth the items are laid out in: the scroll viewer's viewport, in the panel's coordinates (the panel
			// itself only reports the breadth its content asked for).
			var scroller = ItemsElements.ScrollViewerOf(list) ?? throw new NotSupportedException($"\"{name}\" has no scroll viewer in its template.");
			var viewportRight = scroller.TransformToVisual(panel).TransformPoint(default).X + scroller.ViewportWidth;

			result = (shown[header - 1], BoundsIn(container, panel), viewportRight);
		}).ConfigureAwait(false);

		return result;
	}

	private static Rect BoundsIn(FrameworkElement element, UIElement panel) =>
		element.TransformToVisual(panel).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
}
