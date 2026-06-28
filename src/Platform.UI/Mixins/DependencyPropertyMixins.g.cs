


using System;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using CodeBrix.Platform.Extensions.Disposables;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.DataBinding;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#if __APPLE_UIKIT__
using Color = UIKit.UIColor;
using View = UIKit.UIView;
#elif __ANDROID__
using Color = Android.Resource.Color;
using View = Android.Views.View;
#elif IS_UNIT_TESTS || CODEBRIX_REFERENCE_API
using Color = System.Object;
using View = Microsoft.UI.Xaml.FrameworkElement;
#endif  


namespace Microsoft.UI.Xaml.Controls
{
	
#if true
    public partial class Control
	{
				
#if true
        #region BackgroundSizing DependencyProperty

         public BackgroundSizing BackgroundSizing
        {
            get { return (BackgroundSizing)this.GetValue(BackgroundSizingProperty); }
            set { this.SetValue(BackgroundSizingProperty, value); }
        }

        public static DependencyProperty BackgroundSizingProperty { get ; } =
            DependencyProperty.Register(
                "BackgroundSizing",
                typeof(BackgroundSizing),
                typeof(Control),
                new FrameworkPropertyMetadata(
                    defaultValue: (BackgroundSizing)default,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Control)s)?.OnBackgroundSizingChanged((BackgroundSizing)e.OldValue, (BackgroundSizing)e.NewValue)
                )
            );

        protected virtual void OnBackgroundSizingChanged(BackgroundSizing oldBackgroundSizing, BackgroundSizing newBackgroundSizing)
        {
            OnBackgroundSizingChangedPartial(oldBackgroundSizing, newBackgroundSizing);
            OnBackgroundSizingChangedPartialNative(oldBackgroundSizing, newBackgroundSizing);
        }

        partial void OnBackgroundSizingChangedPartial(BackgroundSizing oldBackgroundSizing, BackgroundSizing newBackgroundSizing);
        partial void OnBackgroundSizingChangedPartialNative(BackgroundSizing oldBackgroundSizing, BackgroundSizing newBackgroundSizing);

		#endregion
#endif

				
#if true
        #region HorizontalContentAlignment DependencyProperty

         public HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)this.GetValue(HorizontalContentAlignmentProperty); }
            set { this.SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public static DependencyProperty HorizontalContentAlignmentProperty { get ; } =
            DependencyProperty.Register(
                "HorizontalContentAlignment",
                typeof(HorizontalAlignment),
                typeof(Control),
                new FrameworkPropertyMetadata(
                    defaultValue: (HorizontalAlignment)(CodeBrix.Platform.UI.FeatureConfiguration.Control.UseLegacyContentAlignment ? HorizontalAlignment.Left : HorizontalAlignment.Center),
					options: FrameworkPropertyMetadataOptions.AffectsArrange,
                    propertyChangedCallback: (s, e) => ((Control)s)?.OnHorizontalContentAlignmentChanged((HorizontalAlignment)e.OldValue, (HorizontalAlignment)e.NewValue)
                )
            );

        protected virtual void OnHorizontalContentAlignmentChanged(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment)
        {
            OnHorizontalContentAlignmentChangedPartial(oldHorizontalContentAlignment, newHorizontalContentAlignment);
            OnHorizontalContentAlignmentChangedPartialNative(oldHorizontalContentAlignment, newHorizontalContentAlignment);
        }

        partial void OnHorizontalContentAlignmentChangedPartial(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment);
        partial void OnHorizontalContentAlignmentChangedPartialNative(HorizontalAlignment oldHorizontalContentAlignment, HorizontalAlignment newHorizontalContentAlignment);

		#endregion
#endif

				
#if true
        #region VerticalContentAlignment DependencyProperty

         public VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)this.GetValue(VerticalContentAlignmentProperty); }
            set { this.SetValue(VerticalContentAlignmentProperty, value); }
        }

        public static DependencyProperty VerticalContentAlignmentProperty { get ; } =
            DependencyProperty.Register(
                "VerticalContentAlignment",
                typeof(VerticalAlignment),
                typeof(Control),
                new FrameworkPropertyMetadata(
                    defaultValue: (VerticalAlignment)(CodeBrix.Platform.UI.FeatureConfiguration.Control.UseLegacyContentAlignment ? VerticalAlignment.Top : VerticalAlignment.Center),
					options: FrameworkPropertyMetadataOptions.AffectsArrange,
                    propertyChangedCallback: (s, e) => ((Control)s)?.OnVerticalContentAlignmentChanged((VerticalAlignment)e.OldValue, (VerticalAlignment)e.NewValue)
                )
            );

        protected virtual void OnVerticalContentAlignmentChanged(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment)
        {
            OnVerticalContentAlignmentChangedPartial(oldVerticalContentAlignment, newVerticalContentAlignment);
            OnVerticalContentAlignmentChangedPartialNative(oldVerticalContentAlignment, newVerticalContentAlignment);
        }

        partial void OnVerticalContentAlignmentChangedPartial(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment);
        partial void OnVerticalContentAlignmentChangedPartialNative(VerticalAlignment oldVerticalContentAlignment, VerticalAlignment newVerticalContentAlignment);

		#endregion
#endif

			}

#endif

	
#if __IOS__
    public partial class Picker
	{
				
#if true
        #region ItemsSource DependencyProperty

         public object ItemsSource
        {
            get { return (object)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        public static DependencyProperty ItemsSourceProperty { get ; } =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(object),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (object)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnItemsSourceChanged((object)e.OldValue, (object)e.NewValue)
                )
            );

        protected virtual void OnItemsSourceChanged(object oldItemsSource, object newItemsSource)
        {
            OnItemsSourceChangedPartial(oldItemsSource, newItemsSource);
            OnItemsSourceChangedPartialNative(oldItemsSource, newItemsSource);
        }

        partial void OnItemsSourceChangedPartial(object oldItemsSource, object newItemsSource);
        partial void OnItemsSourceChangedPartialNative(object oldItemsSource, object newItemsSource);

		#endregion
#endif

				
#if true
        #region SelectedItem DependencyProperty

         public object SelectedItem
        {
            get { return (object)this.GetValue(SelectedItemProperty); }
            set { this.SetValue(SelectedItemProperty, value); }
        }

        public static DependencyProperty SelectedItemProperty { get ; } =
            DependencyProperty.Register(
                "SelectedItem",
                typeof(object),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (object)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnSelectedItemChanged((object)e.OldValue, (object)e.NewValue)
                )
            );

        protected virtual void OnSelectedItemChanged(object oldSelectedItem, object newSelectedItem)
        {
            OnSelectedItemChangedPartial(oldSelectedItem, newSelectedItem);
            OnSelectedItemChangedPartialNative(oldSelectedItem, newSelectedItem);
        }

        partial void OnSelectedItemChangedPartial(object oldSelectedItem, object newSelectedItem);
        partial void OnSelectedItemChangedPartialNative(object oldSelectedItem, object newSelectedItem);

		#endregion
#endif

				
#if true
        #region SelectedIndex DependencyProperty

         public int SelectedIndex
        {
            get { return (int)this.GetValue(SelectedIndexProperty); }
            set { this.SetValue(SelectedIndexProperty, value); }
        }

        public static DependencyProperty SelectedIndexProperty { get ; } =
            DependencyProperty.Register(
                "SelectedIndex",
                typeof(int),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (int)-1,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnSelectedIndexChanged((int)e.OldValue, (int)e.NewValue)
                )
            );

        protected virtual void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
        {
            OnSelectedIndexChangedPartial(oldSelectedIndex, newSelectedIndex);
            OnSelectedIndexChangedPartialNative(oldSelectedIndex, newSelectedIndex);
        }

        partial void OnSelectedIndexChangedPartial(int oldSelectedIndex, int newSelectedIndex);
        partial void OnSelectedIndexChangedPartialNative(int oldSelectedIndex, int newSelectedIndex);

		#endregion
