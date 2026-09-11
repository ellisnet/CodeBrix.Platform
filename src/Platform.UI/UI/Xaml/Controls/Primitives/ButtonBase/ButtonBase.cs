using CodeBrix.Platform.Client;
using CodeBrix.Platform.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using System.Windows.Input;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Input;
using CodeBrix.Platform.Extensions.Specialized;
using CodeBrix.Platform.Foundation.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
#if false
using View = UIKit.UIView;
#elif false
using View = Android.Views.View;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		public
#if false
			new
#endif
			event RoutedEventHandler Click;

		public ButtonBase()
		{
			Initialize();

			InitializeProperties();

			Unloaded += (s, e) =>
				IsPressed = false;

			DefaultStyleKey = typeof(ButtonBase);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			OnLoadedPartial();

			RegisterEvents();
		}

		partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			OnUnloadedPartial();
		}

		partial void OnUnloadedPartial();

		public new bool IsPointerOver
		{
			get => base.IsPointerOver;
			set => base.IsPointerOver = value;
		}

		/// <summary>
		/// Keeps <see cref="IsPointerOverProperty"/> in step with the over state that the pointer plumbing
		/// maintains on the element itself.
		/// </summary>
		/// <param name="isPointerOver">Whether a pointer is now over this button.</param>
		/// <remarks>
		/// MEASURED (2026-09-10): the over state lives in a plain field on the element and the property above
		/// forwards straight to it, so nothing ever wrote <see cref="IsPointerOverProperty"/>. That dependency
		/// property read false for the whole life of every button, and a change callback registered on it was
		/// never invoked - which is how SplitButton, MenuBarItem and BreadcrumbBarItem ask to be told, and how
		/// an application's own control asks. A control that recomputes its visual state from that callback
		/// therefore never heard that the pointer had left: a traced touch tap raised PointerExited and set the
		/// over state false, no further state change followed, and the control stayed in its PointerOver state
		/// with the hover overlay its template fades in left up for the rest of its life. Writing the value
		/// here is what makes the public dependency property - and every binding and callback on it - true.
		/// </remarks>
		private protected override void OnIsPointerOverChanged(bool isPointerOver)
		{
			base.OnIsPointerOverChanged(isPointerOver);

			SetValue(IsPointerOverProperty, isPointerOver);
		}

		private void InitializeProperties()
		{
			PartialInitializeProperties();
		}

		partial void PartialInitializeProperties();

		#region Command (DP)
		public static DependencyProperty CommandProperty { get; } = DependencyProperty.Register(
			nameof(Command), typeof(ICommand), typeof(ButtonBase), new FrameworkPropertyMetadata(default(ICommand)));

		public ICommand Command
		{
			get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		#endregion

		#region CommandParameter
		public static DependencyProperty CommandParameterProperty { get; } =
			DependencyProperty.Register(
				nameof(CommandParameter),
				typeof(object),
				typeof(ButtonBase),
				new FrameworkPropertyMetadata(default(object), OnCommandParameterChanged));

		public object CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		private static void OnCommandParameterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) =>
			((ButtonBase)dependencyObject)?.CoerceValue(IsEnabledProperty);
		#endregion

		public ClickMode ClickMode
		{
			get => (ClickMode)this.GetValue(ClickModeProperty);
			set => this.SetValue(ClickModeProperty, value);
		}

		public new bool IsPressed
		{
			get => (bool)GetValue(IsPressedProperty);
			internal set => SetValue(IsPressedProperty, value);
		}

		public static DependencyProperty ClickModeProperty { get; } =
		DependencyProperty.Register(
			name: nameof(ClickMode),
			propertyType: typeof(ClickMode),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(ClickMode.Release));

		public static DependencyProperty IsPointerOverProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPointerOver),
			propertyType: typeof(bool),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		public static DependencyProperty IsPressedProperty { get; } =
		DependencyProperty.Register(
			name: nameof(IsPressed),
			propertyType: typeof(bool),
			ownerType: typeof(ButtonBase),
			typeMetadata: new FrameworkPropertyMetadata(default(bool)));

		partial void RegisterEvents();

#if false
		private void OnCanExecuteChanged()
		{
			this.CoerceValue(IsEnabledProperty);
		}
#endif

		private protected override object CoerceIsEnabled(object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			if (Command != null
				&& !Command.CanExecute(CommandParameter))
			{
				return false;
			}

			return base.CoerceIsEnabled(baseValue, precedence);
		}

		private protected override void OnContentTemplateRootSet() => RegisterEvents();


		// Might be changed if the method does not conflict in CodeBrixViewGroup.
		internal override bool IsViewHit()
		{
			// Overrides the need for a non-null Background (required by base.IsViewHit)
			return true;
		}

		// Allows native buttons (e.g., UIBarButtonItem, IMenuItem) to raise clicks on their associated AppBarButton.
		internal void RaiseClick(PointerRoutedEventArgs args = null)
		{
			OnClick();
		}

		internal void AutomationPeerClick()
		{
			OnClick();
		}

#if false
		private void OnClick(PointerRoutedEventArgs args = null)
		{
			Click?.Invoke(this, new RoutedEventArgs(args?.OriginalSource ?? this));

			InvokeCommand();
		}
#endif

		internal void InvokeCommand()
		{
			try
			{
				if (this.Log().IsEnabled(CodeBrix.Platform.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("Executing command");
				}

				Command.ExecuteIfPossible(CommandParameter);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to execute command", e);
			}
		}
	}
}
