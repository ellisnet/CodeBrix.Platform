using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Xaml.Core;
using WinUICoreServices = CodeBrix.Platform.UI.Xaml.Core.CoreServices;
using CodeBrix.Platform.UI.Dispatching;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private readonly SerialDisposable _closePopup = new();

	// Set while EnsureOpenedInRoot runs: assigning the XamlRoot inside it must not re-enter it.
	private bool _isOpeningInRoot;

	// The missing-XamlRoot warning is logged once per Popup.
	private bool _missingXamlRootReported;

#if false
	private bool _useNativePopup = FeatureConfiguration.Popup.UseNativePopup;
	internal bool UseNativePopup => _useNativePopup;
#endif

	partial void InitializePartial()
	{
#if false
		if (_useNativePopup)
		{
			InitializeNativePartial();
		}
#endif

		PopupPanel = new PopupPanel(this);
	}

#if false
	partial void InitializeNativePartial();
#endif

	partial void OnChildChangedPartialNative(UIElement oldChild, UIElement newChild)
	{
		PopupPanel.Children.Remove(oldChild);

		if (newChild != null)
		{
			PopupPanel.Children.Add(newChild);
		}
	}

	partial void OnIsLightDismissEnabledChangedPartialNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
	{
#if false
		if (_useNativePopup)
		{
			OnIsLightDismissEnabledChangedNative(oldIsLightDismissEnabled, newIsLightDismissEnabled);
		}
		else
#endif
		{
			if (PopupPanel != null)
			{
				PopupPanel.Background = GetPanelBackground();
			}
		}
	}

#if false
	partial void OnIsLightDismissEnabledChangedNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled);
#endif

	partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen)
	{
		if (this.Log().IsEnabled(CodeBrix.Platform.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Popup.IsOpenChanged({oldIsOpen}, {newIsOpen})");
		}

#if false
		if (_useNativePopup)
		{
			OnIsOpenChangedNative(oldIsOpen, newIsOpen);
		}
		else
#endif
		{
			if (newIsOpen)
			{
				EnsureOpenedInRoot();
			}
			else
			{
				_closePopup.Disposable = null;
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		if (!newIsOpen)
		{
#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
			NativeDispatcher.Main.Enqueue(() => Closed?.Invoke(this, newIsOpen), NativeDispatcherPriority.Normal);
#else
			Closed?.Invoke(this, newIsOpen);
#endif
		}
	}

	/// <summary>
	/// Shows the Popup in its XamlRoot's PopupRoot when IsOpen is true and it is not shown yet; otherwise does nothing
	/// (idempotent). Called when IsOpen becomes true, when the Popup is loaded, and when a XamlRoot is assigned to it,
	/// so a Popup opened before it could reach a XamlRoot (declared open in XAML, opened and then added to a panel,
	/// parentless with its XamlRoot set afterwards) is shown as soon as it can be. <see cref="Opened"/> is raised when
	/// the Popup is actually shown. A parentless Popup that has no XamlRoot stays closed (its IsOpen stays true) and
	/// logs one warning: it needs its XamlRoot set, as in WinUI 3.
	/// </summary>
	private void EnsureOpenedInRoot()
	{
		if (!IsOpen || _isOpeningInRoot || _closePopup.Disposable is not null)
		{
			return;
		}

		var xamlRoot = ResolveOpenXamlRoot();
		if (xamlRoot is null)
		{
			ReportMissingXamlRoot();
			return;
		}

		_isOpeningInRoot = true;
		try
		{
			RegisterOpenInRoot(xamlRoot);

#if !HAS_CODEBRIX_WINUI
			// In UWP, XamlRoot is set automatically to CoreWindow XamlRoot if not set beforehand.
			if (XamlRoot is null && Child?.XamlRoot is null && WinUICoreServices.Instance.InitializationType != InitializationType.IslandsOnly)
			{
				XamlRoot = WinUICoreServices.Instance.ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.XamlRoot;
			}
#endif

			// It's important for PopupPanel to be visible before the popup is opened so that
			// child controls can be IsFocusable, which depends on all ancestors (including PopupPanel)
			// being visible
			PopupPanel.Visibility = Visibility.Visible;

			var currentXamlRoot = ResolveOpenXamlRoot();
			_closePopup.Disposable = currentXamlRoot?.OpenPopup(this);
		}
		finally
		{
			_isOpeningInRoot = false;
		}

#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
		// TODO: Add EventManager.RaiseEvent method and use it here.
		NativeDispatcher.Main.Enqueue(() => Opened?.Invoke(this, true), NativeDispatcherPriority.Normal);
#else
		Opened?.Invoke(this, true);
#endif
	}

	/// <summary>
	/// Logs, once per Popup, that a parentless Popup was opened with no XamlRoot to open in. A Popup that has a parent
	/// is not reported: it is shown when it is loaded.
	/// </summary>
	private void ReportMissingXamlRoot()
	{
		if (_missingXamlRootReported || Parent is not null || VisualTreeHelper.GetParent(this) is not null)
		{
			return;
		}

		_missingXamlRootReported = true;
		MissingXamlRootReports++;

		if (this.Log().IsEnabled(CodeBrix.Platform.Foundation.Logging.LogLevel.Warning))
		{
			this.Log().Warn(
				$"The Popup '{Name}' was opened with no parent and no XamlRoot, so it stays closed. " +
				"Set its XamlRoot (for example popup.XamlRoot = someElement.XamlRoot) before opening a parentless Popup, " +
				"or add it to the visual tree.");
		}
	}

	/// <summary>How many times this Popup logged the missing-XamlRoot warning (0 or 1).</summary>
	internal int MissingXamlRootReports { get; private set; }

#if false
	partial void OnIsOpenChangedNative(bool oldIsOpen, bool newIsOpen);
#endif

	partial void OnPopupPanelChangedPartial(PopupPanel previousPanel, PopupPanel newPanel)
	{
#if false
		if (_useNativePopup)
		{
			OnPopupPanelChangedPartialNative(previousPanel, newPanel);
		}
		else
#endif
		{
			previousPanel?.Children.Clear();

			if (newPanel != null)
			{
				if (Child != null)
				{
					newPanel.Children.Add(Child);
				}
				newPanel.Background = GetPanelBackground();
			}
		}
	}

#if false
	partial void OnPopupPanelChangedPartialNative(PopupPanel previousPanel, PopupPanel newPanel);
#endif
}
