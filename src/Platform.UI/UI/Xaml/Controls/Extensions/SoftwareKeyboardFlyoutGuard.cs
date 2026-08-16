#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CodeBrix.Platform.UI.Xaml.Controls.Extensions;

/// <summary>
/// The keyboard-neutrality state machine a text-entry control runs around its
/// OWN flyout marked <see cref="FlyoutBase.DoesNotAffectSoftwareKeyboard"/>
/// (the built-in context menu, say): losing focus to the open flyout and
/// regaining it when the flyout closes must not hide and re-show the software
/// keyboard. Create one guard per (control, flyout) pair; in the control's
/// unfocus path ask <see cref="ShouldSuppressUnfocus"/> and in its focus path
/// <see cref="ShouldSuppressFocus"/>, skipping the software-keyboard
/// notification when true — the keyboard then simply never hears about the
/// round-trip. If the flyout instead closes with focus somewhere ELSE, the
/// guard delivers the suppressed unfocus itself, one dispatcher hop after the
/// close so the flyout's normal focus-restoration runs first — so the keyboard
/// still hides exactly when focus genuinely left the control.
/// </summary>
public sealed class SoftwareKeyboardFlyoutGuard
{
	private readonly Control _control;
	private readonly FlyoutBase _flyout;
	private readonly Action _deliverUnfocus;
	private bool _hold;
	private bool _flyoutShowing;

	/// <param name="control">The text-entry control that owns the flyout.</param>
	/// <param name="flyout">The control's own flyout; only while it carries
	/// <see cref="FlyoutBase.DoesNotAffectSoftwareKeyboard"/> does the guard
	/// suppress anything.</param>
	/// <param name="deliverUnfocus">Sends the control's unfocus notification to
	/// the software keyboard — invoked only for a suppressed unfocus whose
	/// flyout closed with focus elsewhere.</param>
	public SoftwareKeyboardFlyoutGuard(Control control, FlyoutBase flyout, Action deliverUnfocus)
	{
		_control = control;
		_flyout = flyout;
		_deliverUnfocus = deliverUnfocus;
		// IsOpen alone cannot carry the suppression window: FlyoutBase raises
		// Opening, then Open() — during which the presenter loads and TAKES THE
		// FOCUS, firing the control's unfocus — and only then sets IsOpen. So
		// Opening arms the guard for that open-in-progress gap; the focus
		// transfer is synchronous within it, and one dispatcher hop later
		// IsOpen carries the state (and a cancelled opening leaves no arm
		// behind to swallow a genuine unfocus later).
		flyout.Opening += (_, _) =>
		{
			_flyoutShowing = true;
			_ = _control.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
				() => _flyoutShowing = false);
		};
		flyout.Closed += (_, _) => OnFlyoutClosed();
	}

	/// <summary>
	/// Call in the control's unfocus path BEFORE notifying the software
	/// keyboard. True means the focus went into the control's open (or
	/// currently opening) marked flyout: skip the notification, the guard
	/// holds it.
	/// </summary>
	public bool ShouldSuppressUnfocus()
	{
		if ((_flyoutShowing || _flyout.IsOpen) && _flyout.DoesNotAffectSoftwareKeyboard)
		{
			_hold = true;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Call in the control's focus path BEFORE notifying the software keyboard.
	/// True means this is focus returning from the marked flyout (the Paste
	/// click): skip the notification, the round-trip stays invisible.
	/// </summary>
	public bool ShouldSuppressFocus()
	{
		if (_hold)
		{
			_hold = false;
			return true;
		}
		return false;
	}

	private void OnFlyoutClosed()
	{
		if (!_hold)
		{
			return;
		}
		_ = _control.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
		{
			// Focus restoration that ran during the hop consumed the hold via
			// ShouldSuppressFocus; a hold still standing means focus really
			// went elsewhere, so the withheld unfocus is delivered now.
			if (!_hold)
			{
				return;
			}
			_hold = false;
			if (_control.FocusState == FocusState.Unfocused)
			{
				_deliverUnfocus();
			}
		});
	}
}
