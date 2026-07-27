// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListPickerFlyoutPresenterAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ListPickerFlyoutPresenter types to Microsoft UI Automation.
/// </summary>
public partial class ListPickerFlyoutPresenterAutomationPeer : FrameworkElementAutomationPeer
{
	private const string UIA_AP_LISTPICKERFLYOUT_NAME = nameof(UIA_AP_LISTPICKERFLYOUT_NAME);

	internal ListPickerFlyoutPresenterAutomationPeer()
	{

	}

	protected override string GetClassNameCore() => nameof(Controls.ListPickerFlyoutPresenter);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Pane;

	protected override string GetNameCore()
	{
		//UNO TODO: Private.FindStringResource
		//return Private.FindStringResource(UIA_AP_LISTPICKERFLYOUT_NAME);
		//CODEBRIX-DIVERGENCE 2026-07-26: the UIA_AP_LISTPICKERFLYOUT_NAME resource was REMOVED
		//  from every locale of UI/Xaml/Controls/WinUIResources - it held a value belonging to
		//  another key (an upstream misalignment) and had no donor to repair it from. Restore a
		//  correct string there before re-enabling the lookup above, or it will return nothing.

		return base.GetNameCore();
	}
}