#endif

				
#if true
        #region ItemTemplate DependencyProperty

         public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)this.GetValue(ItemTemplateProperty); }
            set { this.SetValue(ItemTemplateProperty, value); }
        }

        public static DependencyProperty ItemTemplateProperty { get ; } =
            DependencyProperty.Register(
                "ItemTemplate",
                typeof(DataTemplate),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (DataTemplate)null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnItemTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
                )
            );

        protected virtual void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
        {
            OnItemTemplateChangedPartial(oldItemTemplate, newItemTemplate);
            OnItemTemplateChangedPartialNative(oldItemTemplate, newItemTemplate);
        }

        partial void OnItemTemplateChangedPartial(DataTemplate oldItemTemplate, DataTemplate newItemTemplate);
        partial void OnItemTemplateChangedPartialNative(DataTemplate oldItemTemplate, DataTemplate newItemTemplate);

		#endregion
#endif

				
#if true
        #region ItemContainerStyle DependencyProperty

         public Style ItemContainerStyle
        {
            get { return (Style)this.GetValue(ItemContainerStyleProperty); }
            set { this.SetValue(ItemContainerStyleProperty, value); }
        }

        public static DependencyProperty ItemContainerStyleProperty { get ; } =
            DependencyProperty.Register(
                "ItemContainerStyle",
                typeof(Style),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (Style)null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnItemContainerStyleChanged((Style)e.OldValue, (Style)e.NewValue)
                )
            );

        protected virtual void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
        {
            OnItemContainerStyleChangedPartial(oldItemContainerStyle, newItemContainerStyle);
            OnItemContainerStyleChangedPartialNative(oldItemContainerStyle, newItemContainerStyle);
        }

        partial void OnItemContainerStyleChangedPartial(Style oldItemContainerStyle, Style newItemContainerStyle);
        partial void OnItemContainerStyleChangedPartialNative(Style oldItemContainerStyle, Style newItemContainerStyle);

		#endregion
#endif

				
#if true
        #region ItemTemplateSelector DependencyProperty

         public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)this.GetValue(ItemTemplateSelectorProperty); }
            set { this.SetValue(ItemTemplateSelectorProperty, value); }
        }

        public static DependencyProperty ItemTemplateSelectorProperty { get ; } =
            DependencyProperty.Register(
                "ItemTemplateSelector",
                typeof(DataTemplateSelector),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (DataTemplateSelector)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnItemTemplateSelectorChanged((DataTemplateSelector)e.OldValue, (DataTemplateSelector)e.NewValue)
                )
            );

        protected virtual void OnItemTemplateSelectorChanged(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector)
        {
            OnItemTemplateSelectorChangedPartial(oldItemTemplateSelector, newItemTemplateSelector);
            OnItemTemplateSelectorChangedPartialNative(oldItemTemplateSelector, newItemTemplateSelector);
        }

        partial void OnItemTemplateSelectorChangedPartial(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector);
        partial void OnItemTemplateSelectorChangedPartialNative(DataTemplateSelector oldItemTemplateSelector, DataTemplateSelector newItemTemplateSelector);

		#endregion
#endif

				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

				
#if true
        #region Placeholder DependencyProperty

         public object Placeholder
        {
            get { return (object)this.GetValue(PlaceholderProperty); }
            set { this.SetValue(PlaceholderProperty, value); }
        }

        public static DependencyProperty PlaceholderProperty { get ; } =
            DependencyProperty.Register(
                "Placeholder",
                typeof(object),
                typeof(Picker),
                new FrameworkPropertyMetadata(
                    defaultValue: (object)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Picker)s)?.OnPlaceholderChanged((object)e.OldValue, (object)e.NewValue)
                )
            );

        protected virtual void OnPlaceholderChanged(object oldPlaceholder, object newPlaceholder)
        {
            OnPlaceholderChangedPartial(oldPlaceholder, newPlaceholder);
            OnPlaceholderChangedPartialNative(oldPlaceholder, newPlaceholder);
        }

        partial void OnPlaceholderChangedPartial(object oldPlaceholder, object newPlaceholder);
        partial void OnPlaceholderChangedPartialNative(object oldPlaceholder, object newPlaceholder);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class ComboBox
	{
				
#if true
        #region PlaceholderText DependencyProperty

         public string PlaceholderText
        {
            get { return (string)this.GetValue(PlaceholderTextProperty); }
            set { this.SetValue(PlaceholderTextProperty, value); }
        }

        public static DependencyProperty PlaceholderTextProperty { get ; } =
            DependencyProperty.Register(
                "PlaceholderText",
                typeof(string),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ComboBox)s)?.OnPlaceholderTextChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnPlaceholderTextChanged(string oldPlaceholderText, string newPlaceholderText)
        {
            OnPlaceholderTextChangedPartial(oldPlaceholderText, newPlaceholderText);
            OnPlaceholderTextChangedPartialNative(oldPlaceholderText, newPlaceholderText);
        }

        partial void OnPlaceholderTextChangedPartial(string oldPlaceholderText, string newPlaceholderText);
        partial void OnPlaceholderTextChangedPartialNative(string oldPlaceholderText, string newPlaceholderText);

		#endregion
#endif

				
#if true
        #region MaxDropDownHeight DependencyProperty

         public double MaxDropDownHeight
        {
            get { return (double)this.GetValue(MaxDropDownHeightProperty); }
            set { this.SetValue(MaxDropDownHeightProperty, value); }
        }

        public static DependencyProperty MaxDropDownHeightProperty { get ; } =
            DependencyProperty.Register(
                "MaxDropDownHeight",
                typeof(double),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)double.PositiveInfinity,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ComboBox)s)?.OnMaxDropDownHeightChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnMaxDropDownHeightChanged(double oldMaxDropDownHeight, double newMaxDropDownHeight)
        {
            OnMaxDropDownHeightChangedPartial(oldMaxDropDownHeight, newMaxDropDownHeight);
            OnMaxDropDownHeightChangedPartialNative(oldMaxDropDownHeight, newMaxDropDownHeight);
        }

        partial void OnMaxDropDownHeightChangedPartial(double oldMaxDropDownHeight, double newMaxDropDownHeight);
        partial void OnMaxDropDownHeightChangedPartialNative(double oldMaxDropDownHeight, double newMaxDropDownHeight);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class ItemsControl
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(ItemsControl),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsControl)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class ListViewBase
	{
				
#if true
        #region Header DependencyProperty

         public object Header
        {
            get { return (object)this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }

        public static DependencyProperty HeaderProperty { get ; } =
            DependencyProperty.Register(
                "Header",
                typeof(object),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (object)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnHeaderChanged((object)e.OldValue, (object)e.NewValue)
                )
            );

        protected virtual void OnHeaderChanged(object oldHeader, object newHeader)
        {
            OnHeaderChangedPartial(oldHeader, newHeader);
            OnHeaderChangedPartialNative(oldHeader, newHeader);
        }

        partial void OnHeaderChangedPartial(object oldHeader, object newHeader);
        partial void OnHeaderChangedPartialNative(object oldHeader, object newHeader);

		#endregion
#endif

				
#if true
        #region HeaderTemplate DependencyProperty

         public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)this.GetValue(HeaderTemplateProperty); }
            set { this.SetValue(HeaderTemplateProperty, value); }
        }

        public static DependencyProperty HeaderTemplateProperty { get ; } =
            DependencyProperty.Register(
                "HeaderTemplate",
                typeof(DataTemplate),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (DataTemplate)null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnHeaderTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
                )
            );

        protected virtual void OnHeaderTemplateChanged(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate)
        {
            OnHeaderTemplateChangedPartial(oldHeaderTemplate, newHeaderTemplate);
            OnHeaderTemplateChangedPartialNative(oldHeaderTemplate, newHeaderTemplate);
        }

        partial void OnHeaderTemplateChangedPartial(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate);
        partial void OnHeaderTemplateChangedPartialNative(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate);

		#endregion
