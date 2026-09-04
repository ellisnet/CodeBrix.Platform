#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.UI.Xaml.Core;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Resolves access keys (the letters or digits a user types while the Alt key is held, or while
/// access-key display mode is active) to the element that carries the matching
/// <see cref="UIElement.AccessKey"/>, and raises <see cref="UIElement.AccessKeyInvoked"/> on it.
/// </summary>
/// <remarks>
/// The manager is fed by the runtime's keyboard pipeline. Elements enter the registry when their
/// <see cref="UIElement.AccessKey"/> becomes non-empty and leave it when it is cleared or the
/// element is collected; the registry holds only weak references. When the registry is empty and
/// display mode is off, key handling is a single field read and costs no allocation.
/// </remarks>
public partial class AccessKeyManager
{
	private static readonly List<WeakReference<UIElement>> _registered = new();

	// Read on every key event before anything else, so an application that uses no access keys
	// pays one static field read per key.
	private static int _registeredCount;

	private static bool _isDisplayModeEnabled;
	private static XamlRoot? _displayModeRoot;
	private static UIElement? _displayModeScope;
	private static bool _displayModeScopeIsPopup;
	private static bool _isMenuKeyPressPending;

	/// <summary>
	/// Gets a value that indicates whether access-key display mode is active.
	/// </summary>
	public static bool IsDisplayModeEnabled => _isDisplayModeEnabled;

	/// <summary>
	/// Gets or sets a value that indicates whether key tips are enabled.
	/// </summary>
	/// <remarks>
	/// Key tips - the floating badges that show each access key while display mode is active -
	/// are not rendered by the Skia heads, so this value has no visual effect there. It defaults
	/// to <c>false</c>.
	/// </remarks>
	public static bool AreKeyTipsEnabled { get; set; }

	/// <summary>
	/// Occurs when <see cref="IsDisplayModeEnabled"/> changes.
	/// </summary>
	public static event TypedEventHandler<object, object>? IsDisplayModeEnabledChanged;

	/// <summary>
	/// Enters access-key display mode for the given XAML root, raising
	/// <see cref="UIElement.AccessKeyDisplayRequested"/> on every element with an access key in
	/// the currently active scope.
	/// </summary>
	/// <param name="XamlRoot">The XAML root whose tree should display its access keys.</param>
	public static void EnterDisplayMode(XamlRoot XamlRoot)
	{
		if (XamlRoot is null || _isDisplayModeEnabled)
		{
			return;
		}

		var scope = GetActiveScope(XamlRoot, out var isPopupScope);

		_isDisplayModeEnabled = true;
		_displayModeRoot = XamlRoot;
		_displayModeScope = scope;
		_displayModeScopeIsPopup = isPopupScope;

		var args = new AccessKeyDisplayRequestedEventArgs { PressedKeys = string.Empty };
		foreach (var element in GetScopedElements(XamlRoot, scope))
		{
			element.RaiseAccessKeyDisplayRequested(args);
		}

		IsDisplayModeEnabledChanged?.Invoke(null!, null!);
	}

	/// <summary>
	/// Leaves access-key display mode, raising <see cref="UIElement.AccessKeyDisplayDismissed"/>
	/// on every element with an access key in the scope that was active when it was entered.
	/// </summary>
	public static void ExitDisplayMode()
	{
		if (!_isDisplayModeEnabled)
		{
			return;
		}

		var root = _displayModeRoot;
		var scope = _displayModeScope;

		_isDisplayModeEnabled = false;
		_displayModeRoot = null;
		_displayModeScope = null;
		_displayModeScopeIsPopup = false;

		if (root is not null)
		{
			var args = new AccessKeyDisplayDismissedEventArgs();
			foreach (var element in GetScopedElements(root, scope))
			{
				element.RaiseAccessKeyDisplayDismissed(args);
			}
		}

		IsDisplayModeEnabledChanged?.Invoke(null!, null!);
	}

	/// <summary>
	/// Called by <see cref="UIElement.AccessKeyProperty"/> when an element's access key changes.
	/// </summary>
	/// <param name="element">The element whose access key changed.</param>
	/// <param name="newAccessKey">The new access key, or null/empty when it was cleared.</param>
	internal static void OnElementAccessKeyChanged(UIElement? element, string? newAccessKey)
	{
		if (element is null)
		{
			return;
		}

		if (string.IsNullOrEmpty(newAccessKey))
		{
			Unregister(element);
		}
		else
		{
			Register(element);
		}
	}

