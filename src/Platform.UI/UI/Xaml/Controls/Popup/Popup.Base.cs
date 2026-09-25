using System;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.DataBinding;
using CodeBrix.Platform.UI.Xaml.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = CodeBrix.Platform.UI.Xaml.Core.CoreServices;

#if false
using CoreGraphics;
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup : FrameworkElement, IPopup
{
	private ManagedWeakReference _lastFocusedElement;
	private FocusState _lastFocusState = FocusState.Unfocused;
	private IDisposable _openPopupRegistration;

	public event EventHandler<object> Closed;
	public event EventHandler<object> Opened;

	/// <summary>
	/// Defines a custom layouter which overrides the default placement logic of the <see cref="PopupPanel"/>
	/// </summary>
	internal IDynamicPopupLayouter CustomLayouter { get; set; }

	/// <summary>
	/// Controls whether the Popup should propagate its own DataContext to its Child.
	///
	/// This is particularly useful when the child is a direct dependency of an entered UIElement
	/// while the popup is not (e.g. ToolTip created through ToolTipService)
	/// </summary>
	internal bool PropagatesDataContextToChild { get; set; } = true;

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == AllowFocusOnInteractionProperty ||
			args.Property == AllowFocusWhenDisabledProperty)
		{
			PropagateFocusProperties();
		}

		base.OnPropertyChanged2(args);
	}

	private protected override void OnUnloaded()
	{
		IsOpen = false;
		OnUnloadedPartial();
		base.OnUnloaded();
	}

	/// <summary>
	/// The mirror of <see cref="OnUnloaded"/>: a Popup whose IsOpen was set to true before it entered the live tree
	/// (declared <c>IsOpen="True"</c> in XAML, or opened in code and then added to a panel) could not reach a
	/// XamlRoot at that moment; it is shown now that it has one. A Popup that is closed, or already shown, is left alone.
	/// </summary>
	private protected override void OnLoaded()
	{
		base.OnLoaded();
		EnsureOpenedInRoot();
	}

	/// <summary>
	/// Called by <see cref="Microsoft.UI.Xaml.XamlRoot"/> when a XamlRoot is assigned to this Popup through its XamlRoot property. A
	/// parentless Popup whose IsOpen was set to true before its XamlRoot is shown at that point.
	/// </summary>
	internal void OnXamlRootAssigned() => EnsureOpenedInRoot();

	partial void OnUnloadedPartial();

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		// As the Child is NOT part of the visual tree, it does not have to be measured
		return new Size(Width, Height).FiniteOrDefault(default);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		// As the Child is NOT part of the visual tree, it does not have to be arranged,
		// but we need to manually propagate Translation
		PopupPanel.Translation = Translation;
		return finalSize;
	}

	/// <summary>
	/// The XamlRoot the Popup opens in: its own, its child's, or (CoreWindow hosting only) the window's.
	/// </summary>
	private XamlRoot ResolveOpenXamlRoot() =>
		XamlRoot ?? Child?.XamlRoot ?? WinUICoreServices.Instance.ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.XamlRoot;

	/// <summary>
	/// The first half of opening (see EnsureOpenedInRoot): adopts the XamlRoot, registers the Popup as open in that
	/// root's PopupRoot, and takes focus for a light-dismiss or flyout Popup.
	/// </summary>
	/// <param name="xamlRoot">The XamlRoot the Popup opens in (not null).</param>
	private void RegisterOpenInRoot(XamlRoot xamlRoot)
	{
		if (xamlRoot != XamlRoot)
		{
			XamlRoot = xamlRoot;
		}

		_openPopupRegistration = xamlRoot.VisualTree.PopupRoot.RegisterOpenPopup(this);

		if (IsLightDismissEnabled || AssociatedFlyout is { })
		{
			if (IsLightDismissEnabled)
			{
				m_fIsLightDismiss = true;
			}

			// Store last focused element
			var focusManager = VisualTree.GetFocusManagerForElement(this);
			var focusedElement = focusManager?.FocusedElement as UIElement;
			var focusState = focusManager?.GetRealFocusStateForFocusedElement() ?? FocusState.Unfocused;
			if (focusedElement != null && focusState != FocusState.Unfocused)
			{
				_lastFocusedElement = WeakReferencePool.RentWeakReference(this, focusedElement);
				_lastFocusState = focusState;
			}

			// Usually, FrameworkElements handle focus management inside OnLoaded/OnUnloaded,
			// but since popups are (un)loaded, we have to do it here.
			if (Child is FrameworkElement fw && fw.AllowFocusOnInteraction)
			{
				// Give the child focus if allowed
				Focus(FocusState.Programmatic);
			}
		}
	}

	partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen)
	{
		// Opening is EnsureOpenedInRoot (Popup.WithPopupRoot.cs), called from OnIsOpenChangedPartialNative, from
		// OnLoaded and when a XamlRoot is assigned: a Popup opened before it can reach a XamlRoot is shown later.
		if (!newIsOpen)
		{
			_openPopupRegistration?.Dispose();
			if (IsLightDismissEnabled)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				var focusedElement = focusManager?.FocusedElement as UIElement;

				if (_lastFocusedElement != null && _lastFocusedElement.Target is UIElement target && focusedElement != target)
				{
					target.Focus(_lastFocusState);
					_lastFocusedElement = null;
				}
			}

			m_fIsLightDismiss = false;
		}
	}

	partial void OnChildChangedPartial(UIElement oldChild, UIElement newChild)
	{
		if (oldChild is FrameworkElement oldChildFe && oldChildFe.LogicalParentOverride == this)
		{
			oldChildFe.SetLogicalParent(null);
		}
		if (newChild is FrameworkElement newChildFe)
		{
			newChildFe.SetLogicalParent(this);
		}

		if (oldChild is IDependencyObjectStoreProvider provider &&
			provider.Store.ReadLocalValue(provider.Store.DataContextProperty) != DependencyProperty.UnsetValue)
		{
			provider.Store.ClearValue(AllowFocusOnInteractionProperty, DependencyPropertyValuePrecedences.Local);
			provider.Store.ClearValue(AllowFocusWhenDisabledProperty, DependencyPropertyValuePrecedences.Local);
		}

		UpdateDataContext(null);
		PropagateFocusProperties();
	}

	protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnDataContextChanged(e);

		UpdateDataContext(e);
	}

	private void UpdateDataContext(DependencyPropertyChangedEventArgs e)
	{
		if (PropagatesDataContextToChild)
		{
			((IDependencyObjectStoreProvider)PopupPanel).Store.SetValue(((IDependencyObjectStoreProvider)PopupPanel).Store.DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local);
		}
	}

	private void PropagateFocusProperties()
	{
		if (Child is IDependencyObjectStoreProvider provider)
		{
			provider.Store.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction, DependencyPropertyValuePrecedences.Local);
			provider.Store.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled, DependencyPropertyValuePrecedences.Local);
		}
	}

	/// <summary>
	/// A layouter responsible to layout the content of a popup at the right place
	/// </summary>
	internal interface IDynamicPopupLayouter
	{
		/// <summary>
		/// Measure the content of the popup
		/// </summary>
		/// <param name="available">The available size to place to render the popup. This is expected to be the screen size.</param>
		/// <param name="visibleSize">The size of the visible bounds of the window. This is expected to be AtMost the available.</param>
		/// <returns>The desired size to render the content</returns>
		Size Measure(Size available, Size visibleSize);

		/// <summary>
		/// Render the content of the popup at its final location
		/// </summary>
		/// <param name="finalSize">The final size available to render the view. This is expected to be the screen size.</param>
		/// <param name="visibleBounds">The frame of the visible bounds of the window. This is expected to be AtMost the finalSize.</param>
		/// <param name="desiredSize">The size at which the content expect to be rendered. This is the result of the last <see cref="Measure"/>.</param>
		/// <param name="upperLeftOffset">Coordinate system adjustment, applied to the resulting frame computed from the popup content</param>
		void Arrange(Size finalSize, Rect visibleBounds, Size desiredSize);
	}

	partial void OnIsLightDismissEnabledChangedPartial(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
	{
	}

	event EventHandler<object> IPopup.Closed
	{
		add => Closed += value;
		remove => Closed -= value;
	}

	event EventHandler<object> IPopup.Opened
	{
		add => Opened += value;
		remove => Opened -= value;
	}

	bool IPopup.IsOpen
	{
		get => IsOpen;
		set => IsOpen = value;
	}

	UIElement IPopup.Child
	{
		get => Child;
		set => Child = value;
	}
}
