// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBar.cpp, commit fc2f82117

using System;
using System.Collections.Generic;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Helpers.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Xaml.Controls;

partial class TitleBar
{
	/// <summary>Initializes a new instance of the <see cref="TitleBar"/> class.</summary>
	public TitleBar()
	{
		SetValue(TemplateSettingsProperty, new TitleBarTemplateSettings());

		DefaultStyleKey = typeof(TitleBar);

		SizeChanged += OnSizeChanged;
		RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, OnFlowDirectionChanged);

		// Platform-specific: WinUI subscribes to the window's InputActivationListener in OnApplyTemplate and revokes it in
		// its destructor; here the window's Activated event is followed while the control is in the live tree.
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (XamlRoot?.HostWindow is { } window)
		{
			window.Activated += OnWindowActivated;
			m_windowActivatedToken.Disposable = Disposable.Create(() => window.Activated -= OnWindowActivated);

			var appTitleBar = window.AppWindow.TitleBar;
			appTitleBar.ExtendsContentIntoTitleBarChanged += OnExtendsContentIntoTitleBarChanged;
			m_extendsContentChangedToken.Disposable = Disposable.Create(() => appTitleBar.ExtendsContentIntoTitleBarChanged -= OnExtendsContentIntoTitleBarChanged);
		}

		UpdateTitle();
		UpdatePadding();
		UpdateInteractableElementsList();
		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		m_windowActivatedToken.Disposable = null;
		m_extendsContentChangedToken.Disposable = null;

