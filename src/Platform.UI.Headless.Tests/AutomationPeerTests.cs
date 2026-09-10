#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Headless.Tests;

/// <summary>
/// <see cref="AutomationPeer"/>'s defaults: what every peer reports before a control has told it
/// anything.
/// </summary>
/// <remarks>
/// <para>
/// Ported from src/Platform.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/Given_AutomationPeer.cs.
/// The originals are MSTest <c>[TestMethod] [RunsOnUIThread]</c> and cannot run: this fork has no
/// runtime-test runner (SamplesApp and the in-app engine were dropped). Every one of them touches
/// only public API on a bare peer - no Window, no XamlRoot, no layout pass, no rendering - so the
/// host-free harness in DispatcherInitializer is all they need, and their results are the same on
/// Linux, Windows and macOS.
/// </para>
/// <para>
/// The method names are kept verbatim from the originals so a reader can find the source test, and
/// so a future port of the remaining suites lands beside these without a naming seam. The
/// assertions are rewritten from MSTest's <c>Assert</c> to SilverAssertions.
/// </para>
/// </remarks>
public class AutomationPeerTests
{
	/// <summary>
	/// A fresh peer for each test. xUnit constructs the class once per test method, so this is
	/// per-test state and never shared - the same isolation the original got from constructing one
	/// inside every method.
	/// </summary>
	private readonly TestAutomationPeer _peer = new();

	[Fact]
	public void When_AutomationPeer_Default_GetHeadingLevel()
		=> _peer.GetHeadingLevel().Should().Be(AutomationHeadingLevel.None);

	[Fact]
	public void When_AutomationPeer_Default_IsDialog()
		=> _peer.IsDialog().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_GetPattern()
		=> _peer.GetPattern(PatternInterface.Drag).Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetAcceleratorKey()
		=> _peer.GetAcceleratorKey().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetAccessKey()
		=> _peer.GetAccessKey().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetAutomationControlType()
		=> _peer.GetAutomationControlType().Should().Be(AutomationControlType.Custom);

	[Fact]
	public void When_AutomationPeer_Default_GetAutomationId()
		=> _peer.GetAutomationId().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetBoundingRectangle()
		=> _peer.GetBoundingRectangle().Should().Be(default(Rect));

	[Fact]
	public void When_AutomationPeer_Default_GetChildren()
		=> _peer.GetChildren().Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetClassName()
		=> _peer.GetClassName().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetClickablePoint()
		=> _peer.GetClickablePoint().Should().Be(default(Point));

	[Fact]
	public void When_AutomationPeer_Default_GetHelpText()
		=> _peer.GetHelpText().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetItemStatus()
		=> _peer.GetItemStatus().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetItemType()
		=> _peer.GetItemType().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetLabeledBy()
		=> _peer.GetLabeledBy().Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetLocalizedControlType()
		=> _peer.GetLocalizedControlType().Should().Be("custom");

	[Fact]
	public void When_AutomationPeer_Default_GetName()
		=> _peer.GetName().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_GetOrientation()
		=> _peer.GetOrientation().Should().Be(AutomationOrientation.None);

	[Fact]
	public void When_AutomationPeer_Default_HasKeyboardFocus()
		=> _peer.HasKeyboardFocus().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsContentElement()
		=> _peer.IsContentElement().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsControlElement()
		=> _peer.IsControlElement().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsEnabled()
		=> _peer.IsEnabled().Should().BeTrue();

	[Fact]
	public void When_AutomationPeer_Default_IsKeyboardFocusable()
		=> _peer.IsKeyboardFocusable().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsOffscreen()
		=> _peer.IsOffscreen().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsPassword()
		=> _peer.IsPassword().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsRequiredForForm()
		=> _peer.IsRequiredForForm().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_SetFocus()
	{
		//Act + Assert
		//The default is a no-op; the original asserted only that it does not throw.
		Action act = () => _peer.SetFocus();

		act.Should().NotThrow();
	}

	[Fact]
	public void When_AutomationPeer_Default_GetPeerFromPoint()
	{
#pragma warning disable CS0618 // GetPeerFromPoint is obsolete; the default behaviour is still under test.
		_peer.GetPeerFromPoint(default).Should().BeSameAs(_peer);
#pragma warning restore CS0618
	}

	[Fact]
	public void When_AutomationPeer_Default_GetLiveSetting()
		=> _peer.GetLiveSetting().Should().Be(AutomationLiveSetting.Off);

	[Fact]
	public void When_AutomationPeer_Default_Navigate()
		=> _peer.Navigate(default).Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetElementFromPoint()
		=> _peer.GetElementFromPoint(default).Should().BeSameAs(_peer);

	[Fact]
	public void When_AutomationPeer_Default_GetFocusedElement()
		=> _peer.GetFocusedElement().Should().BeSameAs(_peer);

	[Fact]
	public void When_AutomationPeer_Default_ShowContextMenu()
	{
		//Act + Assert
		//As with SetFocus, the default does nothing and the test is that it stays silent.
		Action act = () => _peer.ShowContextMenu();

		act.Should().NotThrow();
	}

	[Fact]
	public void When_AutomationPeer_Default_GetControlledPeers()
		=> _peer.GetControlledPeers().Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetAnnotations()
		=> _peer.GetAnnotations().Should().BeNull();

	[Fact]
	public void When_AutomationPeer_Default_GetPositionInSet()
		=> _peer.GetPositionInSet().Should().Be(-1);

	[Fact]
	public void When_AutomationPeer_Default_GetSizeOfSet()
		=> _peer.GetSizeOfSet().Should().Be(-1);

	[Fact]
	public void When_AutomationPeer_Default_GetLevel()
		=> _peer.GetLevel().Should().Be(-1);

	[Fact]
	public void When_AutomationPeer_Default_GetLandmarkType()
		=> _peer.GetLandmarkType().Should().Be(AutomationLandmarkType.None);

	[Fact]
	public void When_AutomationPeer_Default_GetLocalizedLandmarkType()
		=> _peer.GetLocalizedLandmarkType().Should().Be(string.Empty);

	[Fact]
	public void When_AutomationPeer_Default_IsPeripheral()
		=> _peer.IsPeripheral().Should().BeFalse();

	[Fact]
	public void When_AutomationPeer_Default_IsDataValidForForm()
		=> _peer.IsDataValidForForm().Should().BeTrue();

	[Fact]
	public void When_AutomationPeer_Default_GetFullDescription()
		=> _peer.GetFullDescription().Should().Be(string.Empty);

	/// <summary>
	/// The bare peer under test: everything it reports is <see cref="AutomationPeer"/>'s own
	/// default, because it overrides nothing.
	/// </summary>
	private sealed class TestAutomationPeer : AutomationPeer
	{
	}
}
