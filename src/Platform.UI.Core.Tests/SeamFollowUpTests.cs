#nullable enable

using System;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for the seam follow-ups the Android track found (WPE1-1, items C0 and C0d): an element whose handler owns
/// its visuals is hit-testable, and a Template set on a control whose handler hosts its content leaves the hosted
/// content in place.
/// </summary>
public class SeamFollowUpTests
{
	private static FakeElementHandlerFactory FactoryFor(Func<UIElement, FakeElementHandler?> select)
		=> new(select);

	private static ControlTemplate BorderTemplate(string name)
		=> new(() => new Border { Name = name, Child = new ContentPresenter() });

	/// <summary>
	/// A live, loaded root with <paramref name="children"/> loaded in it. Host-free there is no window (a window's root
	/// is always hit-testable and needs an Application for its theme), so the root takes that role: its hit-test
	/// visibility is given the window root's value and coerced from there like any element's.
	/// </summary>
	private static Grid LoadedRoot(params FrameworkElement[] children)
	{
		var root = new Grid { Name = "root", Width = 200, Height = 100 };
		root.Enter(new EnterParams(isLive: true), 0);
		root.HitTestVisibility = HitTestability.Visible;
		foreach (var child in children)
		{
			root.Children.Add(child);
		}

		root.Measure(new Size(200, 100));
		root.Arrange(new Rect(0, 0, 200, 100));
		root.RaiseLoaded();
		foreach (var child in children)
		{
			child.RaiseLoaded();
		}

		return root;
	}

	// ---------------------------------------------------------------- C0: OwnsVisuals makes an element hit-testable

	[Fact]
	public void When_A_Handler_Owns_The_Visuals_Of_A_Control_With_No_Background_Then_The_Control_Is_Hit_Testable_And_The_Pick_Asks_The_Handler()
	{
		//Arrange
		var owning = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals) { HitTestResult = true };
		var factory = FactoryFor(e => e is FrameworkElement { Name: "owned" } ? owning : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var owned = new ContentControl { Name = "owned", Width = 80, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Template = BorderTemplate("ownedRoot") };
		var plain = new ContentControl { Name = "plain", Width = 80, Height = 40, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Template = BorderTemplate("plainRoot") };

		//Act
		var root = LoadedRoot(owned, plain);
		var picked = Microsoft.UI.Xaml.Media.VisualTreeHelper.SearchDownForTopMostElementAt(new Point(10, 10), root, Microsoft.UI.Xaml.Media.VisualTreeHelper.DefaultGetTestability, null).element;

		//Assert
		owned.Background.Should().BeNull();
		owned.HitTestVisibility.Should().Be(HitTestability.Visible);
		plain.HitTestVisibility.Should().Be(HitTestability.Invisible); // no handler: no background, not hit
		picked.Should().BeSameAs(owned);
		owning.HitTests.Should().NotBeEmpty();
	}

	[Fact]
	public void When_A_Handler_Owns_The_Visuals_Of_A_Button_With_No_Background_Then_The_Handler_Receives_The_Hit_Test()
	{
		//Arrange
		var owning = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals) { HitTestResult = true };
		var factory = FactoryFor(e => e is Button ? owning : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var button = new Button { Name = "button", Width = 80, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Template = BorderTemplate("buttonRoot") };

		//Act
		var root = LoadedRoot(button);
		var picked = Microsoft.UI.Xaml.Media.VisualTreeHelper.SearchDownForTopMostElementAt(new Point(12, 7), root, Microsoft.UI.Xaml.Media.VisualTreeHelper.DefaultGetTestability, null).element;

		//Assert
		button.Background.Should().BeNull();
		button.HitTestVisibility.Should().Be(HitTestability.Visible);
		picked.Should().BeSameAs(button);
		owning.HitTests.Should().Equal(new Point(12, 7));
	}