		// Platform-specific: WinUI's destructor restores the window title if it still shows the title this control
		// applied; a control leaving the live tree is the nearest equivalent here (there is no deterministic destructor).
		ResetTitle(m_lastAppliedTitle);
	}

	private void OnExtendsContentIntoTitleBarChanged(bool extends)
	{
		UpdatePadding();
		UpdateDragRegion();
		UpdateIconRegion();
	}

	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() =>
		new TitleBarAutomationPeer(this);

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		m_leftPaddingColumn = GetTemplateChild(s_leftPaddingColumnName) as ColumnDefinition;
		m_rightPaddingColumn = GetTemplateChild(s_rightPaddingColumnName) as ColumnDefinition;

		// A new template: the parts found in the old one are gone.
		m_backButton = null;
		m_paneToggleButton = null;
		m_iconViewbox = null;
		m_leftHeaderArea = null;
		m_contentArea = null;
		m_contentAreaGrid = null;
		m_backButtonClickRevoker.Disposable = null;
		m_paneToggleButtonClickRevoker.Disposable = null;

		UpdateHeight();
		UpdatePadding();
		UpdateIcon();
		UpdateBackButton();
		UpdatePaneToggleButton();
		UpdateTitle();
		UpdateSubtitle();
		UpdateLeftHeader();
		UpdateContent();
		UpdateRightHeader();
		UpdateInteractableElementsList();
		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void HandleTitleChange(string oldTitle, string newTitle)
	{
		// If transitioning from non-empty to empty, prefer ResetTitle to avoid overwriting external titles.
		if (!string.IsNullOrEmpty(oldTitle) && string.IsNullOrEmpty(newTitle))
		{
			ResetTitle(oldTitle);
			GoToState(s_titleTextCollapsedVisualStateName, false);
		}
		else
		{
			UpdateTitle();
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		if (property == IsBackButtonVisibleProperty)
		{
			UpdateBackButton();
		}
		else if (property == IsBackButtonEnabledProperty)
		{
			UpdateInteractableElementsList();
		}
		else if (property == IsPaneToggleButtonVisibleProperty)
		{
			UpdatePaneToggleButton();
		}
		else if (property == IconSourceProperty)
		{
			UpdateIcon();
		}
		else if (property == TitleProperty)
		{
			HandleTitleChange(args.OldValue as string ?? "", Title);
		}
		else if (property == SubtitleProperty)
		{
			UpdateSubtitle();
		}
		else if (property == LeftHeaderProperty)
		{
			UpdateLeftHeader();
		}
		else if (property == ContentProperty)
		{
			UpdateContent();
		}
		else if (property == RightHeaderProperty)
		{
			UpdateRightHeader();
		}

		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void GoToState(string stateName, bool useTransitions)
		=> VisualStateManager.GoToState(this, stateName, useTransitions);

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		if (Content != null)
		{
			var contentArea = m_contentArea;
			var contentAreaGrid = m_contentAreaGrid;

			if (contentArea is not null && contentAreaGrid is not null)
			{
				if (m_compactModeThresholdWidth == 0.0 && contentArea.DesiredSize.Width >= contentAreaGrid.ActualWidth)
				{
					m_compactModeThresholdWidth = args.NewSize.Width;
					m_isCompact = true;
					GoToState(s_compactVisualStateName, false);
				}
				else if (m_isCompact && args.NewSize.Width >= m_compactModeThresholdWidth)
				{
					m_compactModeThresholdWidth = 0.0;
					m_isCompact = false;
					GoToState(s_expandedVisualStateName, false);
					UpdateTitle();
					UpdateSubtitle();
				}
			}
		}

		// Platform-specific: WinUI reads the caption insets once per template (its TODO 50724421); the caption buttons of
		// the window chrome can change size, so the insets are read again whenever the control is resized.
		UpdatePadding();
		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty args)
		=> UpdatePadding();

	private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
	{
		var isDeactivated = args.WindowActivationState == global::Windows.UI.Core.CoreWindowActivationState.Deactivated;

		if (IsBackButtonVisible && IsBackButtonEnabled)
		{
			GoToState(isDeactivated ? s_backButtonDeactivatedVisualStateName : s_backButtonVisibleVisualStateName, false);
		}

		if (IsPaneToggleButtonVisible)
		{
			GoToState(isDeactivated ? s_paneToggleButtonDeactivatedVisualStateName : s_paneToggleButtonVisibleVisualStateName, false);
		}

		if (IconSource != null)
		{
			GoToState(isDeactivated ? s_iconDeactivatedVisualStateName : s_iconVisibleVisualStateName, false);
		}

		if (!string.IsNullOrEmpty(Title) && !m_isCompact)
		{
			GoToState(isDeactivated ? s_titleTextDeactivatedVisualStateName : s_titleTextVisibleVisualStateName, false);
		}

		if (!string.IsNullOrEmpty(Subtitle) && !m_isCompact)
		{
			GoToState(isDeactivated ? s_subtitleTextDeactivatedVisualStateName : s_subtitleTextVisibleVisualStateName, false);
		}

		if (LeftHeader != null)
		{
			GoToState(isDeactivated ? s_leftHeaderDeactivatedVisualStateName : s_leftHeaderVisibleVisualStateName, false);
		}

		if (Content != null)
		{
			GoToState(isDeactivated ? s_contentDeactivatedVisualStateName : s_contentVisibleVisualStateName, false);
		}

		if (RightHeader != null)
		{
			GoToState(isDeactivated ? s_rightHeaderDeactivatedVisualStateName : s_rightHeaderVisibleVisualStateName, false);
		}

		UpdateIconRegion();
	}

	private void OnBackButtonClick(object sender, RoutedEventArgs args)
		=> BackRequested?.Invoke(this, null);

	private void OnPaneToggleButtonClick(object sender, RoutedEventArgs args)
		=> PaneToggleRequested?.Invoke(this, null);

	private void UpdateIcon()
	{
		var templateSettings = TemplateSettings;
		if (IconSource is { } source)
		{
			if (m_iconViewbox is null)
			{
				m_iconViewbox = GetTemplateChild(s_iconViewboxPartName) as FrameworkElement;
			}

			if (m_iconViewbox is { } iconViewbox)
			{
				iconViewbox.LayoutUpdated += OnIconLayoutUpdated;
				m_iconLayoutUpdatedRevoker.Disposable = Disposable.Create(() => iconViewbox.LayoutUpdated -= OnIconLayoutUpdated);
			}
			else
			{
				m_iconLayoutUpdatedRevoker.Disposable = null;
			}

			templateSettings.IconElement = SharedHelpers.MakeIconElementFrom(source);
			GoToState(s_iconVisibleVisualStateName, false);
			m_iconViewbox ??= GetTemplateChild(s_iconViewboxPartName) as FrameworkElement;
		}
		else
		{
			m_iconLayoutUpdatedRevoker.Disposable = null;
			templateSettings.IconElement = null;
			GoToState(s_iconCollapsedVisualStateName, false);
		}

		UpdateDragRegion();
		UpdateIconRegion();
	}

	private void OnIconLayoutUpdated(object sender, object args)
		=> UpdateIconRegion();

	private void OnContentLayoutUpdated(object sender, object args)
	{
		// Content layout has changed (children added/removed/resized): refresh the interactable elements and drag region.
		UpdateInteractableElementsList();
		UpdateDragRegion();
	}

	private void UpdateBackButton()
	{
		if (IsBackButtonVisible)
		{
			if (m_backButton is null)
			{
				LoadBackButton();
			}

			GoToState(s_backButtonVisibleVisualStateName, false);

			if (m_backButton is null)
			{
				// Platform-specific: a deferred (x:DeferLoadStrategy) part may only exist once its visual state has
				// realized it.
				LoadBackButton();
			}
		}
		else
		{
			GoToState(s_backButtonCollapsedVisualStateName, false);
		}

		UpdateInteractableElementsList();
		UpdateLeftHeaderSpacing();
	}

	private void UpdatePaneToggleButton()
	{
		if (IsPaneToggleButtonVisible)
		{
			if (m_paneToggleButton is null)
			{
				LoadPaneToggleButton();
			}

			GoToState(s_paneToggleButtonVisibleVisualStateName, false);

			if (m_paneToggleButton is null)
			{
				// A deferred part may only exist once its visual state has realized it.
				LoadPaneToggleButton();
			}
		}
		else
		{
			GoToState(s_paneToggleButtonCollapsedVisualStateName, false);
		}

		UpdateInteractableElementsList();
		UpdateLeftHeaderSpacing();
	}

	private void UpdateHeight()
	{
		GoToState((Content == null && LeftHeader == null && RightHeader == null) ?
			s_compactHeightVisualStateName : s_expandedHeightVisualStateName,
			false);
	}

	private void UpdatePadding()
	{
		// WinUI sizes the padding columns to the AppWindowTitleBar's LeftInset / RightInset (the system caption buttons).
		// Platform-specific: the caption buttons of an extended window are drawn by the window chrome, so its caption
		// buttons' width is the inset on their side (the right, left to right); a window whose content is not extended
		// has no inset. Without a window (a control not hosted in one) the template's padding stays as it is.
		if (TryGetAppWindow() is not { } appWindow || XamlRoot?.HostWindow is not { } window)
		{
			return;
		}

		var captionInset = appWindow.TitleBar.ExtendsContentIntoTitleBar ? window.GetCaptionButtonsInset() : 0;
		var leftInset = 0d;
		var rightInset = captionInset;

		if (m_leftPaddingColumn is { } leftColumn)
		{
			var width = FlowDirection == FlowDirection.LeftToRight ? leftInset : rightInset;
			if (leftColumn.Width.Value != width || !leftColumn.Width.IsAbsolute)
			{
				leftColumn.Width = GridLengthHelper.FromPixels(width);
			}
		}

		if (m_rightPaddingColumn is { } rightColumn)
		{
			var width = FlowDirection == FlowDirection.LeftToRight ? rightInset : leftInset;
			if (rightColumn.Width.Value != width || !rightColumn.Width.IsAbsolute)
			{
				rightColumn.Width = GridLengthHelper.FromPixels(width);
			}
		}
	}

	private void UpdateTitle()
	{
		var titleText = Title;
		var appWindow = TryGetAppWindow();

		// Capture default title once.
		if (appWindow is not null && !m_hasDefaultAppWindowTitle)
		{
			m_defaultAppWindowTitle = appWindow.Title;
			m_hasDefaultAppWindowTitle = true;
		}

		if (string.IsNullOrEmpty(titleText))
		{
			// Do not set appWindow.Title here. Reset is handled by ResetTitle via OnPropertyChanged.
			GoToState(s_titleTextCollapsedVisualStateName, false);
			return;
		}

		// Only set the window title if it actually needs to change.
		if (appWindow is not null)
		{
			if (appWindow.Title != titleText)
			{
				appWindow.Title = titleText;
			}

			m_lastAppliedTitle = titleText;
		}

		GoToState(s_titleTextVisibleVisualStateName, false);
	}

	private void ResetTitle(string lastAppliedTitle)
	{
		if (!m_hasDefaultAppWindowTitle || string.IsNullOrEmpty(lastAppliedTitle))
		{
			return;
		}

		var appWindow = TryGetAppWindow();
		if (appWindow is null)
		{
			return;
		}

		// Restore only if the current title matches what we previously applied.
		var currentTitle = appWindow.Title;
		if (lastAppliedTitle == currentTitle && currentTitle != m_defaultAppWindowTitle)
		{
			appWindow.Title = m_defaultAppWindowTitle;
			m_hasDefaultAppWindowTitle = false;
			m_lastAppliedTitle = null;
		}
	}

	private void UpdateSubtitle()
	{
		GoToState(string.IsNullOrEmpty(Subtitle) ? s_subtitleTextCollapsedVisualStateName : s_subtitleTextVisibleVisualStateName, false);
	}

	private void UpdateLeftHeader()
	{
		if (LeftHeader == null)
		{
			GoToState(s_leftHeaderCollapsedVisualStateName, false);
		}
		else
		{
			if (m_leftHeaderArea is null)
			{
				m_leftHeaderArea = GetTemplateChild(s_leftHeaderPresenterPartName) as FrameworkElement;
			}

			GoToState(s_leftHeaderVisibleVisualStateName, false);
			m_leftHeaderArea ??= GetTemplateChild(s_leftHeaderPresenterPartName) as FrameworkElement;
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private void UpdateContent()
	{
		m_contentLayoutUpdatedRevoker.Disposable = null;

		if (Content == null)
		{
			GoToState(s_contentCollapsedVisualStateName, false);
		}
		else
		{
			if (m_contentArea is null)
			{
				m_contentAreaGrid = GetTemplateChild(s_contentPresenterGridPartName) as Grid;
				m_contentArea = GetTemplateChild(s_contentPresenterPartName) as FrameworkElement;
			}

			// LayoutUpdated fires after Loaded + Measure + Arrange, so the elements have valid bounds.
			if (Content is FrameworkElement content)
			{
				content.LayoutUpdated += OnContentLayoutUpdated;
				m_contentLayoutUpdatedRevoker.Disposable = Disposable.Create(() => content.LayoutUpdated -= OnContentLayoutUpdated);
			}

			GoToState(s_contentVisibleVisualStateName, false);
			m_contentAreaGrid ??= GetTemplateChild(s_contentPresenterGridPartName) as Grid;
			m_contentArea ??= GetTemplateChild(s_contentPresenterPartName) as FrameworkElement;
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private void UpdateRightHeader()
	{
		if (RightHeader == null)
		{
			GoToState(s_rightHeaderCollapsedVisualStateName, false);
		}
		else
		{
			if (m_rightHeaderArea is null)
			{
				m_rightHeaderArea = GetTemplateChild(s_rightHeaderPresenterPartName) as FrameworkElement;
			}

			GoToState(s_rightHeaderVisibleVisualStateName, false);
			m_rightHeaderArea ??= GetTemplateChild(s_rightHeaderPresenterPartName) as FrameworkElement;
		}

		UpdateHeight();
		UpdateInteractableElementsList();
	}

	private RectInt32 GetBounds(FrameworkElement element)
	{
		var transformBounds = element.TransformToVisual(null);
		var bounds = transformBounds.TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));

		var scale = XamlRoot?.RasterizationScale ?? 1.0;
		return new RectInt32(
			(int)(bounds.X * scale),
			(int)(bounds.Y * scale),
			(int)(bounds.Width * scale),
			(int)(bounds.Height * scale));
	}

	// Once the TitleBar is the window's title bar (content extended into the title-bar area), the whole region's input is
	// non-client (it drags the window); a hole is punched out for each interactable element of the TitleBar.
	private void UpdateDragRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (m_interactableElementsList.Count != 0)
			{
				var passthroughRects = new List<RectInt32>();

				foreach (var frameworkElement in m_interactableElementsList)
				{
					var transparentRect = GetBounds(frameworkElement);
					if (transparentRect.X >= 0 || transparentRect.Y >= 0)
					{
						passthroughRects.Add(transparentRect);
					}
				}

				// Skip the SetRegionRects call if the rects haven't changed since last update.
				var rectsUnchanged = passthroughRects.Count == m_previousPassthroughRects.Count;
				for (var i = 0; rectsUnchanged && i < passthroughRects.Count; i++)
				{
					var a = passthroughRects[i];
					var b = m_previousPassthroughRects[i];
					rectsUnchanged = a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
				}

				if (rectsUnchanged)
				{
					return;
				}

				m_previousPassthroughRects.Clear();
				m_previousPassthroughRects.AddRange(passthroughRects);

				nonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, passthroughRects.ToArray());
			}
			else
			{
				if (m_previousPassthroughRects.Count == 0)
				{
					return;
				}

				m_previousPassthroughRects.Clear();
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Passthrough);
			}
		}
	}

	private void UpdateIconRegion()
	{
		if (GetInputNonClientPointerSource() is { } nonClientPointerSource)
		{
			if (IconSource != null)
			{
				if (m_iconViewbox is { } iconViewbox)
				{
					var iconRects = new List<RectInt32>();
					var iconRect = GetBounds(iconViewbox);
					if (iconRect.X >= 0 || iconRect.Y >= 0)
					{
						iconRects.Add(iconRect);
					}

					nonClientPointerSource.SetRegionRects(NonClientRegionKind.Icon, iconRects.ToArray());
				}
			}
			else
			{
				nonClientPointerSource.ClearRegionRects(NonClientRegionKind.Icon);
			}
		}
	}

	private void UpdateInteractableElementsList()
	{
		m_interactableElementsList.Clear();

		if (IsBackButtonVisible && IsBackButtonEnabled && m_backButton is { } backButton)
		{
			m_interactableElementsList.Add(backButton);
		}

		if (IsPaneToggleButtonVisible && m_paneToggleButton is { } paneToggleButton)
		{
			m_interactableElementsList.Add(paneToggleButton);
		}

		if (LeftHeader != null && m_leftHeaderArea is { } leftHeaderArea)
		{
			m_interactableElementsList.Add(leftHeaderArea);
		}

		if (Content != null && m_contentArea is { } contentArea)
		{
			// Recursively find the interactive controls of the content.
			FindInteractableElements(contentArea);
		}

		if (RightHeader != null && m_rightHeaderArea is { } rightHeaderArea)
		{
			m_interactableElementsList.Add(rightHeaderArea);
		}
	}

	private void UpdateLeftHeaderSpacing()
	{
		GoToState(
			IsBackButtonVisible == IsPaneToggleButtonVisible ?
			s_defaultSpacingVisualStateName : s_negativeInsetVisualStateName,
			false);
	}

	private void LoadBackButton()
	{
		m_backButton = GetTemplateChild(s_backButtonPartName) as Button;

		if (m_backButton is { } backButton)
		{
			backButton.Click += OnBackButtonClick;
			m_backButtonClickRevoker.Disposable = Disposable.Create(() => backButton.Click -= OnBackButtonClick);

			// Do localization for the back button
			if (string.IsNullOrEmpty(AutomationProperties.GetName(backButton)))
			{
				var backButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonName);
				AutomationProperties.SetName(backButton, backButtonName);
			}

			// Setup the tooltip for the back button
			var tooltip = new ToolTip();
			tooltip.Content = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationBackButtonToolTip);
			ToolTipService.SetToolTip(backButton, tooltip);
		}
	}

	private void LoadPaneToggleButton()
	{
		m_paneToggleButton = GetTemplateChild(s_paneToggleButtonPartName) as Button;

		if (m_paneToggleButton is { } paneToggleButton)
		{
			paneToggleButton.Click += OnPaneToggleButtonClick;
			m_paneToggleButtonClickRevoker.Disposable = Disposable.Create(() => paneToggleButton.Click -= OnPaneToggleButtonClick);

			// Do localization for paneToggleButton
			if (string.IsNullOrEmpty(AutomationProperties.GetName(paneToggleButton)))
			{
				var paneToggleButtonName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationButtonToggleName);
				AutomationProperties.SetName(paneToggleButton, paneToggleButtonName);
			}

			// Setup the tooltip for the paneToggleButton
			var tooltip = new ToolTip();
			tooltip.Content = AutomationProperties.GetName(paneToggleButton);
			ToolTipService.SetToolTip(paneToggleButton, tooltip);
		}
	}

	/// <summary>
	/// The id of the window the control is in. Platform-specific: WinUI reads it from
	/// XamlRoot.ContentIslandEnvironment.AppWindowId (not implemented here); the XamlRoot's host window gives the same id.
	/// </summary>
	private WindowId GetAppWindowId()
	{
		var appWindowId = XamlRoot?.HostWindow?.AppWindow?.Id ?? default;

		if (appWindowId.Value != m_lastAppWindowId.Value)
		{
			m_lastAppWindowId = appWindowId;
			m_inputNonClientPointerSource = null;
			m_appWindow = null;
		}

		return appWindowId;
	}

	/// <summary>
	/// The window's non-client input source, when the window's content is extended into its title-bar area (only then is
	/// the TitleBar in the non-client area whose drag and passthrough regions it sets); else null.
	/// </summary>
	private InputNonClientPointerSource GetInputNonClientPointerSource()
	{
		var appWindowId = GetAppWindowId();
		if (appWindowId.Value == 0 || TryGetAppWindow() is not { TitleBar.ExtendsContentIntoTitleBar: true })
		{
			return null;
		}

		if (m_inputNonClientPointerSource is null && InputNonClientPointerSource.TryGetForWindowId(appWindowId, out var source))
		{
			m_inputNonClientPointerSource = source;
		}

		return m_inputNonClientPointerSource;
	}

	// Helper to retrieve and cache AppWindow for current WindowId
	private AppWindow TryGetAppWindow()
	{
		var appWindowId = GetAppWindowId();
		if (appWindowId.Value == 0)
		{
			m_appWindow = null;
			return null;
		}

		m_appWindow ??= XamlRoot?.HostWindow?.AppWindow;
		return m_appWindow;
	}

	private void FindInteractableElements(DependencyObject element)
	{
		if (element is not UIElement uiElement)
		{
			return;
		}

		// Skip elements that are not visible or not hit-testable.
		if (uiElement.Visibility != Visibility.Visible || !uiElement.IsHitTestVisible)
		{
			return;
		}

		// An enabled control is interactable; its bounds cover its internal elements.
		if (uiElement is Control { IsEnabled: true } control)
		{
			m_interactableElementsList.Add(control);
			return;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < childCount; i++)
		{
			FindInteractableElements(VisualTreeHelper.GetChild(element, i));
		}
	}
}
