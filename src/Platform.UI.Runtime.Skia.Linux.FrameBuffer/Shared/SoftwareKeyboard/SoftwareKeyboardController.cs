// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
/// when a TextBox or PasswordBox gains focus and auto-hides when focus leaves
/// (a focus move between two text controls does not flicker the keyboard).</item>
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

	internal SoftwareKeyboardController(IXamlRootHost host, ISoftwareKeyInjector injector,
		SoftwareKeyboardOptions options)
	{
		_host = host;
		_injector = injector;
		_options = options;
	}

	public bool TryShow()
	{
		if (_visible)
		{
			return false;
		}
		var xamlRoot = (_host.RootElement as FrameworkElement)?.XamlRoot;
		if (xamlRoot is null)
		{
			return false;
		}

		if (_view is null || _popup is null)
		{
			var active = KeyboardLayoutCatalog.ResolveActive(_options.Layout);
			var enabled = KeyboardLayoutCatalog.ResolveEnabled(active, _options.EnabledLayouts);
			_view = new SoftwareKeyboardView(_injector, enabled);
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
		if (_host.RootElement is RootVisual rootVisual)
		{
			rootVisual.ContentBottomOcclusionInset = 0;
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
		var keyboardHeight = SoftwareKeyboardView.ComputeHeight(size);
		_view.ApplyMetrics(size.Width, keyboardHeight);
		_popup.HorizontalOffset = 0;
		_popup.VerticalOffset = size.Height - keyboardHeight;

		if (_host.RootElement is RootVisual rootVisual)
		{
			rootVisual.ContentBottomOcclusionInset = keyboardHeight;
		}
		InputPane.GetForCurrentView().OccludedRect
			= new Rect(0, size.Height - keyboardHeight, size.Width, keyboardHeight);
	}

	void ITextBoxNotificationsProviderSingleton.OnFocused(TextBox textBox)
	{
		_focusVersion++;
		_focusedTextBox = textBox;
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
				() =>
				{
					if (_focusVersion == version && _focusedTextBox is null)
					{
						InputPane.GetForCurrentView().TryHide();
					}
				});
		}
	}
}