	/// <summary>
	/// Offers a key to the access-key pipeline.
	/// </summary>
	/// <param name="root">The XAML root the key was delivered to.</param>
	/// <param name="key">The virtual key.</param>
	/// <param name="modifiers">The modifiers held when the key was delivered.</param>
	/// <param name="isDown">True for a key press, false for a key release.</param>
	/// <returns>True when the access-key pipeline consumed the key.</returns>
	/// <remarks>
	/// When the matching element's <see cref="UIElement.AccessKeyInvoked"/> handlers leave the
	/// event unhandled, the element is invoked through its automation peer, which is what makes a
	/// plain <c>Button</c> or a <c>MenuFlyoutItem</c> answer its access key with no code behind.
	/// An invoke that opens a menu moves the active scope into that menu and keeps access-key mode
	/// alive there, so the menu's own items then answer bare letters - the WinUI menu behaviour.
	/// </remarks>
	internal static bool TryProcessKey(XamlRoot? root, VirtualKey key, VirtualKeyModifiers modifiers, bool isDown)
	{
		// The fast path an application without access keys always takes.
		if (_registeredCount == 0 && !_isDisplayModeEnabled)
		{
			return false;
		}

		if (root is null)
		{
			return false;
		}

		if (!isDown)
		{
			if (IsMenuKey(key) && _isMenuKeyPressPending)
			{
				// Alt pressed and released with nothing in between: toggle display mode.
				_isMenuKeyPressPending = false;
				if (_isDisplayModeEnabled)
				{
					ExitDisplayMode();
				}
				else
				{
					EnterDisplayMode(root);
				}

				return true;
			}

			return false;
		}

		if (IsMenuKey(key))
		{
			// Auto-repeat of a held Alt must not count as a fresh press.
			_isMenuKeyPressPending = !_isDisplayModeEnabled;
			return false;
		}

		var altHeld = (modifiers & VirtualKeyModifiers.Menu) != 0;
		_isMenuKeyPressPending = false;

		if (key == VirtualKey.Escape)
		{
			// Leave access-key mode but let the key travel on, so an open menu still closes.
			ExitDisplayMode();
			return false;
		}

		if (!altHeld && !_isDisplayModeEnabled)
		{
			return false;
		}

		if (!TryGetAccessKeyCharacter(key, out var character))
		{
			DismissOnUnmatchedKey();
			return false;
		}

		var scopeBefore = _isDisplayModeEnabled ? _displayModeScope : GetActiveScope(root, out _);
		var target = FindTarget(root, scopeBefore, character);
		if (target is null)
		{
			DismissOnUnmatchedKey();
			return false;
		}

		var invokedArgs = new AccessKeyInvokedEventArgs();
		target.RaiseAccessKeyInvoked(invokedArgs);

		if (!invokedArgs.Handled)
		{
			// WinUI's documented default when nothing handles the event: invoke the element the way
			// its automation peer would (Invoke, then Toggle, SelectionItem, ExpandCollapse).
			KeyboardAutomationInvoker.InvokeAutomationAction(target);
		}

		var scopeAfter = GetActiveScope(root, out var afterIsPopup);
		if (afterIsPopup && !ReferenceEquals(scopeAfter, scopeBefore))
		{
			// The invoke opened a menu: follow the scope into it and keep access-key mode alive so
			// its items answer their letters.
			ExitDisplayMode();
			EnterDisplayMode(root);
		}
		else if (_isDisplayModeEnabled && target.ExitDisplayModeOnAccessKeyInvoked)
		{
			ExitDisplayMode();
		}

		return true;
	}

	/// <summary>
	/// Gets the element in the active scope of <paramref name="root"/> whose access key is
	/// <paramref name="character"/>, or null when there is none.
	/// </summary>
	/// <param name="root">The XAML root to search.</param>
	/// <param name="character">The upper-case access-key character.</param>
	/// <returns>The matching element, or null.</returns>
	internal static UIElement? FindTarget(XamlRoot root, char character)
		=> FindTarget(root, GetActiveScope(root, out _), character);

	/// <summary>
	/// Gets the element that roots the currently active access-key scope of
	/// <paramref name="root"/>: the child of the top-most open popup when a popup (a menu, a
	/// flyout) is open, otherwise the root visual of the tree.
	/// </summary>
	/// <param name="root">The XAML root to inspect.</param>
	/// <returns>The element rooting the active scope, or null when the tree has no content.</returns>
	internal static UIElement? GetActiveScope(XamlRoot root) => GetActiveScope(root, out _);

