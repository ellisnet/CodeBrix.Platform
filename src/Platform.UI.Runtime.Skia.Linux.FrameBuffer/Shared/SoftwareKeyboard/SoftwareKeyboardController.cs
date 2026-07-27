// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Controls.Extensions;
using CodeBrix.Platform.UI.Xaml.Core;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// Owns the software keyboard's lifetime and behavior:
/// <list type="bullet">
/// <item>Implements InputPane's platform seam, so the standard
/// InputPane.TryShow()/TryHide() calls work for manual control.</item>
/// <item>Implements the TextBox notifications seam, so the keyboard auto-shows
/// when an editable (non-read-only) TextBox or PasswordBox gains focus and
/// auto-hides when focus leaves (a focus move between two text controls does
/// not flicker the keyboard).</item>
/// <item>Honors the dismiss key: tapping it hides the keyboard and latches it
/// hidden for the dismissed control, so focus restoration (a closing dialog,
/// programmatic Focus(), hardware-keyboard Tab) cannot summon it back. The
/// latch clears — and the keyboard shows — when the user taps back inside that
/// control, when a different editable text control gains focus, when the
/// application itself calls InputPane.TryShow(), or (silently) when the control
/// leaves the visual tree.</item>
/// <item>While visible, withholds the keyboard's height from the application
/// content's layout (the root visual's bottom occlusion inset) so the UI
/// re-lays-out into the remaining space and the focused field can never sit
/// under the keyboard — Android's adjustResize semantics, not an overlay.</item>
/// <item>Reports the keyboard strip through InputPane.OccludedRect, which also
/// raises the public Showing/Hiding events.</item>
/// <item>Re-lays-out on every XamlRoot change, so device rotation while the
/// keyboard is up re-fits both the application and the keyboard.</item>
/// </list>
/// Registered only when the host builder's EnableSoftwareKeyboard was called;
/// otherwise none of this exists and the head behaves exactly as before.
/// </summary>
internal sealed class SoftwareKeyboardController : IInputPaneExtension, ITextBoxNotificationsProviderSingleton
{
	private readonly IXamlRootHost _host;
	private readonly ISoftwareKeyInjector _injector;
	private readonly SoftwareKeyboardOptions _options;

	private Popup? _popup;
	private SoftwareKeyboardView? _view;
	private bool _visible;
	private TextBox? _focusedTextBox;
	private int _focusVersion;

	// The dismiss-key latch: the control whose keyboard the user explicitly sent
	// away. While set, focus (re)gained by THIS control does not re-show the
	// keyboard; see the class remarks for what clears it. One latch, not a
	// per-control memory — once the keyboard legitimately shows again anywhere,
	// every control is fresh.
	private TextBox? _dismissedTextBox;
	private bool _rootPressHooked;

	internal SoftwareKeyboardController(IXamlRootHost host, ISoftwareKeyInjector injector,
		SoftwareKeyboardOptions options)
	{
		_host = host;
		_injector = injector;
		_options = options;
	}

	public bool TryShow()
	{
		// Showing — whether focus-driven, tap-driven, or the application's own
		// InputPane.TryShow() call — always clears the dismiss latch: every path
		// here means somebody actively wants the keyboard back.
		_dismissedTextBox = null;

		if (_visible)
		{
			return false;
		}
		var xamlRoot = (_host.RootElement as FrameworkElement)?.XamlRoot;
		if (xamlRoot is null)
		{
			return false;
		}
		EnsureRootPressHook();

		if (_view is null || _popup is null)
		{
			var active = KeyboardLayoutCatalog.ResolveActive(_options.Layout);
			var enabled = KeyboardLayoutCatalog.ResolveEnabled(active, _options.EnabledLayouts);
			_view = new SoftwareKeyboardView(_injector, enabled, _options);
			_view.DismissRequested += OnUserDismissed;
			_popup = new Popup
			{
				XamlRoot = xamlRoot,
				Child = _view,
			};
			xamlRoot.Changed += OnXamlRootChanged;
		}

		_visible = true;
		ApplyMetrics(xamlRoot);
		_popup.IsOpen = true;
		return true;
	}

	public bool TryHide()
	{
		if (!_visible || _popup is null)
		{
			return false;
		}
		_visible = false;
		_popup.IsOpen = false;
		// IRootElement, not a concrete root type: on the Skia heads the root is a
		// XamlIslandRoot ("Skia always uses Desktop windows"), not a RootVisual.
		if (_host.RootElement is IRootElement rootElement)
		{
			rootElement.ContentBottomOcclusionInset = 0;
		}
		InputPane.GetForCurrentView().OccludedRect = default;
		return true;
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		if (_visible)
		{
			ApplyMetrics(sender);
		}
	}

	private void ApplyMetrics(XamlRoot xamlRoot)
	{
		if (_view is null || _popup is null)
		{
			return;
		}
		var size = xamlRoot.Size;
		var keyboardHeight = _view.ComputeHeight(size);
		_view.ApplyMetrics(size.Width, keyboardHeight);
		_popup.HorizontalOffset = 0;
		_popup.VerticalOffset = size.Height - keyboardHeight;

		if (_host.RootElement is IRootElement rootElement)
		{
			rootElement.ContentBottomOcclusionInset = keyboardHeight;
		}
		InputPane.GetForCurrentView().OccludedRect
			= new Rect(0, size.Height - keyboardHeight, size.Width, keyboardHeight);
	}