#endif

				
#if true
        #region Footer DependencyProperty

         public object Footer
        {
            get { return (object)this.GetValue(FooterProperty); }
            set { this.SetValue(FooterProperty, value); }
        }

        public static DependencyProperty FooterProperty { get ; } =
            DependencyProperty.Register(
                "Footer",
                typeof(object),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (object)null,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnFooterChanged((object)e.OldValue, (object)e.NewValue)
                )
            );

        protected virtual void OnFooterChanged(object oldFooter, object newFooter)
        {
            OnFooterChangedPartial(oldFooter, newFooter);
            OnFooterChangedPartialNative(oldFooter, newFooter);
        }

        partial void OnFooterChangedPartial(object oldFooter, object newFooter);
        partial void OnFooterChangedPartialNative(object oldFooter, object newFooter);

		#endregion
#endif

				
#if true
        #region FooterTemplate DependencyProperty

         public DataTemplate FooterTemplate
        {
            get { return (DataTemplate)this.GetValue(FooterTemplateProperty); }
            set { this.SetValue(FooterTemplateProperty, value); }
        }

        public static DependencyProperty FooterTemplateProperty { get ; } =
            DependencyProperty.Register(
                "FooterTemplate",
                typeof(DataTemplate),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (DataTemplate)null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnFooterTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
                )
            );

        protected virtual void OnFooterTemplateChanged(DataTemplate oldFooterTemplate, DataTemplate newFooterTemplate)
        {
            OnFooterTemplateChangedPartial(oldFooterTemplate, newFooterTemplate);
            OnFooterTemplateChangedPartialNative(oldFooterTemplate, newFooterTemplate);
        }

        partial void OnFooterTemplateChangedPartial(DataTemplate oldFooterTemplate, DataTemplate newFooterTemplate);
        partial void OnFooterTemplateChangedPartialNative(DataTemplate oldFooterTemplate, DataTemplate newFooterTemplate);

		#endregion
#endif

				
#if true
        #region SelectionMode DependencyProperty

         public ListViewSelectionMode SelectionMode
        {
            get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
            set { this.SetValue(SelectionModeProperty, value); }
        }

        public static DependencyProperty SelectionModeProperty { get ; } =
            DependencyProperty.Register(
                "SelectionMode",
                typeof(ListViewSelectionMode),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (ListViewSelectionMode)ListViewSelectionMode.Single,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnSelectionModeChanged((ListViewSelectionMode)e.OldValue, (ListViewSelectionMode)e.NewValue)
                )
            );

        protected virtual void OnSelectionModeChanged(ListViewSelectionMode oldSelectionMode, ListViewSelectionMode newSelectionMode)
        {
            OnSelectionModeChangedPartial(oldSelectionMode, newSelectionMode);
            OnSelectionModeChangedPartialNative(oldSelectionMode, newSelectionMode);
        }

        partial void OnSelectionModeChangedPartial(ListViewSelectionMode oldSelectionMode, ListViewSelectionMode newSelectionMode);
        partial void OnSelectionModeChangedPartialNative(ListViewSelectionMode oldSelectionMode, ListViewSelectionMode newSelectionMode);

		#endregion
#endif

				
#if true
        #region IsItemClickEnabled DependencyProperty

         public bool IsItemClickEnabled
        {
            get { return (bool)this.GetValue(IsItemClickEnabledProperty); }
            set { this.SetValue(IsItemClickEnabledProperty, value); }
        }

        public static DependencyProperty IsItemClickEnabledProperty { get ; } =
            DependencyProperty.Register(
                "IsItemClickEnabled",
                typeof(bool),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)false,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnIsItemClickEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnIsItemClickEnabledChanged(bool oldIsItemClickEnabled, bool newIsItemClickEnabled)
        {
            OnIsItemClickEnabledChangedPartial(oldIsItemClickEnabled, newIsItemClickEnabled);
            OnIsItemClickEnabledChangedPartialNative(oldIsItemClickEnabled, newIsItemClickEnabled);
        }

        partial void OnIsItemClickEnabledChangedPartial(bool oldIsItemClickEnabled, bool newIsItemClickEnabled);
        partial void OnIsItemClickEnabledChangedPartialNative(bool oldIsItemClickEnabled, bool newIsItemClickEnabled);

		#endregion
#endif

				
#if true
        #region DataFetchSize DependencyProperty

         public double DataFetchSize
        {
            get { return (double)this.GetValue(DataFetchSizeProperty); }
            set { this.SetValue(DataFetchSizeProperty, value); }
        }

        public static DependencyProperty DataFetchSizeProperty { get ; } =
            DependencyProperty.Register(
                "DataFetchSize",
                typeof(double),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)3d,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnDataFetchSizeChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnDataFetchSizeChanged(double oldDataFetchSize, double newDataFetchSize)
        {
            OnDataFetchSizeChangedPartial(oldDataFetchSize, newDataFetchSize);
            OnDataFetchSizeChangedPartialNative(oldDataFetchSize, newDataFetchSize);
        }

        partial void OnDataFetchSizeChangedPartial(double oldDataFetchSize, double newDataFetchSize);
        partial void OnDataFetchSizeChangedPartialNative(double oldDataFetchSize, double newDataFetchSize);

		#endregion
#endif

				
#if true
        #region IncrementalLoadingThreshold DependencyProperty

         public double IncrementalLoadingThreshold
        {
            get { return (double)this.GetValue(IncrementalLoadingThresholdProperty); }
            set { this.SetValue(IncrementalLoadingThresholdProperty, value); }
        }

        public static DependencyProperty IncrementalLoadingThresholdProperty { get ; } =
            DependencyProperty.Register(
                "IncrementalLoadingThreshold",
                typeof(double),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)0d,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnIncrementalLoadingThresholdChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnIncrementalLoadingThresholdChanged(double oldIncrementalLoadingThreshold, double newIncrementalLoadingThreshold)
        {
            OnIncrementalLoadingThresholdChangedPartial(oldIncrementalLoadingThreshold, newIncrementalLoadingThreshold);
            OnIncrementalLoadingThresholdChangedPartialNative(oldIncrementalLoadingThreshold, newIncrementalLoadingThreshold);
        }

        partial void OnIncrementalLoadingThresholdChangedPartial(double oldIncrementalLoadingThreshold, double newIncrementalLoadingThreshold);
        partial void OnIncrementalLoadingThresholdChangedPartialNative(double oldIncrementalLoadingThreshold, double newIncrementalLoadingThreshold);

		#endregion
