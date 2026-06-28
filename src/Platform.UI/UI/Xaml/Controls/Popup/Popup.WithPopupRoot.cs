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

				var currentXamlRoot = XamlRoot ?? Child?.XamlRoot ?? WinUICoreServices.Instance.ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.XamlRoot;
				_closePopup.Disposable = currentXamlRoot?.OpenPopup(this);

			}
			else
			{
				_closePopup.Disposable = null;
				PopupPanel.Visibility = Visibility.Collapsed;
			}
		}

		if (newIsOpen)
		{
#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
			// TODO: Add EventManager.RaiseEvent method and use it here.
			NativeDispatcher.Main.Enqueue(() => Opened?.Invoke(this, newIsOpen), NativeDispatcherPriority.Normal);
#else
			Opened?.Invoke(this, newIsOpen);
#endif
		}
		else
		{
#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
			NativeDispatcher.Main.Enqueue(() => Closed?.Invoke(this, newIsOpen), NativeDispatcherPriority.Normal);
#else
			Closed?.Invoke(this, newIsOpen);
#endif
		}
	}

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
