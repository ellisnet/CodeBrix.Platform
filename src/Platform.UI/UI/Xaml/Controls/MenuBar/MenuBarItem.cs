using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Automation.Peers;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using AutomationPeer = Microsoft.UI.Xaml.Automation.Peers.AutomationPeer;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuBarItem : Control
	{
		private readonly SerialDisposable _registrations = new SerialDisposable();

		private MenuBar m_menuBar;
		private MenuBarItemFlyout m_flyout;
		private Button m_button;
		private bool m_isFlyoutOpen;
		private Control m_subscribedPresenter;
		private KeyEventHandler m_presenterKeyDownHandler;
		private bool m_isClosingByEscape;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		private DependencyObject m_passThroughElement;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

		private CompositeDisposable _activeDisposables;

		public MenuBarItem()
		{
			this.SetDefaultStyleKey();

			var observableVector = new ObservableVector<MenuFlyoutItemBase>();

			observableVector.VectorChanged += OnItemsVectorChanged;

			SetValue(ItemsProperty, observableVector);

			Loaded += MenuBarItem_Loaded;
		}

		private void MenuBarItem_Loaded(object sender, RoutedEventArgs e)
		{
			SynchronizeMenuBar();
		}

		// IUIElement / IUIElementOverridesHelper
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new MenuBarItemAutomationPeer(this);
		}

		// IFramework Override
		protected override void OnApplyTemplate()
		{
			m_button = GetTemplateChild("ContentButton") as Button;

			PopulateContent();
			AttachEventHandlers();

			SynchronizeMenuBar();
		}

		private void SynchronizeMenuBar()
			=> m_menuBar = SharedHelpers.GetAncestorOfType<MenuBar>(VisualTreeHelper.GetParent(this));

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			SetFlyoutDataContext();
		}

		private void SetFlyoutDataContext()
		{
			// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout.
			m_flyout?.SetValue(
				MenuFlyout.DataContextProperty,
				this.DataContext,
				precedence: DependencyPropertyValuePrecedences.Inheritance
			);
		}

		private void PopulateContent()
		{
			// Create flyout
			var flyout = new MenuBarItemFlyout();

			foreach (var flyoutItem in Items)
			{
				flyout.Items.Add(flyoutItem);
			}

			flyout.Placement = FlyoutPlacementMode.Bottom;

			if (m_passThroughElement != null)
			{
				flyout.OverlayInputPassThroughElement = m_passThroughElement;
			}

			m_flyout = flyout;

			if (m_button != null)
			{
				m_button.IsAccessKeyScope = true;
				m_button.ContextFlyout = flyout;
			}

			SetFlyoutDataContext();
		}

		private void AttachEventHandlers()
		{
			_registrations.Disposable = null;

			_activeDisposables = new CompositeDisposable();

			if (m_button != null)
			{
				_activeDisposables.Add(m_button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged));
				_activeDisposables.Add(m_button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged));
			}

			if (m_flyout != null)
			{
				m_flyout.Closed += OnFlyoutClosed;
				m_flyout.Opening += OnFlyoutOpening;

				_activeDisposables.Add(() =>
				{
					m_flyout.Closed -= OnFlyoutClosed;
					m_flyout.Opening -= OnFlyoutOpening;
				});
			}

			PointerEntered += OnMenuBarItemPointerEntered;
			_activeDisposables.Add(() => PointerEntered -= OnMenuBarItemPointerEntered);

			var pointerPressHandler = new PointerEventHandler(OnMenuBarItemPointerPressed);
			AddHandler(UIElement.PointerPressedEvent, pointerPressHandler, true);
			var keyDownHandler = new KeyEventHandler(OnMenuBarItemKeyDown);
			AddHandler(UIElement.KeyDownEvent, keyDownHandler, true);

			_activeDisposables.Add(() =>
			{
				RemoveHandler(UIElement.PointerPressedEvent, pointerPressHandler);
				RemoveHandler(UIElement.KeyDownEvent, keyDownHandler);
			});

			AccessKeyInvoked += OnMenuBarItemAccessKeyInvoked;
			_activeDisposables.Add(() => AccessKeyInvoked -= OnMenuBarItemAccessKeyInvoked);

			_registrations.Disposable = _activeDisposables;
		}

		// Event Handlers
		private void OnMenuBarItemPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			if (m_menuBar != null)
			{
				if (m_menuBar.IsFlyoutOpen)
				{
					ShowMenuFlyout();
				}
			}
		}

		private void OnMenuBarItemPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (m_menuBar != null)
			{
				if (!m_menuBar.IsFlyoutOpen)
				{
					ShowMenuFlyout();
				}
			}
		}

		private void OnMenuBarItemKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var key = args.Key;
			if (key == VirtualKey.Down
				|| key == VirtualKey.Enter
				|| key == VirtualKey.Space)
			{
				ShowMenuFlyout();
			}
			else if (key == VirtualKey.Escape
				&& !m_isFlyoutOpen
				&& m_menuBar != null
				&& IsFromThisTitle(args.OriginalSource))
			{
				// CodeBrix (classic Windows menus): Escape on a menu title with no menu open leaves the menu
				// bar - focus goes back to where it was before the menus were entered. (An Escape that closes
				// an open menu comes from the menu's presenter, not from the title, and only closes it.)
				if (m_menuBar.LeaveMenuBar())
				{
					args.Handled = true;
				}
			}
		}

		private bool IsFromThisTitle(object originalSource)
		{
			for (var element = originalSource as DependencyObject; element != null; element = VisualTreeHelper.GetParent(element))
			{
				if (ReferenceEquals(element, this))
				{
					return true;
				}
			}

			return false;
		}

		private void OnPresenterKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var key = args.Key;
			if (key == VirtualKey.Escape)
			{
				// CodeBrix: the presenter closes the menu itself; remember why, so the title is then focused
				// visibly (OnFlyoutClosed) and a second Escape can leave the menu bar.
				m_isClosingByEscape = true;
				return;
			}

			if (args.Handled)
			{
				// e.g. Right that opened a submenu of the focused item
				return;
			}

			if (key == VirtualKey.Right)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					OpenFlyoutFrom(FlyoutLocation.Left);
				}
				else
				{
					OpenFlyoutFrom(FlyoutLocation.Right);
				}

				// CodeBrix: the key has done its job (the neighbouring menu is open and focused); left
				// unhandled it bubbled on and moved focus back out of the menu just opened, closing it.
				args.Handled = true;
			}
			else if (key == VirtualKey.Left)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					OpenFlyoutFrom(FlyoutLocation.Right);
				}
				else
				{
					OpenFlyoutFrom(FlyoutLocation.Left);
				}

				args.Handled = true;
			}
		}

		private void OnItemsVectorChanged(IObservableVector<MenuFlyoutItemBase> sender, IVectorChangedEventArgs e)
		{
			if (m_flyout != null)
			{
				var index = e.Index;
				switch (e.CollectionChange)
				{
					case CollectionChange.ItemInserted:
						m_flyout.Items.Insert((int)index, Items[(int)index]);
						break;
					case CollectionChange.ItemRemoved:
						m_flyout.Items.RemoveAt((int)index);
						break;
					default:
						break;
				}
			}
		}

		private void OnMenuBarItemAccessKeyInvoked(DependencyObject sender, AccessKeyInvokedEventArgs args)
		{
			ShowMenuFlyout();
			args.Handled = true;
		}

		// Menu Flyout actions
		internal void ShowMenuFlyout()
		{
			if (m_button != null)
			{
				var width = m_button.ActualWidth;
				var height = m_button.ActualHeight;

				if (SharedHelpers.IsFlyoutShowOptionsAvailable())
				{
					// Sets an exclusion rect over the button that generates the flyout so that even if the menu opens upwards
					// (which is the default in touch mode) it doesn't cover the menu bar button.
					FlyoutShowOptions options = new FlyoutShowOptions();
					options.Position = new Point(0, height);
					options.Placement = FlyoutPlacementMode.Bottom;
					options.ExclusionRect = new Rect(0, 0, width, height);
					m_flyout.ShowAt(m_button, options);
				}
				else
				{
					m_flyout.ShowAt(m_button, new Point(0, height));
				}

				// CodeBrix: subscribe to a presenter once. This ran on every open, so after a menu had been
				// opened twice one Right/Left moved two menus at a time.
				if (m_flyout?.m_presenter is { } presenter && !ReferenceEquals(presenter, m_subscribedPresenter))
				{
					if (m_subscribedPresenter is not null)
					{
						m_subscribedPresenter.RemoveHandler(UIElement.KeyDownEvent, m_presenterKeyDownHandler);
					}

					// handledEventsToo: the presenter marks the Escape that closes it handled
					m_presenterKeyDownHandler ??= new KeyEventHandler(OnPresenterKeyDown);
					presenter.AddHandler(UIElement.KeyDownEvent, m_presenterKeyDownHandler, true);
					m_subscribedPresenter = presenter;

					_activeDisposables.Add(() =>
					{
						presenter.RemoveHandler(UIElement.KeyDownEvent, m_presenterKeyDownHandler);
						if (ReferenceEquals(m_subscribedPresenter, presenter))
						{
							m_subscribedPresenter = null;
						}
					});
				}
			}
		}

		internal void CloseMenuFlyout()
		{
			m_flyout.Hide();
		}

		void OpenFlyoutFrom(FlyoutLocation location)
		{
			if (m_menuBar != null)
			{
				int index = m_menuBar.Items.IndexOf(this);
				CloseMenuFlyout();
				if (location == FlyoutLocation.Left)
				{
					m_menuBar.Items[((index - 1) + m_menuBar.Items.Count) % m_menuBar.Items.Count].ShowMenuFlyout();
				}
				else
				{
					m_menuBar.Items[(index + 1) % m_menuBar.Items.Count].ShowMenuFlyout();
				}
			}
		}