#endif

				
#if true
        #region IncrementalLoadingTrigger DependencyProperty

         public IncrementalLoadingTrigger IncrementalLoadingTrigger
        {
            get { return (IncrementalLoadingTrigger)this.GetValue(IncrementalLoadingTriggerProperty); }
            set { this.SetValue(IncrementalLoadingTriggerProperty, value); }
        }

        public static DependencyProperty IncrementalLoadingTriggerProperty { get ; } =
            DependencyProperty.Register(
                "IncrementalLoadingTrigger",
                typeof(IncrementalLoadingTrigger),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (IncrementalLoadingTrigger)IncrementalLoadingTrigger.Edge,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnIncrementalLoadingTriggerChanged((IncrementalLoadingTrigger)e.OldValue, (IncrementalLoadingTrigger)e.NewValue)
                )
            );

        protected virtual void OnIncrementalLoadingTriggerChanged(IncrementalLoadingTrigger oldIncrementalLoadingTrigger, IncrementalLoadingTrigger newIncrementalLoadingTrigger)
        {
            OnIncrementalLoadingTriggerChangedPartial(oldIncrementalLoadingTrigger, newIncrementalLoadingTrigger);
            OnIncrementalLoadingTriggerChangedPartialNative(oldIncrementalLoadingTrigger, newIncrementalLoadingTrigger);
        }

        partial void OnIncrementalLoadingTriggerChangedPartial(IncrementalLoadingTrigger oldIncrementalLoadingTrigger, IncrementalLoadingTrigger newIncrementalLoadingTrigger);
        partial void OnIncrementalLoadingTriggerChangedPartialNative(IncrementalLoadingTrigger oldIncrementalLoadingTrigger, IncrementalLoadingTrigger newIncrementalLoadingTrigger);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class ItemsStackPanel
	{
				
#if true
        #region AreStickyGroupHeadersEnabled DependencyProperty

         public bool AreStickyGroupHeadersEnabled
        {
            get { return (bool)this.GetValue(AreStickyGroupHeadersEnabledProperty); }
            set { this.SetValue(AreStickyGroupHeadersEnabledProperty, value); }
        }

        public static DependencyProperty AreStickyGroupHeadersEnabledProperty { get ; } =
            DependencyProperty.Register(
                "AreStickyGroupHeadersEnabled",
                typeof(bool),
                typeof(ItemsStackPanel),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsStackPanel)s)?.OnAreStickyGroupHeadersEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnAreStickyGroupHeadersEnabledChanged(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled)
        {
            OnAreStickyGroupHeadersEnabledChangedPartial(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
            OnAreStickyGroupHeadersEnabledChangedPartialNative(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
        }

        partial void OnAreStickyGroupHeadersEnabledChangedPartial(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);
        partial void OnAreStickyGroupHeadersEnabledChangedPartialNative(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);

		#endregion
#endif

				
#if true
        #region GroupHeaderPlacement DependencyProperty

         public GroupHeaderPlacement GroupHeaderPlacement
        {
            get { return (GroupHeaderPlacement)this.GetValue(GroupHeaderPlacementProperty); }
            set { this.SetValue(GroupHeaderPlacementProperty, value); }
        }

        public static DependencyProperty GroupHeaderPlacementProperty { get ; } =
            DependencyProperty.Register(
                "GroupHeaderPlacement",
                typeof(GroupHeaderPlacement),
                typeof(ItemsStackPanel),
                new FrameworkPropertyMetadata(
                    defaultValue: (GroupHeaderPlacement)GroupHeaderPlacement.Top,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsStackPanel)s)?.OnGroupHeaderPlacementChanged((GroupHeaderPlacement)e.OldValue, (GroupHeaderPlacement)e.NewValue)
                )
            );

        protected virtual void OnGroupHeaderPlacementChanged(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement)
        {
            OnGroupHeaderPlacementChangedPartial(oldGroupHeaderPlacement, newGroupHeaderPlacement);
            OnGroupHeaderPlacementChangedPartialNative(oldGroupHeaderPlacement, newGroupHeaderPlacement);
        }

        partial void OnGroupHeaderPlacementChangedPartial(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);
        partial void OnGroupHeaderPlacementChangedPartialNative(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);

		#endregion
#endif

				
#if true
        #region GroupPadding DependencyProperty

         public Thickness GroupPadding
        {
            get { return (Thickness)this.GetValue(GroupPaddingProperty); }
            set { this.SetValue(GroupPaddingProperty, value); }
        }

        public static DependencyProperty GroupPaddingProperty { get ; } =
            DependencyProperty.Register(
                "GroupPadding",
                typeof(Thickness),
                typeof(ItemsStackPanel),
                new FrameworkPropertyMetadata(
                    defaultValue: (Thickness)Thickness.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsStackPanel)s)?.OnGroupPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
                )
            );

        protected virtual void OnGroupPaddingChanged(Thickness oldGroupPadding, Thickness newGroupPadding)
        {
            OnGroupPaddingChangedPartial(oldGroupPadding, newGroupPadding);
            OnGroupPaddingChangedPartialNative(oldGroupPadding, newGroupPadding);
        }

        partial void OnGroupPaddingChangedPartial(Thickness oldGroupPadding, Thickness newGroupPadding);
        partial void OnGroupPaddingChangedPartialNative(Thickness oldGroupPadding, Thickness newGroupPadding);

		#endregion
#endif

				
#if true
        #region Orientation DependencyProperty

         public Orientation Orientation
        {
            get { return (Orientation)this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        public static DependencyProperty OrientationProperty { get ; } =
            DependencyProperty.Register(
                "Orientation",
                typeof(Orientation),
                typeof(ItemsStackPanel),
                new FrameworkPropertyMetadata(
                    defaultValue: (Orientation)Orientation.Vertical,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsStackPanel)s)?.OnOrientationChanged((Orientation)e.OldValue, (Orientation)e.NewValue)
                )
            );

        protected virtual void OnOrientationChanged(Orientation oldOrientation, Orientation newOrientation)
        {
            OnOrientationChangedPartial(oldOrientation, newOrientation);
            OnOrientationChangedPartialNative(oldOrientation, newOrientation);
        }

        partial void OnOrientationChangedPartial(Orientation oldOrientation, Orientation newOrientation);
        partial void OnOrientationChangedPartialNative(Orientation oldOrientation, Orientation newOrientation);

		#endregion
