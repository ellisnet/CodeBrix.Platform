// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBarAutomationPeer.h, TitleBarAutomationPeer.cpp, commit 5f9e85113

using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>Exposes <see cref="TitleBar"/> types to Microsoft UI Automation.</summary>
public partial class TitleBarAutomationPeer : FrameworkElementAutomationPeer
{
	/// <summary>Initializes a new instance of the <see cref="TitleBarAutomationPeer"/> class.</summary>
	/// <param name="owner">The TitleBar associated with this automation peer.</param>
	public TitleBarAutomationPeer(TitleBar owner) : base(owner)
	{
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;

	/// <inheritdoc />
	protected override string GetClassNameCore() => nameof(TitleBar);

	/// <inheritdoc />
	protected override string GetNameCore()
	{
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name) && Owner is TitleBar titleBar)
		{
			name = titleBar.Title;
		}

		return name ?? string.Empty;
	}
}
