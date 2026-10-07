// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ListViewAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ListView types to Microsoft UI Automation.
/// </summary>
public partial class ListViewAutomationPeer : ListViewBaseAutomationPeer
{
	public ListViewAutomationPeer(ListView owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
		=> nameof(ListView);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.List;
}