#endif

				
#if __ANDROID__
        #region CacheLength DependencyProperty

         public double CacheLength
        {
            get { return (double)this.GetValue(CacheLengthProperty); }
            set { this.SetValue(CacheLengthProperty, value); }
        }

        public static DependencyProperty CacheLengthProperty { get ; } =
            DependencyProperty.Register(
                "CacheLength",
                typeof(double),
                typeof(ItemsStackPanel),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)4.0,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsStackPanel)s)?.OnCacheLengthChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnCacheLengthChanged(double oldCacheLength, double newCacheLength)
        {
            OnCacheLengthChangedPartial(oldCacheLength, newCacheLength);
            OnCacheLengthChangedPartialNative(oldCacheLength, newCacheLength);
        }

        partial void OnCacheLengthChangedPartial(double oldCacheLength, double newCacheLength);
        partial void OnCacheLengthChangedPartialNative(double oldCacheLength, double newCacheLength);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class ItemsWrapGrid
	{
				
#if true
        #region AreStickyGroupHeadersEnabled DependencyProperty

         public bool AreStickyGroupHeadersEnabled
        {
            get { return (bool)this.GetValue(AreStickyGroupHeadersEnabledProperty); }
            set { this.SetValue(AreStickyGroupHeadersEnabledProperty, value); }
        }

        public static DependencyProperty AreStickyGroupHeadersEnabledProperty { get ; } =
            DependencyProperty.Register(
                "AreStickyGroupHeadersEnabled",
                typeof(bool),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnAreStickyGroupHeadersEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnAreStickyGroupHeadersEnabledChanged(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled)
        {
            OnAreStickyGroupHeadersEnabledChangedPartial(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
            OnAreStickyGroupHeadersEnabledChangedPartialNative(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
        }

        partial void OnAreStickyGroupHeadersEnabledChangedPartial(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);
        partial void OnAreStickyGroupHeadersEnabledChangedPartialNative(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);

		#endregion
#endif

				
#if true
        #region GroupHeaderPlacement DependencyProperty

         public GroupHeaderPlacement GroupHeaderPlacement
        {
            get { return (GroupHeaderPlacement)this.GetValue(GroupHeaderPlacementProperty); }
            set { this.SetValue(GroupHeaderPlacementProperty, value); }
        }

        public static DependencyProperty GroupHeaderPlacementProperty { get ; } =
            DependencyProperty.Register(
                "GroupHeaderPlacement",
                typeof(GroupHeaderPlacement),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (GroupHeaderPlacement)GroupHeaderPlacement.Top,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnGroupHeaderPlacementChanged((GroupHeaderPlacement)e.OldValue, (GroupHeaderPlacement)e.NewValue)
                )
            );

        protected virtual void OnGroupHeaderPlacementChanged(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement)
        {
            OnGroupHeaderPlacementChangedPartial(oldGroupHeaderPlacement, newGroupHeaderPlacement);
            OnGroupHeaderPlacementChangedPartialNative(oldGroupHeaderPlacement, newGroupHeaderPlacement);
        }

        partial void OnGroupHeaderPlacementChangedPartial(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);
        partial void OnGroupHeaderPlacementChangedPartialNative(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);

		#endregion
#endif

				
#if true
        #region GroupPadding DependencyProperty

         public Thickness GroupPadding
        {
            get { return (Thickness)this.GetValue(GroupPaddingProperty); }
            set { this.SetValue(GroupPaddingProperty, value); }
        }

        public static DependencyProperty GroupPaddingProperty { get ; } =
            DependencyProperty.Register(
                "GroupPadding",
                typeof(Thickness),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (Thickness)Thickness.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnGroupPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
                )
            );

        protected virtual void OnGroupPaddingChanged(Thickness oldGroupPadding, Thickness newGroupPadding)
        {
            OnGroupPaddingChangedPartial(oldGroupPadding, newGroupPadding);
            OnGroupPaddingChangedPartialNative(oldGroupPadding, newGroupPadding);
        }

        partial void OnGroupPaddingChangedPartial(Thickness oldGroupPadding, Thickness newGroupPadding);
        partial void OnGroupPaddingChangedPartialNative(Thickness oldGroupPadding, Thickness newGroupPadding);

		#endregion
#endif

				
#if true
        #region ItemHeight DependencyProperty

         public double ItemHeight
        {
            get { return (double)this.GetValue(ItemHeightProperty); }
            set { this.SetValue(ItemHeightProperty, value); }
        }

        public static DependencyProperty ItemHeightProperty { get ; } =
            DependencyProperty.Register(
                "ItemHeight",
                typeof(double),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)Double.NaN,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnItemHeightChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnItemHeightChanged(double oldItemHeight, double newItemHeight)
        {
            OnItemHeightChangedPartial(oldItemHeight, newItemHeight);
            OnItemHeightChangedPartialNative(oldItemHeight, newItemHeight);
        }

        partial void OnItemHeightChangedPartial(double oldItemHeight, double newItemHeight);
        partial void OnItemHeightChangedPartialNative(double oldItemHeight, double newItemHeight);

		#endregion
#endif

				
#if true
        #region ItemWidth DependencyProperty

         public double ItemWidth
        {
            get { return (double)this.GetValue(ItemWidthProperty); }
            set { this.SetValue(ItemWidthProperty, value); }
        }

        public static DependencyProperty ItemWidthProperty { get ; } =
            DependencyProperty.Register(
                "ItemWidth",
                typeof(double),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)Double.NaN,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnItemWidthChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnItemWidthChanged(double oldItemWidth, double newItemWidth)
        {
            OnItemWidthChangedPartial(oldItemWidth, newItemWidth);
            OnItemWidthChangedPartialNative(oldItemWidth, newItemWidth);
        }

        partial void OnItemWidthChangedPartial(double oldItemWidth, double newItemWidth);
        partial void OnItemWidthChangedPartialNative(double oldItemWidth, double newItemWidth);

		#endregion
#endif

				
#if true
        #region Orientation DependencyProperty

         public Orientation Orientation
        {
            get { return (Orientation)this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        public static DependencyProperty OrientationProperty { get ; } =
            DependencyProperty.Register(
                "Orientation",
                typeof(Orientation),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (Orientation)Orientation.Vertical,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnOrientationChanged((Orientation)e.OldValue, (Orientation)e.NewValue)
                )
            );

        protected virtual void OnOrientationChanged(Orientation oldOrientation, Orientation newOrientation)
        {
            OnOrientationChangedPartial(oldOrientation, newOrientation);
            OnOrientationChangedPartialNative(oldOrientation, newOrientation);
        }

        partial void OnOrientationChangedPartial(Orientation oldOrientation, Orientation newOrientation);
        partial void OnOrientationChangedPartialNative(Orientation oldOrientation, Orientation newOrientation);

		#endregion
#endif

				
#if true
        #region MaximumRowsOrColumns DependencyProperty

         public int MaximumRowsOrColumns
        {
            get { return (int)this.GetValue(MaximumRowsOrColumnsProperty); }
            set { this.SetValue(MaximumRowsOrColumnsProperty, value); }
        }

        public static DependencyProperty MaximumRowsOrColumnsProperty { get ; } =
            DependencyProperty.Register(
                "MaximumRowsOrColumns",
                typeof(int),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (int)-1,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnMaximumRowsOrColumnsChanged((int)e.OldValue, (int)e.NewValue)
                )
            );

        protected virtual void OnMaximumRowsOrColumnsChanged(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns)
        {
            OnMaximumRowsOrColumnsChangedPartial(oldMaximumRowsOrColumns, newMaximumRowsOrColumns);
            OnMaximumRowsOrColumnsChangedPartialNative(oldMaximumRowsOrColumns, newMaximumRowsOrColumns);
        }

        partial void OnMaximumRowsOrColumnsChangedPartial(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns);
        partial void OnMaximumRowsOrColumnsChangedPartialNative(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns);

		#endregion
#endif

				
#if __ANDROID__
        #region CacheLength DependencyProperty

         public double CacheLength
        {
            get { return (double)this.GetValue(CacheLengthProperty); }
            set { this.SetValue(CacheLengthProperty, value); }
        }

        public static DependencyProperty CacheLengthProperty { get ; } =
            DependencyProperty.Register(
                "CacheLength",
                typeof(double),
                typeof(ItemsWrapGrid),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)4.0,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGrid)s)?.OnCacheLengthChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnCacheLengthChanged(double oldCacheLength, double newCacheLength)
        {
            OnCacheLengthChangedPartial(oldCacheLength, newCacheLength);
            OnCacheLengthChangedPartialNative(oldCacheLength, newCacheLength);
        }

        partial void OnCacheLengthChangedPartial(double oldCacheLength, double newCacheLength);
        partial void OnCacheLengthChangedPartialNative(double oldCacheLength, double newCacheLength);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class VirtualizingPanelLayout
	{
				
#if true
        #region AreStickyGroupHeadersEnabled DependencyProperty

         public bool AreStickyGroupHeadersEnabled
        {
            get { return (bool)this.GetValue(AreStickyGroupHeadersEnabledProperty); }
            set { this.SetValue(AreStickyGroupHeadersEnabledProperty, value); }
        }

        public static DependencyProperty AreStickyGroupHeadersEnabledProperty { get ; } =
            DependencyProperty.Register(
                "AreStickyGroupHeadersEnabled",
                typeof(bool),
                typeof(VirtualizingPanelLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((VirtualizingPanelLayout)s)?.OnAreStickyGroupHeadersEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnAreStickyGroupHeadersEnabledChanged(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled)
        {
            OnAreStickyGroupHeadersEnabledChangedPartial(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
            OnAreStickyGroupHeadersEnabledChangedPartialNative(oldAreStickyGroupHeadersEnabled, newAreStickyGroupHeadersEnabled);
        }

        partial void OnAreStickyGroupHeadersEnabledChangedPartial(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);
        partial void OnAreStickyGroupHeadersEnabledChangedPartialNative(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled);

		#endregion
#endif

				
#if true
        #region GroupHeaderPlacement DependencyProperty

         public GroupHeaderPlacement GroupHeaderPlacement
        {
            get { return (GroupHeaderPlacement)this.GetValue(GroupHeaderPlacementProperty); }
            set { this.SetValue(GroupHeaderPlacementProperty, value); }
        }

        public static DependencyProperty GroupHeaderPlacementProperty { get ; } =
            DependencyProperty.Register(
                "GroupHeaderPlacement",
                typeof(GroupHeaderPlacement),
                typeof(VirtualizingPanelLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (GroupHeaderPlacement)GroupHeaderPlacement.Top,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((VirtualizingPanelLayout)s)?.OnGroupHeaderPlacementChanged((GroupHeaderPlacement)e.OldValue, (GroupHeaderPlacement)e.NewValue)
                )
            );

        protected virtual void OnGroupHeaderPlacementChanged(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement)
        {
            OnGroupHeaderPlacementChangedPartial(oldGroupHeaderPlacement, newGroupHeaderPlacement);
            OnGroupHeaderPlacementChangedPartialNative(oldGroupHeaderPlacement, newGroupHeaderPlacement);
        }

        partial void OnGroupHeaderPlacementChangedPartial(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);
        partial void OnGroupHeaderPlacementChangedPartialNative(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement);

		#endregion
#endif

				
#if true
        #region GroupPadding DependencyProperty

         public Thickness GroupPadding
        {
            get { return (Thickness)this.GetValue(GroupPaddingProperty); }
            set { this.SetValue(GroupPaddingProperty, value); }
        }

        public static DependencyProperty GroupPaddingProperty { get ; } =
            DependencyProperty.Register(
                "GroupPadding",
                typeof(Thickness),
                typeof(VirtualizingPanelLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (Thickness)Thickness.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((VirtualizingPanelLayout)s)?.OnGroupPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
                )
            );

        protected virtual void OnGroupPaddingChanged(Thickness oldGroupPadding, Thickness newGroupPadding)
        {
            OnGroupPaddingChangedPartial(oldGroupPadding, newGroupPadding);
            OnGroupPaddingChangedPartialNative(oldGroupPadding, newGroupPadding);
        }

        partial void OnGroupPaddingChangedPartial(Thickness oldGroupPadding, Thickness newGroupPadding);
        partial void OnGroupPaddingChangedPartialNative(Thickness oldGroupPadding, Thickness newGroupPadding);

		#endregion
#endif

				
#if true
        #region CacheLength DependencyProperty

         public double CacheLength
        {
            get { return (double)this.GetValue(CacheLengthProperty); }
            set { this.SetValue(CacheLengthProperty, value); }
        }

        public static DependencyProperty CacheLengthProperty { get ; } =
            DependencyProperty.Register(
                "CacheLength",
                typeof(double),
                typeof(VirtualizingPanelLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)4.0,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((VirtualizingPanelLayout)s)?.OnCacheLengthChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnCacheLengthChanged(double oldCacheLength, double newCacheLength)
        {
            OnCacheLengthChangedPartial(oldCacheLength, newCacheLength);
            OnCacheLengthChangedPartialNative(oldCacheLength, newCacheLength);
        }

        partial void OnCacheLengthChangedPartial(double oldCacheLength, double newCacheLength);
        partial void OnCacheLengthChangedPartialNative(double oldCacheLength, double newCacheLength);

		#endregion
#endif

			}

#endif

	
#if true
    internal partial class ItemsWrapGridLayout
	{
				
#if true
        #region ItemHeight DependencyProperty

         public double ItemHeight
        {
            get { return (double)this.GetValue(ItemHeightProperty); }
            set { this.SetValue(ItemHeightProperty, value); }
        }

        public static DependencyProperty ItemHeightProperty { get ; } =
            DependencyProperty.Register(
                "ItemHeight",
                typeof(double),
                typeof(ItemsWrapGridLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)Double.NaN,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGridLayout)s)?.OnItemHeightChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnItemHeightChanged(double oldItemHeight, double newItemHeight)
        {
            OnItemHeightChangedPartial(oldItemHeight, newItemHeight);
            OnItemHeightChangedPartialNative(oldItemHeight, newItemHeight);
        }

        partial void OnItemHeightChangedPartial(double oldItemHeight, double newItemHeight);
        partial void OnItemHeightChangedPartialNative(double oldItemHeight, double newItemHeight);

		#endregion
#endif

				
#if true
        #region ItemWidth DependencyProperty

         public double ItemWidth
        {
            get { return (double)this.GetValue(ItemWidthProperty); }
            set { this.SetValue(ItemWidthProperty, value); }
        }

        public static DependencyProperty ItemWidthProperty { get ; } =
            DependencyProperty.Register(
                "ItemWidth",
                typeof(double),
                typeof(ItemsWrapGridLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (double)Double.NaN,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGridLayout)s)?.OnItemWidthChanged((double)e.OldValue, (double)e.NewValue)
                )
            );

        protected virtual void OnItemWidthChanged(double oldItemWidth, double newItemWidth)
        {
            OnItemWidthChangedPartial(oldItemWidth, newItemWidth);
            OnItemWidthChangedPartialNative(oldItemWidth, newItemWidth);
        }

        partial void OnItemWidthChangedPartial(double oldItemWidth, double newItemWidth);
        partial void OnItemWidthChangedPartialNative(double oldItemWidth, double newItemWidth);

		#endregion
#endif

				
#if true
        #region MaximumRowsOrColumns DependencyProperty

         public int MaximumRowsOrColumns
        {
            get { return (int)this.GetValue(MaximumRowsOrColumnsProperty); }
            set { this.SetValue(MaximumRowsOrColumnsProperty, value); }
        }

        public static DependencyProperty MaximumRowsOrColumnsProperty { get ; } =
            DependencyProperty.Register(
                "MaximumRowsOrColumns",
                typeof(int),
                typeof(ItemsWrapGridLayout),
                new FrameworkPropertyMetadata(
                    defaultValue: (int)-1,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ItemsWrapGridLayout)s)?.OnMaximumRowsOrColumnsChanged((int)e.OldValue, (int)e.NewValue)
                )
            );

        protected virtual void OnMaximumRowsOrColumnsChanged(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns)
        {
            OnMaximumRowsOrColumnsChangedPartial(oldMaximumRowsOrColumns, newMaximumRowsOrColumns);
            OnMaximumRowsOrColumnsChangedPartialNative(oldMaximumRowsOrColumns, newMaximumRowsOrColumns);
        }

        partial void OnMaximumRowsOrColumnsChangedPartial(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns);
        partial void OnMaximumRowsOrColumnsChangedPartialNative(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class DatePickerSelector
	{
				
#if true
        #region Date DependencyProperty

         public DateTimeOffset Date
        {
            get { return (DateTimeOffset)this.GetValue(DateProperty); }
            set { this.SetValue(DateProperty, value); }
        }

        public static DependencyProperty DateProperty { get ; } =
            DependencyProperty.Register(
                "Date",
                typeof(DateTimeOffset),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (DateTimeOffset)DateTimeOffset.MinValue,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnDateChanged((DateTimeOffset)e.OldValue, (DateTimeOffset)e.NewValue)
                )
            );

        protected virtual void OnDateChanged(DateTimeOffset oldDate, DateTimeOffset newDate)
        {
            OnDateChangedPartial(oldDate, newDate);
            OnDateChangedPartialNative(oldDate, newDate);
        }

        partial void OnDateChangedPartial(DateTimeOffset oldDate, DateTimeOffset newDate);
        partial void OnDateChangedPartialNative(DateTimeOffset oldDate, DateTimeOffset newDate);

		#endregion
#endif

				
#if true
        #region DayVisible DependencyProperty

         public bool DayVisible
        {
            get { return (bool)this.GetValue(DayVisibleProperty); }
            set { this.SetValue(DayVisibleProperty, value); }
        }

        public static DependencyProperty DayVisibleProperty { get ; } =
            DependencyProperty.Register(
                "DayVisible",
                typeof(bool),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnDayVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnDayVisibleChanged(bool oldDayVisible, bool newDayVisible)
        {
            OnDayVisibleChangedPartial(oldDayVisible, newDayVisible);
            OnDayVisibleChangedPartialNative(oldDayVisible, newDayVisible);
        }

        partial void OnDayVisibleChangedPartial(bool oldDayVisible, bool newDayVisible);
        partial void OnDayVisibleChangedPartialNative(bool oldDayVisible, bool newDayVisible);

		#endregion
#endif

				
#if true
        #region MonthVisible DependencyProperty

         public bool MonthVisible
        {
            get { return (bool)this.GetValue(MonthVisibleProperty); }
            set { this.SetValue(MonthVisibleProperty, value); }
        }

        public static DependencyProperty MonthVisibleProperty { get ; } =
            DependencyProperty.Register(
                "MonthVisible",
                typeof(bool),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnMonthVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnMonthVisibleChanged(bool oldMonthVisible, bool newMonthVisible)
        {
            OnMonthVisibleChangedPartial(oldMonthVisible, newMonthVisible);
            OnMonthVisibleChangedPartialNative(oldMonthVisible, newMonthVisible);
        }

        partial void OnMonthVisibleChangedPartial(bool oldMonthVisible, bool newMonthVisible);
        partial void OnMonthVisibleChangedPartialNative(bool oldMonthVisible, bool newMonthVisible);

		#endregion
#endif

				
#if true
        #region YearVisible DependencyProperty

         public bool YearVisible
        {
            get { return (bool)this.GetValue(YearVisibleProperty); }
            set { this.SetValue(YearVisibleProperty, value); }
        }

        public static DependencyProperty YearVisibleProperty { get ; } =
            DependencyProperty.Register(
                "YearVisible",
                typeof(bool),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnYearVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnYearVisibleChanged(bool oldYearVisible, bool newYearVisible)
        {
            OnYearVisibleChangedPartial(oldYearVisible, newYearVisible);
            OnYearVisibleChangedPartialNative(oldYearVisible, newYearVisible);
        }

        partial void OnYearVisibleChangedPartial(bool oldYearVisible, bool newYearVisible);
        partial void OnYearVisibleChangedPartialNative(bool oldYearVisible, bool newYearVisible);

		#endregion
#endif

				
#if true
        #region MaxYear DependencyProperty

         public DateTimeOffset MaxYear
        {
            get { return (DateTimeOffset)this.GetValue(MaxYearProperty); }
            set { this.SetValue(MaxYearProperty, value); }
        }

        public static DependencyProperty MaxYearProperty { get ; } =
            DependencyProperty.Register(
                "MaxYear",
                typeof(DateTimeOffset),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (DateTimeOffset)DateTimeOffset.MaxValue,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnMaxYearChanged((DateTimeOffset)e.OldValue, (DateTimeOffset)e.NewValue)
                )
            );

        protected virtual void OnMaxYearChanged(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear)
        {
            OnMaxYearChangedPartial(oldMaxYear, newMaxYear);
            OnMaxYearChangedPartialNative(oldMaxYear, newMaxYear);
        }

        partial void OnMaxYearChangedPartial(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear);
        partial void OnMaxYearChangedPartialNative(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear);

		#endregion
#endif

				
#if true
        #region MinYear DependencyProperty

         public DateTimeOffset MinYear
        {
            get { return (DateTimeOffset)this.GetValue(MinYearProperty); }
            set { this.SetValue(MinYearProperty, value); }
        }

        public static DependencyProperty MinYearProperty { get ; } =
            DependencyProperty.Register(
                "MinYear",
                typeof(DateTimeOffset),
                typeof(DatePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (DateTimeOffset)DateTimeOffset.MinValue,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerSelector)s)?.OnMinYearChanged((DateTimeOffset)e.OldValue, (DateTimeOffset)e.NewValue)
                )
            );

        protected virtual void OnMinYearChanged(DateTimeOffset oldMinYear, DateTimeOffset newMinYear)
        {
            OnMinYearChangedPartial(oldMinYear, newMinYear);
            OnMinYearChangedPartialNative(oldMinYear, newMinYear);
        }

        partial void OnMinYearChangedPartial(DateTimeOffset oldMinYear, DateTimeOffset newMinYear);
        partial void OnMinYearChangedPartialNative(DateTimeOffset oldMinYear, DateTimeOffset newMinYear);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class DatePickerFlyout
	{
				
#if true
        #region DayVisible DependencyProperty

         public bool DayVisible
        {
            get { return (bool)this.GetValue(DayVisibleProperty); }
            set { this.SetValue(DayVisibleProperty, value); }
        }

        public static DependencyProperty DayVisibleProperty { get ; } =
            DependencyProperty.Register(
                "DayVisible",
                typeof(bool),
                typeof(DatePickerFlyout),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerFlyout)s)?.OnDayVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnDayVisibleChanged(bool oldDayVisible, bool newDayVisible)
        {
            OnDayVisibleChangedPartial(oldDayVisible, newDayVisible);
            OnDayVisibleChangedPartialNative(oldDayVisible, newDayVisible);
        }

        partial void OnDayVisibleChangedPartial(bool oldDayVisible, bool newDayVisible);
        partial void OnDayVisibleChangedPartialNative(bool oldDayVisible, bool newDayVisible);

		#endregion
#endif

				
#if true
        #region MonthVisible DependencyProperty

         public bool MonthVisible
        {
            get { return (bool)this.GetValue(MonthVisibleProperty); }
            set { this.SetValue(MonthVisibleProperty, value); }
        }

        public static DependencyProperty MonthVisibleProperty { get ; } =
            DependencyProperty.Register(
                "MonthVisible",
                typeof(bool),
                typeof(DatePickerFlyout),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerFlyout)s)?.OnMonthVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnMonthVisibleChanged(bool oldMonthVisible, bool newMonthVisible)
        {
            OnMonthVisibleChangedPartial(oldMonthVisible, newMonthVisible);
            OnMonthVisibleChangedPartialNative(oldMonthVisible, newMonthVisible);
        }

        partial void OnMonthVisibleChangedPartial(bool oldMonthVisible, bool newMonthVisible);
        partial void OnMonthVisibleChangedPartialNative(bool oldMonthVisible, bool newMonthVisible);

		#endregion
