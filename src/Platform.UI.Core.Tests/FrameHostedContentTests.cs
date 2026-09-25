#nullable enable

using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>A page the Frame test navigates to.</summary>
public sealed partial class FrameTestPageA : Page { }

/// <summary>A second page the Frame test navigates to.</summary>
public sealed partial class FrameTestPageB : Page { }

/// <summary>
/// Regression fence for the AP2L2 seam note "a Frame given HostsContent|OwnsVisuals loses the page Frame.GoBack creates":
/// with Core as it is after WPE1-1's C0d (hosted content is never removed as an old template root), the page a hosting
/// Frame goes back to is hosted and live.
/// </summary>
public class FrameHostedContentTests
{
	[Fact]
	public void When_A_Frame_Whose_Handler_Hosts_Its_Content_Goes_Back_Then_The_Page_Is_Hosted_In_The_Live_Tree()
	{
		//Arrange
		var factory = new FakeElementHandlerFactory(e => e is Frame
			? new FakeElementHandler(ElementHandlerCapabilities.HostsContent | ElementHandlerCapabilities.OwnsVisuals) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = new Grid();
		host.Enter(new EnterParams(isLive: true), 0);
		var frame = new Frame();
		host.Children.Add(frame);
		frame.Navigate(typeof(FrameTestPageA));
		frame.Navigate(typeof(FrameTestPageB));

		//Act
		frame.GoBack();

		//Assert
		var page = frame.Content.Should().BeOfType<FrameTestPageA>().Subject;
		page.IsInLiveTree.Should().BeTrue();
		VisualTreeHelper.GetParent(page).Should().BeSameAs(frame);
	}
}
