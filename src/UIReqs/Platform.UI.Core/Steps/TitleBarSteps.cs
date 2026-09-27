using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reqnroll;
using SilverAssertions;
using Xunit;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;

namespace CodeBrix.Platform.UI.Core.UIReqs.Steps;

/// <summary>
/// The steps of the TitleBar scenarios: a TitleBar built from a property table, the parts of its default template
/// (back button, pane toggle button, left header, icon, title, subtitle, content, right header), taps on its buttons and
/// the BackRequested / PaneToggleRequested events they raise.
/// </summary>
[Binding]
public sealed class TitleBarSteps
{
	/// <summary>The event a TitleBar raises when its back button is invoked.</summary>
	public const string BackRequestedEvent = "BackRequested";

	/// <summary>The event a TitleBar raises when its pane toggle button is invoked.</summary>
	public const string PaneToggleRequestedEvent = "PaneToggleRequested";

	/// <summary>How far (in logical pixels) a measured edge may be from where a requirement puts it.</summary>
	public const double Tolerance = 1.0;

	private static readonly Dictionary<string, string> Parts = new(StringComparer.Ordinal)
	{
		["back button"] = "PART_BackButton",
		["pane toggle button"] = "PART_PaneToggleButton",
		["left header"] = "PART_LeftHeaderPresenter",
		["icon"] = "PART_Icon",
		["title"] = "PART_TitleText",
		["subtitle"] = "PART_SubtitleText",
		["content"] = "PART_ContentPresenter",
		["right header"] = "PART_RightHeaderPresenter",
	};

	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public TitleBarSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

	/// <summary>Teaches the element factory the TitleBar and the properties a scenario sets on it.</summary>
	[BeforeTestRun(Order = 15)]
	public static void Register_the_TitleBar()
	{
		ElementFactory.RegisterKind("TitleBar", BuildTitleBar);
		ElementFactory.RegisterProperty<TitleBar>("Title", SetTitle);
		ElementFactory.RegisterProperty<TitleBar>("Subtitle", SetSubtitle);
		ElementFactory.RegisterProperty<TitleBar>("IsBackButtonVisible", SetIsBackButtonVisible);
		ElementFactory.RegisterProperty<TitleBar>("IsBackButtonEnabled", SetIsBackButtonEnabled);
		ElementFactory.RegisterProperty<TitleBar>("IsPaneToggleButtonVisible", SetIsPaneToggleButtonVisible);
		ElementFactory.RegisterProperty<TitleBar>("LeftHeader", SetLeftHeader);
		ElementFactory.RegisterProperty<TitleBar>("Content", SetContent);
		ElementFactory.RegisterProperty<TitleBar>("RightHeader", SetRightHeader);
	}

	// ---------------------------------------------------------------- input

