// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHost.cs

using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using WUX = Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.XamlHost.Skia.Wpf //Was previously: Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// CodeBrixXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
	/// </summary>
	public partial class CodeBrixXamlHost : CodeBrixXamlHostBase
	{
		private static readonly Style _style = (Style)XamlReader.Parse(
			"""
			<Style xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				   xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				   xmlns:xamlHostWpf="clr-namespace:CodeBrix.Platform.UI.XamlHost.Skia.Wpf;assembly=CodeBrix.Platform.UI.XamlHost.Skia.Wpf"
				   TargetType="{x:Type xamlHostWpf:CodeBrixXamlHost}">
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="{x:Type xamlHostWpf:CodeBrixXamlHost}">
							<Border Background="{TemplateBinding Background}"
									BorderBrush="{TemplateBinding BorderBrush}"
									BorderThickness="{TemplateBinding BorderThickness}">
								<Canvas x:Name="NativeOverlayLayer" />
							</Border>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");

		public CodeBrixXamlHost()
		{
			this.Style = _style;
			this.DataContextChanged += CodeBrixXamlHost_DataContextChanged;
			this.Loaded += OnLoaded;
		}

		private void CodeBrixXamlHost_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			PropagateDataContext();
		}

		private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Child = CreateXamlContent();
			TryLoadContent();

			// Specifically on Uno islands, the CodeBrixXamlHost isn't focused by default
			Focus();
		}

		/// <summary>
		/// Gets or sets the root UWP XAML element displayed in the WPF control instance.
		/// </summary>
		/// <remarks>This UWP XAML element is the root element of the wrapped DesktopWindowXamlSource.</remarks>
		[Browsable(true)]
		public WUX.UIElement Child
		{
			get => ChildInternal;

			set => ChildInternal = value;
		}
	}
}