#if false
		void AddPassThroughElement(DependencyObject element)
		{
			m_passThroughElement = element;
		}
#endif

		public bool IsFlyoutOpen()
		{
			return m_isFlyoutOpen;
		}

		/// <summary>
		/// True when this item's menu is really open (asks the flyout itself, not the open flag, which
		/// only a Closed event clears).
		/// </summary>
		internal bool IsMenuReallyOpen() => m_flyout is { IsOpen: true };

		/// <summary>
		/// CodeBrix: with the item's menu closed, clears any highlight the title still shows - the open flag
		/// and "Selected" look left behind by a Closed event that never came, or a stale PointerOver /
		/// Pressed look. The next pointer move over the title restores a real PointerOver.
		/// </summary>
		internal void ResetHighlight()
		{
			if (IsMenuReallyOpen())
			{
				return;
			}

			m_isFlyoutOpen = false;
			m_isClosingByEscape = false;
			if (m_button != null)
			{
				VisualStateManager.GoToState(this, "Normal", false);
			}
		}

		public void Invoke()
		{
			if (IsFlyoutOpen())
			{
				CloseMenuFlyout();
			}
			else
			{
				ShowMenuFlyout();
			}
		}

		// Menu Flyout Events
		void OnFlyoutClosed(object sender, object args)
		{
			m_isFlyoutOpen = false;

			if (m_menuBar != null)
			{
				m_menuBar.IsFlyoutOpen = false;
			}

			UpdateVisualStates();

			if (!m_isClosingByEscape)
			{
				// CodeBrix (classic Windows menus): a menu that closed because an item ran (Enter, a click) or its
				// title was clicked hands keyboard focus back to its title, invisibly, where it would stay - keys
				// then go to the menu bar instead of, say, a game surface. Running a command ends the menus, so
				// leave the menu bar - but only if focus really is on this title: a menu closed by moving to the
				// next one, by a click elsewhere, or by a dialog the command opened has put focus there already.
				DispatcherQueue?.TryEnqueue(() =>
				{
					if (!m_isFlyoutOpen &&
						m_menuBar != null &&
						XamlRoot != null &&
						IsFromThisTitle(FocusManager.GetFocusedElement(XamlRoot)) &&
						FocusState != FocusState.Keyboard)
					{
						m_menuBar.LeaveMenuBar();
					}
				});
			}

			if (m_isClosingByEscape)
			{
				// CodeBrix (classic Windows menus): after Escape closes a menu its title stays visibly
				// highlighted - keyboard focus, which draws the focus rectangle - and a second Escape leaves
				// the menu bar. Deferred so it lands after the popup has handed focus back.
				m_isClosingByEscape = false;
				DispatcherQueue?.TryEnqueue(() =>
				{
					if (!m_isFlyoutOpen)
					{
						Focus(FocusState.Keyboard);
					}
				});
			}
		}

		void OnFlyoutOpening(object sender, object args)
		{
			m_isClosingByEscape = false;
			Focus(FocusState.Pointer);

			m_isFlyoutOpen = true;

			if (m_menuBar != null)
			{
				m_menuBar.IsFlyoutOpen = true;
			}

			UpdateVisualStates();
		}

		void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStates();
		}

		void UpdateVisualStates()
		{
			if (m_button != null)
			{
				if (m_isFlyoutOpen)
				{
					VisualStateManager.GoToState(this, "Selected", false);
				}
				else if (m_button.IsPressed)
				{
					VisualStateManager.GoToState(this, "Pressed", false);
				}
				else if (m_button.IsPointerOver)
				{
					VisualStateManager.GoToState(this, "PointerOver", false);
				}
				else
				{
					VisualStateManager.GoToState(this, "Normal", false);
				}
			}
		}
	}
}