	private static UIElement? GetActiveScope(XamlRoot root, out bool isPopupScope)
	{
		var visualTree = root.VisualTree;

		if (visualTree.PopupRoot?.GetTopmostPopup(PopupRoot.PopupFilter.All) is { } popup &&
			popup.Child is { } popupChild)
		{
			isPopupScope = true;
			return popupChild;
		}

		isPopupScope = false;
		return visualTree.PublicRootVisual ?? visualTree.RootElement;
	}

	private static UIElement? FindTarget(XamlRoot root, UIElement? scope, char character)
	{
		foreach (var element in GetScopedElements(root, scope))
		{
			var accessKey = element.AccessKey;
			if (accessKey is { Length: 1 } && char.ToUpperInvariant(accessKey[0]) == character)
			{
				return element;
			}
		}

		return null;
	}

	private static void DismissOnUnmatchedKey()
	{
		// While a menu is open its mnemonics stay live; at the root scope an unmatched key ends
		// access-key mode the way it does in WinUI.
		if (_isDisplayModeEnabled && !_displayModeScopeIsPopup)
		{
			ExitDisplayMode();
		}
	}

	private static IEnumerable<UIElement> GetScopedElements(XamlRoot root, UIElement? scope)
	{
		if (scope is null)
		{
			yield break;
		}

		for (var i = _registered.Count - 1; i >= 0; i--)
		{
			if (!_registered[i].TryGetTarget(out var element))
			{
				_registered.RemoveAt(i);
				_registeredCount = _registered.Count;
				continue;
			}

			if (element.XamlRoot == root && IsInScope(element, scope))
			{
				yield return element;
			}
		}
	}

	/// <summary>
	/// Determines whether <paramref name="element"/> answers access keys while
	/// <paramref name="scope"/> is the active scope.
	/// </summary>
	/// <param name="element">The candidate element.</param>
	/// <param name="scope">The element rooting the active scope.</param>
	/// <returns>True when the element belongs to the active scope.</returns>
	private static bool IsInScope(UIElement element, UIElement scope)
	{
		// An explicit scope owner moves the element into that owner's scope, wherever the element
		// sits in the tree.
		if (element.AccessKeyScopeOwner is { } declaredOwner)
		{
			return ReferenceEquals(declaredOwner, scope);
		}

		var current = VisualTreeHelper.GetParent(element);
		while (current is not null)
		{
			if (ReferenceEquals(current, scope))
			{
				return true;
			}

			// A nested scope, or a subtree redirected to another owner, hides everything below it
			// until that scope becomes the active one.
			if (current is UIElement ancestor &&
				(AccessKeys.IsAccessKeyScope(ancestor) || ancestor.AccessKeyScopeOwner is not null))
			{
				return false;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return false;
	}

	private static void Register(UIElement element)
	{
		for (var i = _registered.Count - 1; i >= 0; i--)
		{
			if (!_registered[i].TryGetTarget(out var existing))
			{
				_registered.RemoveAt(i);
			}
			else if (ReferenceEquals(existing, element))
			{
				return;
			}
		}

		_registered.Add(new WeakReference<UIElement>(element));
		_registeredCount = _registered.Count;
	}

	private static void Unregister(UIElement element)
	{
		for (var i = _registered.Count - 1; i >= 0; i--)
		{
			if (!_registered[i].TryGetTarget(out var existing) || ReferenceEquals(existing, element))
			{
				_registered.RemoveAt(i);
			}
		}

		_registeredCount = _registered.Count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsMenuKey(VirtualKey key)
		=> key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu;

	private static bool TryGetAccessKeyCharacter(VirtualKey key, out char character)
	{
		if (key >= VirtualKey.A && key <= VirtualKey.Z)
		{
			character = (char)key;
			return true;
		}

		if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
		{
			character = (char)key;
			return true;
		}

		if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
		{
			character = (char)('0' + (key - VirtualKey.NumberPad0));
			return true;
		}

		character = default;
		return false;
	}

	/// <summary>
	/// Clears every piece of manager state. Only for tests, which share one process.
	/// </summary>
	internal static void ResetForTests()
	{
		_registered.Clear();
		_registeredCount = 0;
		_isDisplayModeEnabled = false;
		_displayModeRoot = null;
		_displayModeScope = null;
		_displayModeScopeIsPopup = false;
		_isMenuKeyPressPending = false;
		AreKeyTipsEnabled = false;
		IsDisplayModeEnabledChanged = null;
	}
}
