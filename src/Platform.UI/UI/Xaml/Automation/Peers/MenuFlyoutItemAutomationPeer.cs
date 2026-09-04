// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference MenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes MenuFlyoutItem types to UI Automation.
/// </summary>
public partial class MenuFlyoutItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyoutItemAutomationPeer class.
	/// </summary>
	/// <param name="owner">The MenuFlyoutItem this peer represents.</param>
	public MenuFlyoutItemAutomationPeer(MenuFlyoutItem owner) : base(owner)
	{
	}

	/// <inheritdoc />
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(MenuFlyoutItem);

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

	/// <summary>
	/// Sends a request to invoke the menu item associated with the automation peer: it raises the
	/// item's Click event, runs its command and closes the menu.
	/// </summary>
	public void Invoke()
	{
		if (IsEnabled() && Owner is MenuFlyoutItem item)
		{
			item.Invoke();
		}
	}
}