	[Fact]
	public void When_A_Handler_Starts_Or_Stops_Owning_The_Visuals_Then_The_Hit_Test_Visibility_Is_Evaluated_Again()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.None);
		var factory = FactoryFor(e => e is FrameworkElement { Name: "switching" } ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var element = new ContentControl { Name = "switching", Width = 80, Height = 40, Template = BorderTemplate("switchingRoot") };
		LoadedRoot(element);
		var before = element.HitTestVisibility;

		//Act
		handler.Capabilities = ElementHandlerCapabilities.OwnsVisuals;
		element.NotifyHandlerCapabilitiesChanged();
		var owning = element.HitTestVisibility;
		handler.Capabilities = ElementHandlerCapabilities.None;
		element.NotifyHandlerCapabilitiesChanged();
		var released = element.HitTestVisibility;

		//Assert
		before.Should().Be(HitTestability.Invisible);
		owning.Should().Be(HitTestability.Visible);
		released.Should().Be(HitTestability.Invisible);
	}

	[Fact]
	public void When_No_Handler_Service_Is_Registered_Then_A_Control_With_No_Background_Stays_Not_Hit_Testable()
	{
		//Arrange
		using var _ = ElementHandlerTestPlatform.Activate(null);
		var element = new ContentControl { Name = "skia", Width = 80, Height = 40, Template = BorderTemplate("skiaRoot") };

		//Act
		LoadedRoot(element);

		//Assert
		UIElement.AreHandlersActive.Should().BeFalse();
		element.HitTestVisibility.Should().Be(HitTestability.Invisible);
	}

	// ---------------------------------------------------------------- C0d: a new Template leaves hosted content alone

	[Fact]
	public void When_The_Template_Of_A_Control_Whose_Handler_Hosts_Its_Content_Changes_Then_The_Hosted_Content_Stays()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent);
		var factory = FactoryFor(e => e is ContentControl { Name: "hosting" } ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = new Grid { Name = "host" };
		host.Enter(new EnterParams(isLive: true), 0);
		var control = new ContentControl { Name = "hosting" };
		host.Children.Add(control);
		var content = new Border { Name = "content" };
		control.Content = content;

		//Act
		control.Template = BorderTemplate("newTemplateRoot");

		//Assert
		control.ContentTemplateRoot.Should().BeSameAs(content);
		VisualTreeHelper.GetChildrenCount(control).Should().Be(1);
		VisualTreeHelper.GetChild(control, 0).Should().BeSameAs(content);
		VisualTreeHelper.GetParent(content).Should().BeSameAs(control);
		handler.ChildEvents.Should().NotContain("-content");
	}

	[Fact]
	public void When_A_Control_Whose_Content_Was_Set_Before_It_Entered_The_Tree_Gets_Its_Template_From_A_Style_Then_The_Hosted_Content_Stays()
	{
		//Arrange: the AP6 sequence - Content set before Enter; the handler starts hosting it while it connects (as a
		// handler that switches mode does); the implicit style then sets Template during the same Enter.
		var hostedAtConnect = false;
		var handler = new FakeElementHandler(ElementHandlerCapabilities.None)
		{
			OnConnect = (self, element) =>
			{
				self.Capabilities = ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent;
				element.NotifyHandlerCapabilitiesChanged();
				hostedAtConnect = element is ContentControl { ContentTemplateRoot: not null };
			},
		};
		var factory = FactoryFor(e => e is ContentControl { Name: "expanderLike" } ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = new Grid { Name = "host" };
		var style = new Style(typeof(ContentControl));
		style.Setters.Add(new Setter(Control.TemplateProperty, BorderTemplate("styleTemplateRoot")));
		host.Resources[typeof(ContentControl)] = style;
		host.Enter(new EnterParams(isLive: true), 0);
		var content = new Border { Name = "content" };
		var control = new ContentControl { Name = "expanderLike", Content = content };

		//Act
		host.Children.Add(control);

		//Assert
		control.Template.Should().NotBeNull(); // the style did set it
		hostedAtConnect.Should().BeTrue();
		control.ContentTemplateRoot.Should().BeSameAs(content);
		VisualTreeHelper.GetParent(content).Should().BeSameAs(control);
		handler.ChildEvents.Should().NotContain("-content");
	}

	[Fact]
	public void When_No_Handler_Service_Is_Registered_Then_A_New_Template_Still_Replaces_The_Old_Template_Root()
	{
		//Arrange
		using var _ = ElementHandlerTestPlatform.Activate(null);
		var host = new Grid { Name = "host" };
		host.Enter(new EnterParams(isLive: true), 0);
		var control = new ContentControl { Name = "skia", Template = BorderTemplate("firstRoot") };
		host.Children.Add(control);
		control.Measure(new Size(100, 100));
		var firstRoot = VisualTreeHelper.GetChild(control, 0);

		//Act
		control.Template = BorderTemplate("secondRoot");
		var countAfterChange = VisualTreeHelper.GetChildrenCount(control);
		control.Measure(new Size(100, 100));

		//Assert
		((FrameworkElement)firstRoot).Name.Should().Be("firstRoot");
		countAfterChange.Should().Be(0);
		((FrameworkElement)VisualTreeHelper.GetChild(control, 0)).Name.Should().Be("secondRoot");
	}
}
