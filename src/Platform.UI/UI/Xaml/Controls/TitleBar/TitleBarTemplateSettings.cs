// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBarTemplateSettings.idl, TitleBarTemplateSettings.cpp, TitleBarTemplateSettings.properties.cpp, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a
/// <see cref="TitleBar"/>.
/// </summary>
public partial class TitleBarTemplateSettings : DependencyObject
{
	/// <summary>Initializes a new instance of the <see cref="TitleBarTemplateSettings"/> class.</summary>
	public TitleBarTemplateSettings()
	{
	}

	/// <summary>Gets or sets the icon element of the title bar (built from <see cref="TitleBar.IconSource"/>).</summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>Identifies the <see cref="IconElement"/> dependency property.</summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TitleBarTemplateSettings),
			new FrameworkPropertyMetadata(null));
}