#endif

				
#if true
        #region YearVisible DependencyProperty

         public bool YearVisible
        {
            get { return (bool)this.GetValue(YearVisibleProperty); }
            set { this.SetValue(YearVisibleProperty, value); }
        }

        public static DependencyProperty YearVisibleProperty { get ; } =
            DependencyProperty.Register(
                "YearVisible",
                typeof(bool),
                typeof(DatePickerFlyout),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)true,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((DatePickerFlyout)s)?.OnYearVisibleChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnYearVisibleChanged(bool oldYearVisible, bool newYearVisible)
        {
            OnYearVisibleChangedPartial(oldYearVisible, newYearVisible);
            OnYearVisibleChangedPartialNative(oldYearVisible, newYearVisible);
        }

        partial void OnYearVisibleChangedPartial(bool oldYearVisible, bool newYearVisible);
        partial void OnYearVisibleChangedPartialNative(bool oldYearVisible, bool newYearVisible);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class TimePickerSelector
	{
				
#if true
        #region Time DependencyProperty

         public TimeSpan Time
        {
            get { return (TimeSpan)this.GetValue(TimeProperty); }
            set { this.SetValue(TimeProperty, value); }
        }

        public static DependencyProperty TimeProperty { get ; } =
            DependencyProperty.Register(
                "Time",
                typeof(TimeSpan),
                typeof(TimePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (TimeSpan)DateTime.Now.TimeOfDay,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((TimePickerSelector)s)?.OnTimeChanged((TimeSpan)e.OldValue, (TimeSpan)e.NewValue)
                )
            );

        protected virtual void OnTimeChanged(TimeSpan oldTime, TimeSpan newTime)
        {
            OnTimeChangedPartial(oldTime, newTime);
            OnTimeChangedPartialNative(oldTime, newTime);
        }

        partial void OnTimeChangedPartial(TimeSpan oldTime, TimeSpan newTime);
        partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime);

		#endregion