	void ITextBoxNotificationsProviderSingleton.OnFocused(TextBox textBox)
	{
		// A read-only text control is not text ENTRY: it never summons the
		// keyboard, and deliberately neither claims _focusedTextBox nor bumps
		// the version — focus moving editable→read-only must leave the pending
		// auto-hide alive, exactly as if focus had left text entirely.
		if (textBox.IsReadOnly)
		{
			return;
		}

		_focusVersion++;
		_focusedTextBox = textBox;

		// The dismissed control regaining focus stays quiet — that is how a
		// closing dialog's focus restoration, programmatic Focus() and a
		// hardware-keyboard Tab honor the user's dismissal. A press inside the
		// control re-shows through OnRootPointerPressed instead, and any OTHER
		// editable control focusing falls through to a normal show.
		if (ReferenceEquals(_dismissedTextBox, textBox))
		{
			return;
		}
		InputPane.GetForCurrentView().TryShow();
	}

	void ITextBoxNotificationsProviderSingleton.OnUnfocused(TextBox textBox)
	{
		if (ReferenceEquals(_focusedTextBox, textBox))
		{
			_focusedTextBox = null;
			ScheduleAutoHide();
		}
	}

	void ITextBoxNotificationsProviderSingleton.OnEnteredVisualTree(TextBox textBox)
	{
	}

	void ITextBoxNotificationsProviderSingleton.OnLeaveVisualTree(TextBox textBox)
	{
		// A latched control leaving the tree (page navigation) releases the
		// latch silently, so it cannot haunt a control that no longer exists.
		if (ReferenceEquals(_dismissedTextBox, textBox))
		{
			_dismissedTextBox = null;
		}
		if (ReferenceEquals(_focusedTextBox, textBox))
		{
			_focusedTextBox = null;
			ScheduleAutoHide();
		}
	}

	void ITextBoxNotificationsProviderSingleton.FinishAutofillContext(bool shouldSave)
	{
	}

	void ITextBoxNotificationsProviderSingleton.NotifyValueChanged(TextBox textBox)
	{
	}

	// The user tapped the keyboard's dismiss key: hide, and remember WHOSE
	// keyboard was sent away — the keys are non-focusable, so the text control
	// being typed into still holds focus and would otherwise re-summon it.
	// Only this path arms the latch; the application's own TryHide() is not a
	// statement of user intent.
	private void OnUserDismissed()
	{
		var dismissed = _focusedTextBox;
		InputPane.GetForCurrentView().TryHide();
		_dismissedTextBox = dismissed;
	}

	// Registered once, for already-handled events too: a press anywhere in the
	// application, checked against the latched control. A press inside it is
	// the user actively coming back to the field, which is the one gesture that
	// says "now I DO want the keyboard" — regardless of whether the control
	// still has focus (no focus event fires then) or is being refocused by this
	// very press (whose OnFocused ran latched, and so stayed quiet).
	private void EnsureRootPressHook()
	{
		if (_rootPressHooked || _host.RootElement is not UIElement rootElement)
		{
			return;
		}
		_rootPressHooked = true;
		rootElement.AddHandler(UIElement.PointerPressedEvent,
			new PointerEventHandler(OnRootPointerPressed), handledEventsToo: true);
	}

	private void OnRootPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		if (_dismissedTextBox is not { } dismissed)
		{
			return;
		}
		for (var node = args.OriginalSource as DependencyObject; node is not null;
			node = VisualTreeHelper.GetParent(node))
		{
			if (ReferenceEquals(node, dismissed))
			{
				InputPane.GetForCurrentView().TryShow();
				return;
			}
		}
	}

	// Deferred so that a focus MOVE — blur of one text control followed in the
	// same pass by focus of another — keeps the keyboard up without a flicker:
	// by the time this runs, the newly focused control has already bumped the
	// version and re-claimed it.
	private void ScheduleAutoHide()
	{
		var version = ++_focusVersion;
		if (_host.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() => AutoHideIfStillUnfocused(version));
		}
	}

	// Hiding re-lays-out the page, so it must not happen while a finger is
	// still down: the control being tapped would move out from under it
	// mid-gesture. Focus moves on the PRESS, so without this the layout would
	// change between a press and its release on every tap that leaves a text
	// control — which is most of them.
	private void AutoHideIfStillUnfocused(int version)
	{
		if (_focusVersion != version || _focusedTextBox is not null)
		{
			return;
		}
		if (!ActivePointerTracker.IsPointerDown)
		{
			InputPane.GetForCurrentView().TryHide();
			return;
		}
		// Wait for the finger to lift, then re-check: by then the user may have
		// landed in another text control, which bumps the version and cancels
		// this hide exactly as the deferral above does.
		void OnReleased()
		{
			ActivePointerTracker.AllPointersReleased -= OnReleased;
			if (_host.RootElement is { } rootElement)
			{
				_ = rootElement.Dispatcher.RunAsync(
					Windows.UI.Core.CoreDispatcherPriority.Normal,
					() => AutoHideIfStillUnfocused(version));
			}
		}
		ActivePointerTracker.AllPointersReleased += OnReleased;
	}
}
