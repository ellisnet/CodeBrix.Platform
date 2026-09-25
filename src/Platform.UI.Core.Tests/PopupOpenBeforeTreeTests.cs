#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Host-free fences for the warning half of WPE1-6's Popup fix (a Popup opened before it can reach a XamlRoot). The
/// showing half needs a live tree (a PopupRoot) and is fenced by the Core UIReqs Popup.feature scenarios; what this
/// suite can check without a host is that a PARENTLESS Popup with no XamlRoot stays closed and says so exactly once,
/// and that a Popup that has a parent (it is shown when it is loaded) says nothing.
/// </summary>
public class PopupOpenBeforeTreeTests
{
	[Fact]
	public void When_A_Parentless_Popup_Is_Opened_With_No_XamlRoot_Then_It_Stays_Closed_And_Warns_Once()
	{
		//Arrange
		var popup = new Popup { Child = new Border { Width = 20, Height = 10 } };

		//Act
		popup.IsOpen = true;
		popup.IsOpen = false;
		popup.IsOpen = true;

		//Assert
		popup.IsOpen.Should().BeTrue("IsOpen keeps what the application set, as in WinUI 3");
		popup.XamlRoot.Should().BeNull();
		popup.MissingXamlRootReports.Should().Be(1, "the warning is logged once per Popup, not once per open");
	}

	[Fact]
	public void When_A_Popup_With_A_Parent_Is_Opened_Before_It_Is_Loaded_Then_No_Warning_Is_Logged()
	{
		//Arrange
		var popup = new Popup { Child = new Border { Width = 20, Height = 10 } };
		var panel = new Grid();
		panel.Children.Add(popup);

		//Act
		popup.IsOpen = true;

		//Assert
		popup.IsOpen.Should().BeTrue();
		popup.MissingXamlRootReports.Should().Be(0, "a Popup with a parent is shown when it is loaded, so it is not reported");
	}

	[Fact]
	public void When_A_Parentless_Popup_Is_Never_Opened_Then_No_Warning_Is_Logged()
	{
		//Arrange
		var popup = new Popup { Child = new Border { Width = 20, Height = 10 } };

		//Act
		popup.IsOpen = false;

		//Assert
		popup.MissingXamlRootReports.Should().Be(0);
	}
}