#endif

				
#if true
        #region MinuteIncrement DependencyProperty

         public int MinuteIncrement
        {
            get { return (int)this.GetValue(MinuteIncrementProperty); }
            set { this.SetValue(MinuteIncrementProperty, value); }
        }

        public static DependencyProperty MinuteIncrementProperty { get ; } =
            DependencyProperty.Register(
                "MinuteIncrement",
                typeof(int),
                typeof(TimePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (int)1,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((TimePickerSelector)s)?.OnMinuteIncrementChanged((int)e.OldValue, (int)e.NewValue)
                )
            );

        protected virtual void OnMinuteIncrementChanged(int oldMinuteIncrement, int newMinuteIncrement)
        {
            OnMinuteIncrementChangedPartial(oldMinuteIncrement, newMinuteIncrement);
            OnMinuteIncrementChangedPartialNative(oldMinuteIncrement, newMinuteIncrement);
        }

        partial void OnMinuteIncrementChangedPartial(int oldMinuteIncrement, int newMinuteIncrement);
        partial void OnMinuteIncrementChangedPartialNative(int oldMinuteIncrement, int newMinuteIncrement);

		#endregion
#endif

				
#if true
        #region ClockIdentifier DependencyProperty

         public string ClockIdentifier
        {
            get { return (string)this.GetValue(ClockIdentifierProperty); }
            set { this.SetValue(ClockIdentifierProperty, value); }
        }

        public static DependencyProperty ClockIdentifierProperty { get ; } =
            DependencyProperty.Register(
                "ClockIdentifier",
                typeof(string),
                typeof(TimePickerSelector),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)global::Windows.Globalization.ClockIdentifiers.TwelveHour,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((TimePickerSelector)s)?.OnClockIdentifierChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnClockIdentifierChanged(string oldClockIdentifier, string newClockIdentifier)
        {
            OnClockIdentifierChangedPartial(oldClockIdentifier, newClockIdentifier);
            OnClockIdentifierChangedPartialNative(oldClockIdentifier, newClockIdentifier);
        }

        partial void OnClockIdentifierChangedPartial(string oldClockIdentifier, string newClockIdentifier);
        partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier);

		#endregion
