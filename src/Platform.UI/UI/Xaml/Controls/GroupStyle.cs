using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GroupStyle
	{
		private DataTemplate _headerTemplate;
		private DataTemplateSelector _headerTemplateSelector;
		private Style _headerContainerStyle;
		private bool _hidesIfEmpty;
		private ItemsPanelTemplate _panel;
		private Style _containerStyle;
		private StyleSelector _containerStyleSelector;

		/// <summary>Occurs when a property value changes (an ItemsControl that uses this GroupStyle regroups).</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		public DataTemplate HeaderTemplate
		{
			get => _headerTemplate;
			set => SetProperty(ref _headerTemplate, value);
		}

#if IS_UNIT_TESTS
		[CodeBrix.Platform.NotImplemented]
#endif
		public DataTemplateSelector HeaderTemplateSelector
		{
			get => _headerTemplateSelector;
			set => SetProperty(ref _headerTemplateSelector, value);
		}

		public Style HeaderContainerStyle
		{
			get => _headerContainerStyle;
			set => SetProperty(ref _headerContainerStyle, value);
		}

		public bool HidesIfEmpty
		{
			get => _hidesIfEmpty;
			set => SetProperty(ref _hidesIfEmpty, value);
		}

		/// <summary>
		/// Gets or sets the template of the panel that lays out the items of each group, used when the ItemsControl's own
		/// items panel is not a virtualizing panel (each group is then a <see cref="GroupItem"/>).
		/// </summary>
		public ItemsPanelTemplate Panel
		{
			get => _panel;
			set => SetProperty(ref _panel, value);
		}

		/// <summary>Gets or sets the style applied to the <see cref="GroupItem"/> generated for each group.</summary>
		public Style ContainerStyle
		{
			get => _containerStyle;
			set => SetProperty(ref _containerStyle, value);
		}

		/// <summary>Gets or sets the selector of the style applied to the <see cref="GroupItem"/> generated for each group.</summary>
		public StyleSelector ContainerStyleSelector
		{
			get => _containerStyleSelector;
			set => SetProperty(ref _containerStyleSelector, value);
		}

		private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