	/// <summary>Taps a part of a TitleBar (its back button or its pane toggle button).</summary>
	/// <param name="part">The part, as the scenario names it.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <returns>A task that completes once the tap has been delivered and the UI thread is idle.</returns>
	[When("the {string} of the TitleBar {string} is tapped")]
	public async Task When_the_part_is_tapped(string part, string name)
	{
		var bounds = await DeviceRect.OfAsync(await PartAsync(name, part).ConfigureAwait(false)).ConfigureAwait(false);
		var (x, y) = bounds.Center;
		await Task.Delay(ListBoxSteps.BeforeTapDelay, TestContext.Current.CancellationToken).ConfigureAwait(false);
		TestTargetFixture.Session.Tap(x, y);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	// ---------------------------------------------------------------- facts

	/// <summary>Asserts how often a TitleBar raised BackRequested.</summary>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <param name="times">How often it must have been raised.</param>
	[Then("the BackRequested of the TitleBar {string} was raised {int} times")]
	public void Then_BackRequested_was_raised(string name, int times) =>
		EventRecorder.Count(name, BackRequestedEvent).Should().Be(times, "the BackRequested count of \"{0}\" was asserted", name);

	/// <summary>Asserts how often a TitleBar raised PaneToggleRequested.</summary>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <param name="times">How often it must have been raised.</param>
	[Then("the PaneToggleRequested of the TitleBar {string} was raised {int} times")]
	public void Then_PaneToggleRequested_was_raised(string name, int times) =>
		EventRecorder.Count(name, PaneToggleRequestedEvent).Should().Be(times, "the PaneToggleRequested count of \"{0}\" was asserted", name);

	/// <summary>Asserts that a part of a TitleBar is shown (in the tree, visible, with a size).</summary>
	/// <param name="part">The part, as the scenario names it.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {string} of the TitleBar {string} is shown")]
	public async Task Then_the_part_is_shown(string part, string name) =>
		(await IsShownAsync(name, part).ConfigureAwait(false)).Should().BeTrue("the {0} of \"{1}\" must be shown", part, name);

	/// <summary>Asserts that a part of a TitleBar is not shown.</summary>
	/// <param name="part">The part, as the scenario names it.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {string} of the TitleBar {string} is not shown")]
	public async Task Then_the_part_is_not_shown(string part, string name) =>
		(await IsShownAsync(name, part).ConfigureAwait(false)).Should().BeFalse("the {0} of \"{1}\" must not be shown", part, name);

	/// <summary>Asserts whether the back button of a TitleBar is enabled.</summary>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the back button of the TitleBar {string} is disabled")]
	public async Task Then_the_back_button_is_disabled(string name)
	{
		var enabled = true;
		var button = await PartAsync(name, "back button").ConfigureAwait(false);
		await TestTargetFixture.RunOnUIThreadAsync(() => enabled = ((Control)button).IsEnabled).ConfigureAwait(false);
		enabled.Should().BeFalse("the back button of \"{0}\" must be disabled", name);
	}

	/// <summary>Asserts the text a text part (title or subtitle) of a TitleBar shows.</summary>
	/// <param name="part">The part, as the scenario names it.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <param name="text">The text it must show.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {string} of the TitleBar {string} shows {string}")]
	public async Task Then_the_part_shows(string part, string name, string text)
	{
		var element = await PartAsync(name, part).ConfigureAwait(false);
		var actual = string.Empty;
		await TestTargetFixture.RunOnUIThreadAsync(() => actual = (element as TextBlock)?.Text ?? string.Empty).ConfigureAwait(false);
		actual.Should().Be(text, "the {0} of \"{1}\" was asserted", part, name);
	}

	/// <summary>Asserts a TitleBar's height (32 compact, 48 when it has headers or content).</summary>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <param name="height">The height in logical pixels.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the TitleBar {string} is {int} pixels tall")]
	public async Task Then_the_TitleBar_is_tall(string name, int height)
	{
		var actual = 0.0;
		await TestTargetFixture.RunOnUIThreadAsync(() => actual = Bar(name).ActualHeight).ConfigureAwait(false);
		Math.Abs(actual - height).Should().BeLessThanOrEqualTo(Tolerance, "the height of \"{0}\" was asserted: it is {1}", name, actual);
	}

	/// <summary>Asserts that one part of a TitleBar ends at or before another starts (left to right).</summary>
	/// <param name="part">The part that must be on the left.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <param name="other">The part that must be on the right.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {string} of the TitleBar {string} is to the left of its {string}")]
	public async Task Then_the_part_is_to_the_left_of(string part, string name, string other)
	{
		var left = await BoundsAsync(name, part).ConfigureAwait(false);
		var right = await BoundsAsync(name, other).ConfigureAwait(false);
		left.Right.Should().BeLessThanOrEqualTo(right.Left + Tolerance,
			"the {0} of \"{1}\" must be to the left of its {2}: they are at {3} and {4}", part, name, other, left, right);
	}

	/// <summary>Asserts that a part of a TitleBar was drawn.</summary>
	/// <param name="part">The part, as the scenario names it.</param>
	/// <param name="name">The Gherkin name of the TitleBar.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the {string} of the TitleBar {string} has ink")]
	public async Task Then_the_part_has_ink(string part, string name)
	{
		var bounds = await DeviceRect.OfAsync(await PartAsync(name, part).ConfigureAwait(false)).ConfigureAwait(false);
		new Region(ScenarioFrames.Get(_scenarioContext, ScenarioFrames.CurrentFrameName), bounds,
			string.Create(CultureInfo.InvariantCulture, $"the {part} of \"{name}\"")).HasInk();
	}

	// ---------------------------------------------------------------- helpers

	private static FrameworkElement BuildTitleBar()
	{
		var bar = new TitleBar();
		bar.BackRequested += (sender, _) => EventRecorder.Record(sender.Name, BackRequestedEvent);
		bar.PaneToggleRequested += (sender, _) => EventRecorder.Record(sender.Name, PaneToggleRequestedEvent);
		return bar;
	}

	private static void SetTitle(TitleBar bar, string value) => bar.Title = value;

	private static void SetSubtitle(TitleBar bar, string value) => bar.Subtitle = value;

	private static void SetIsBackButtonVisible(TitleBar bar, string value) => bar.IsBackButtonVisible = bool.Parse(value);

	private static void SetIsBackButtonEnabled(TitleBar bar, string value) => bar.IsBackButtonEnabled = bool.Parse(value);

	private static void SetIsPaneToggleButtonVisible(TitleBar bar, string value) => bar.IsPaneToggleButtonVisible = bool.Parse(value);

	private static void SetLeftHeader(TitleBar bar, string value) => bar.LeftHeader = new Button { Content = value };

	private static void SetContent(TitleBar bar, string value) => bar.Content = new Button { Content = value, MinWidth = 120 };

	private static void SetRightHeader(TitleBar bar, string value) => bar.RightHeader = new Button { Content = value };

	private static string PartName(string part) =>
		Parts.TryGetValue(part, out var partName)
			? partName
			: throw new NotSupportedException($"A TitleBar has no part \"{part}\"; it has: {string.Join(", ", Parts.Keys)}.");

	private static TitleBar Bar(string name) =>
		ElementRegistry.Resolve(name) as TitleBar ?? throw new NotSupportedException($"\"{name}\" is not a TitleBar.");

	private static async Task<FrameworkElement> PartAsync(string name, string part)
	{
		FrameworkElement element = null!;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
			element = VisualTreeSearch.FindDescendantNamed(Bar(name), PartName(part))
				?? throw new InvalidOperationException($"\"{name}\" has no {part} ({PartName(part)}) in its template."))
			.ConfigureAwait(false);
		return element;
	}

	private static async Task<bool> IsShownAsync(string name, string part)
	{
		var shown = false;
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			if (VisualTreeSearch.FindDescendantNamed(Bar(name), PartName(part)) is not { } element)
			{
				return;
			}

			shown = element.ActualWidth > 0 && element.ActualHeight > 0;
			for (DependencyObject? current = element; current is not null && shown; current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current))
			{
				shown = current is not UIElement { Visibility: Visibility.Collapsed };
			}
		}).ConfigureAwait(false);
		return shown;
	}

	private static async Task<Windows.Foundation.Rect> BoundsAsync(string name, string part)
	{
		var element = await PartAsync(name, part).ConfigureAwait(false);
		var bounds = default(Windows.Foundation.Rect);
		await TestTargetFixture.RunOnUIThreadAsync(() =>
			bounds = element.TransformToVisual(Bar(name)).TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight)))
			.ConfigureAwait(false);
		return bounds;
	}
}