#endif

			}

#endif

	}

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	
#if true
    public partial class SelectorItem
	{
				
#if true
        #region IsSelected DependencyProperty

         public bool IsSelected
        {
            get { return (bool)this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        public static DependencyProperty IsSelectedProperty { get ; } =
            DependencyProperty.Register(
                "IsSelected",
                typeof(bool),
                typeof(SelectorItem),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)false,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((SelectorItem)s)?.OnIsSelectedChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnIsSelectedChanged(bool oldIsSelected, bool newIsSelected)
        {
            OnIsSelectedChangedPartial(oldIsSelected, newIsSelected);
            OnIsSelectedChangedPartialNative(oldIsSelected, newIsSelected);
        }

        partial void OnIsSelectedChangedPartial(bool oldIsSelected, bool newIsSelected);
        partial void OnIsSelectedChangedPartialNative(bool oldIsSelected, bool newIsSelected);

		#endregion
#endif

			}

#endif

	
#if true
    public partial class Popup
	{
				
#if true
        #region IsOpen DependencyProperty

         public bool IsOpen
        {
            get { return (bool)this.GetValue(IsOpenProperty); }
            set { this.SetValue(IsOpenProperty, value); }
        }

        public static DependencyProperty IsOpenProperty { get ; } =
            DependencyProperty.Register(
                "IsOpen",
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)false,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Popup)s)?.OnIsOpenChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnIsOpenChanged(bool oldIsOpen, bool newIsOpen)
        {
            OnIsOpenChangedPartial(oldIsOpen, newIsOpen);
            OnIsOpenChangedPartialNative(oldIsOpen, newIsOpen);
        }

        partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen);
        partial void OnIsOpenChangedPartialNative(bool oldIsOpen, bool newIsOpen);

		#endregion
#endif

				
#if true
        #region Child DependencyProperty

         public UIElement Child
        {
            get { return (UIElement)this.GetValue(ChildProperty); }
            set { this.SetValue(ChildProperty, value); }
        }

        public static DependencyProperty ChildProperty { get ; } =
            DependencyProperty.Register(
                "Child",
                typeof(UIElement),
                typeof(Popup),
                new FrameworkPropertyMetadata(
                    defaultValue: (UIElement)null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext,
                    propertyChangedCallback: (s, e) => ((Popup)s)?.OnChildChanged((UIElement)e.OldValue, (UIElement)e.NewValue)
                )
            );

        protected virtual void OnChildChanged(UIElement oldChild, UIElement newChild)
        {
            OnChildChangedPartial(oldChild, newChild);
            OnChildChangedPartialNative(oldChild, newChild);
        }

        partial void OnChildChangedPartial(UIElement oldChild, UIElement newChild);
        partial void OnChildChangedPartialNative(UIElement oldChild, UIElement newChild);

		#endregion
#endif

				
#if true
        #region IsLightDismissEnabled DependencyProperty

         public bool IsLightDismissEnabled
        {
            get { return (bool)this.GetValue(IsLightDismissEnabledProperty); }
            set { this.SetValue(IsLightDismissEnabledProperty, value); }
        }

        public static DependencyProperty IsLightDismissEnabledProperty { get ; } =
            DependencyProperty.Register(
                "IsLightDismissEnabled",
                typeof(bool),
                typeof(Popup),
                new FrameworkPropertyMetadata(
                    defaultValue: (bool)false,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((Popup)s)?.OnIsLightDismissEnabledChanged((bool)e.OldValue, (bool)e.NewValue)
                )
            );

        protected virtual void OnIsLightDismissEnabledChanged(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
        {
            OnIsLightDismissEnabledChangedPartial(oldIsLightDismissEnabled, newIsLightDismissEnabled);
            OnIsLightDismissEnabledChangedPartialNative(oldIsLightDismissEnabled, newIsLightDismissEnabled);
        }

        partial void OnIsLightDismissEnabledChangedPartial(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled);
        partial void OnIsLightDismissEnabledChangedPartialNative(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled);

		#endregion
#endif

			}

#endif

	}

namespace CodeBrix.Platform.UI.Controls.Legacy
{
	
#if __APPLE_UIKIT__
    public partial class ListViewBase
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(ListViewBase),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListViewBase)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	
#if __ANDROID__
    public partial class ListView
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(ListView),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((ListView)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	
#if __ANDROID__
    public partial class HorizontalListView
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(HorizontalListView),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((HorizontalListView)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	
#if __ANDROID__
    public partial class GridView
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(GridView),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((GridView)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	
#if __ANDROID__
    public partial class HorizontalGridView
	{
				
#if true
        #region DisplayMemberPath DependencyProperty

         public string DisplayMemberPath
        {
            get { return (string)this.GetValue(DisplayMemberPathProperty); }
            set { this.SetValue(DisplayMemberPathProperty, value); }
        }

        public static DependencyProperty DisplayMemberPathProperty { get ; } =
            DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(HorizontalGridView),
                new FrameworkPropertyMetadata(
                    defaultValue: (string)string.Empty,
					options: FrameworkPropertyMetadataOptions.None,
                    propertyChangedCallback: (s, e) => ((HorizontalGridView)s)?.OnDisplayMemberPathChanged((string)e.OldValue, (string)e.NewValue)
                )
            );

        protected virtual void OnDisplayMemberPathChanged(string oldDisplayMemberPath, string newDisplayMemberPath)
        {
            OnDisplayMemberPathChangedPartial(oldDisplayMemberPath, newDisplayMemberPath);
            OnDisplayMemberPathChangedPartialNative(oldDisplayMemberPath, newDisplayMemberPath);
        }

        partial void OnDisplayMemberPathChangedPartial(string oldDisplayMemberPath, string newDisplayMemberPath);
        partial void OnDisplayMemberPathChangedPartialNative(string oldDisplayMemberPath, string newDisplayMemberPath);

		#endregion
#endif

			}

#endif

	}

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	}

