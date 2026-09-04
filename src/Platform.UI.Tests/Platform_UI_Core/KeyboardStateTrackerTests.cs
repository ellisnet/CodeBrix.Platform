using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.UI.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace CodeBrix.Platform.UI.Tests.Platform_UI_Core;

/// <summary>
/// Fences the modifier half of the key-state table against the modifier mask that every routed key
/// and pointer event carries. A table fed only by key down and key up cannot notice a press or a
/// release the application never received - a window manager that keeps Alt for a window drag, or
/// Alt+Space for its window menu, swallows both - and would report that modifier held on every later
/// click until the window was deactivated, which is what a modifier-aware click reads at the click.
/// The keys are driven through the platform's own key hook, the routed-event path.
/// </summary>
[TestClass]
public class KeyboardStateTrackerTests
{
	[TestInitialize]
	public void Setup() => KeyboardStateTracker.Reset();

	[TestCleanup]
	public void Cleanup() => KeyboardStateTracker.Reset();

	[TestMethod]
	public void ReconcileModifiers_releases_a_modifier_the_mask_of_a_later_key_event_does_not_carry()
	{
		//Arrange
		var element = new Border();
		PressLeftMenu(element);

		//Act
		//The release never arrives; the next key the application does see says, in its mask, that
		//Alt is not held any more.
		element.RaiseEvent(
			UIElement.KeyDownEvent,
			new KeyRoutedEventArgs(element, VirtualKey.Z, VirtualKeyModifiers.None));

		//Assert
		KeyboardStateTracker.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down).Should().BeFalse();
		KeyboardStateTracker.GetKeyState(VirtualKey.LeftMenu).HasFlag(CoreVirtualKeyStates.Down).Should().BeFalse();
		KeyboardStateTracker.GetKeyState(VirtualKey.RightMenu).HasFlag(CoreVirtualKeyStates.Down).Should().BeFalse();
	}

	[TestMethod]
	public void ReconcileModifiers_releases_a_modifier_the_mask_of_a_later_pointer_event_does_not_carry()
	{
		//Arrange
		var element = new Border();
		PressLeftMenu(element);

		//Act
		//A plain click, with no modifier in its mask, is the event a modifier-aware button reads its
		//modifiers at.
		element.RaiseEvent(
			UIElement.PointerPressedEvent,
			new PointerRoutedEventArgs(new Point(10, 10), VirtualKeyModifiers.None));

		//Assert
		KeyboardStateTracker.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down).Should().BeFalse();
		KeyboardStateTracker.GetKeyState(VirtualKey.LeftMenu).HasFlag(CoreVirtualKeyStates.Down).Should().BeFalse();
	}

	[TestMethod]
	public void ReconcileModifiers_presses_a_modifier_the_mask_carries()
	{
		//Arrange
		var element = new Border();
		KeyboardStateTracker.GetKeyState(VirtualKey.Shift).Should().Be(CoreVirtualKeyStates.None);

		//Act
		//The Shift press was never delivered, but the mask on the key that follows carries it.
		element.RaiseEvent(
			UIElement.KeyDownEvent,
			new KeyRoutedEventArgs(element, VirtualKey.Z, VirtualKeyModifiers.Shift));

		//Assert
		KeyboardStateTracker.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down).Should().BeTrue();
	}

	[TestMethod]
	public void ReconcileModifiers_leaves_the_non_modifier_keys_alone()
	{
		//Arrange
		var element = new Border();
		element.RaiseEvent(
			UIElement.KeyDownEvent,
			new KeyRoutedEventArgs(element, VirtualKey.Z, VirtualKeyModifiers.None));
		KeyboardStateTracker.GetKeyState(VirtualKey.Z).HasFlag(CoreVirtualKeyStates.Down).Should().BeTrue();

		//Act
		element.RaiseEvent(
			UIElement.PointerPressedEvent,
			new PointerRoutedEventArgs(new Point(10, 10), VirtualKeyModifiers.None));

		//Assert
		//Z is not a modifier, so no mask can speak for it: only its own key events may.
		KeyboardStateTracker.GetKeyState(VirtualKey.Z).HasFlag(CoreVirtualKeyStates.Down).Should().BeTrue();
	}

	[TestMethod]
	public void ReconcileModifiers_does_not_answer_for_the_key_its_own_event_is_about()
	{
		//Arrange
		var element = new Border();

		//Act
		//X11 samples the mask BEFORE the key that raised the event is applied, so the Alt press
		//arrives with an Alt-free mask. The key the event is about must not be reconciled away.
		PressLeftMenu(element);

		//Assert
		KeyboardStateTracker.GetKeyState(VirtualKey.LeftMenu).HasFlag(CoreVirtualKeyStates.Down).Should().BeTrue();
		KeyboardStateTracker.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down).Should().BeTrue();
	}

	private static void PressLeftMenu(UIElement element)
		=> element.RaiseEvent(
			UIElement.KeyDownEvent,
			new KeyRoutedEventArgs(element, VirtualKey.LeftMenu, VirtualKeyModifiers.None));
}
