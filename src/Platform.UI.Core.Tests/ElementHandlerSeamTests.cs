#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The host-free proof of the per-control handler seam (WPH1): a stand-in handler factory, registered the way a
/// non-Skia platform registers its own, gets handlers created, connected, updated, laid out, hit-tested and
/// disconnected by Core; the raise entry points run each control's own event path; and with no factory registered
/// (the Skia configuration) nothing of the seam runs.
/// </summary>
public class ElementHandlerSeamTests
{
	private static FakeElementHandlerFactory FactoryFor(Func<UIElement, FakeElementHandler?> select, List<string>? log = null)
		=> new(select, log);

	/// <summary>
	/// A button with an explicit template (host-free there is no Application, so no default style supplies one).
	/// </summary>
	private static Button TemplatedButton(object? content = null)
		=> new() { Content = content, Template = new ControlTemplate(() => new Border { Name = "templateRoot", Child = new ContentPresenter() }) };

	/// <summary>A root made live the way a visual tree's root is entered; elements added to it enter the live tree.</summary>
	private static Grid LiveHost()
	{
		var host = new Grid { Name = "host" };
		host.Enter(new EnterParams(isLive: true), 0);
		return host;
	}

	// ---------------------------------------------------------------- T1 / T2: lifecycle and the Skia configuration

	[Fact]
	public void When_Elements_Enter_And_Leave_A_Live_Tree_Then_Handlers_Are_Created_Connected_Parent_First_And_Disconnected()
	{
		//Arrange
		var log = new List<string>();
		var factory = FactoryFor(e => e is FrameworkElement { Name: "parent" or "child" } ? new FakeElementHandler() : null, log);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var parent = new StackPanel { Name = "parent" };
		var child = new Border { Name = "child" };
		parent.Children.Add(child);

		//Act
		host.Children.Add(parent);
		var parentHandler = (FakeElementHandler)parent.Handler!;
		var childHandler = (FakeElementHandler)child.Handler!;
		host.Children.Remove(parent);
		var handlersAfterLeave = (Parent: parent.Handler, Child: child.Handler);
		host.Children.Add(parent);

		//Assert
		log.Take(4).Should().Equal("Create parent", "Connect parent", "Create child", "Connect child");
		parentHandler.ConnectCount.Should().Be(1);
		childHandler.ConnectCount.Should().Be(1);
		parentHandler.ChildCountAtConnect.Should().Be(1);
		childHandler.ParentHandlerConnectedAtConnect.Should().BeTrue();
		log.Skip(4).Take(2).Should().Equal("Disconnect child", "Disconnect parent");
		parentHandler.DisconnectCount.Should().Be(1);
		handlersAfterLeave.Parent.Should().BeNull();
		handlersAfterLeave.Child.Should().BeNull();
		parent.Handler.Should().NotBeNull().And.NotBeSameAs(parentHandler);
		child.Handler.Should().NotBeNull().And.NotBeSameAs(childHandler);
		factory.Created.Count.Should().Be(4);
		host.Handler.Should().BeNull();
	}

	[Fact]
	public void When_No_Handler_Factory_Is_Registered_Then_No_Handler_Exists_And_The_Factory_Is_Never_Asked()
	{
		//Arrange
		var unregistered = FactoryFor(_ => new FakeElementHandler());
		using var _ = ElementHandlerTestPlatform.Activate(null);
		var host = LiveHost();
		var border = new Border { Name = "b" };

		//Act
		host.Children.Add(border);
		border.Width = 40;
		border.Measure(new Size(100, 100));
		border.Arrange(new Rect(0, 0, 100, 100));
		host.Children.Remove(border);

		//Assert
		UIElement.AreHandlersActive.Should().BeFalse();
		PlatformServices.ElementHandlerFactory.Should().BeNull();
		PlatformServices.OverlayPresenter.Should().BeNull();
		border.Handler.Should().BeNull();
		host.Handler.Should().BeNull();
		unregistered.CreateCalls.Should().Be(0);
	}

	[Fact]
	public void When_An_Element_Enters_As_A_Resource_Then_It_Gets_No_Handler()
	{
		//Arrange
		var factory = FactoryFor(_ => new FakeElementHandler());
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var resourceElement = new Border { Name = "resource" };
		var owner = new Grid { Name = "owner" };
		owner.Resources["r"] = resourceElement;
		var host = LiveHost();

		//Act
		host.Children.Add(owner);

		//Assert
		owner.Handler.Should().NotBeNull();
		resourceElement.Handler.Should().BeNull();
	}

	// ---------------------------------------------------------------- T3: UpdateValue

	[Fact]
	public void When_Effective_Values_Change_From_Every_Source_Then_The_Handler_Receives_UpdateValue()
	{
		//Arrange
		var factory = FactoryFor(e => e is FrameworkElement { Name: "page" or "text" } ? new FakeElementHandler() : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var text = new TextBlock { Name = "text" };
		var page = new Page { Name = "page", Content = text };
		var model = new NotifyingModel { Value = "first" };
		text.SetBinding(FrameworkElement.TagProperty, new Binding { Path = new PropertyPath(nameof(NotifyingModel.Value)) });
		host.Children.Add(page);
		var pageHandler = (FakeElementHandler)page.Handler!;
		var textHandler = (FakeElementHandler)text.Handler!;
		pageHandler.Updates.Clear();
		textHandler.Updates.Clear();

		//Act + Assert: a local value
		text.Width = 120;
		textHandler.Updates.Should().Contain(FrameworkElement.WidthProperty);

		//Act + Assert: a style setter
		var style = new Style(typeof(TextBlock));
		style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 31d));
		text.Style = style;
		textHandler.Updates.Should().Contain(TextBlock.FontSizeProperty);

		//Act + Assert: a binding update
		page.DataContext = model;
		textHandler.Updates.Clear();
		model.Value = "second";
		text.Tag.Should().Be("second");
		textHandler.Updates.Should().Contain(FrameworkElement.TagProperty);

		//Act + Assert: an inherited value from the page
		textHandler.Updates.Clear();
		page.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
		pageHandler.Updates.Should().Contain(Control.ForegroundProperty);
		textHandler.Updates.Select(p => p.Name).Should().Contain("Foreground");

		//Act + Assert: a theme change
		pageHandler.Updates.Clear();
		page.RequestedTheme = ElementTheme.Dark;
		pageHandler.Updates.Should().Contain(FrameworkElement.RequestedThemeProperty);
	}

	// ---------------------------------------------------------------- T4: children

	[Fact]
	public void When_Panel_Children_Are_Added_Moved_Removed_And_Cleared_Then_The_Panel_Handler_Is_Told()
	{
		//Arrange
		var factory = FactoryFor(e => e is FrameworkElement { Name: "panel" } ? new FakeElementHandler(ElementHandlerCapabilities.OwnsChildren) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var panel = new StackPanel { Name = "panel" };
		host.Children.Add(panel);
		var handler = (FakeElementHandler)panel.Handler!;

		//Act
		panel.Children.Add(new Border { Name = "a" });
		panel.Children.Add(new Border { Name = "b" });
		panel.Children.Insert(0, new Border { Name = "c" });
		panel.Children.Move(0, 2);
		panel.Children.RemoveAt(0);
		panel.Children.Clear();

		//Assert
		handler.ChildEvents.Should().Equal("+a@0", "+b@1", "+c@0", "~0->2", "-a", "-b", "-c");
	}

	[Fact]
	public void When_A_Hosted_Content_Is_Swapped_Then_The_ContentControl_Handler_Sees_The_Old_Root_Leave_And_The_New_One_Arrive()
	{
		//Arrange
		var factory = FactoryFor(e => e is ContentControl { Name: "cc" }
			? new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent)
			: null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var control = new ContentControl { Name = "cc" };
		host.Children.Add(control);
		var handler = (FakeElementHandler)control.Handler!;

		//Act
		control.Content = new Border { Name = "first" };
		control.Content = new Border { Name = "second" };

		//Assert
		handler.ChildEvents.Should().Equal("+first@0", "-first", "+second@0");
		control.ContentTemplateRoot.Should().BeSameAs(control.Content);
	}

	[Fact]
	public void When_An_ItemsControl_Generates_Its_Items_Into_Its_Panel_Then_The_Panel_Handler_Sees_Each_Container()
	{
		//Arrange
		var host = LiveHost();
		var factory = FactoryFor(e => e is Panel && !ReferenceEquals(e, host) ? new FakeElementHandler(ElementHandlerCapabilities.OwnsChildren) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var items = new ItemsControl { ItemsSource = new[] { "one", "two", "three" }, Template = new ControlTemplate(() => new ItemsPresenter()) };
		items.ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()); // host-free: no default style supplies it
		host.Children.Add(items);

		//Act
		items.Measure(new Size(200, 200));
		// Host-free there is no layout tick, so no Loaded: do what ItemsPresenter.OnLoaded does.
		items.SetItemsPresenter((ItemsPresenter)VisualTreeHelper.GetChild(items, 0));
		items.Measure(new Size(200, 200));
		items.Arrange(new Rect(0, 0, 200, 200));

		//Assert
		factory.Log.Should().Contain("Create StackPanel");
		var panelHandler = factory.Created.Single();
		panelHandler.Element.Should().BeOfType<StackPanel>();
		panelHandler.ChildEvents.Where(e => e.StartsWith("+", StringComparison.Ordinal)).Count().Should().Be(3);
		panelHandler.ChildEvents.Should().Equal("+ContentPresenter@0", "+ContentPresenter@1", "+ContentPresenter@2");
	}

	// ---------------------------------------------------------------- T5 / T6: templates and content

	[Fact]
	public void When_A_Handler_Owns_A_Buttons_Visuals_Then_No_Template_Is_Materialized_And_The_Suppression_Is_Reported_Once()
	{
		//Arrange
		var factory = FactoryFor(e => e is Button ? new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var button = TemplatedButton("OK");
		host.Children.Add(button);

		//Act
		button.Measure(new Size(200, 50));
		button.InvalidateMeasure();
		button.Measure(new Size(210, 50));
		var applied = button.ApplyTemplate();

		//Assert
		var handler = (FakeElementHandler)button.Handler!;
		button.Template.Should().NotBeNull();
		applied.Should().BeFalse();
		VisualTreeHelper.GetChildrenCount(button).Should().Be(0);
		handler.SuppressedTemplates.Should().Equal(button.Template);
	}

	[Fact]
	public void When_A_Template_Was_Applied_Before_The_Handler_Connected_Then_It_Is_Released_On_Connect()
	{
		//Arrange
		var factory = FactoryFor(e => e is Button ? new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var button = TemplatedButton("OK");
		button.ApplyTemplate();
		var childrenBeforeEnter = VisualTreeHelper.GetChildrenCount(button);

		//Act
		host.Children.Add(button);

		//Assert
		childrenBeforeEnter.Should().Be(1);
		((FakeElementHandler)button.Handler!).ChildCountAtConnect.Should().Be(0);
		VisualTreeHelper.GetChildrenCount(button).Should().Be(0);
		button.TemplatedRoot.Should().BeNull();
	}

	[Fact]
	public void When_A_Handler_Hosts_Content_Then_Element_Content_Is_The_ContentTemplateRoot_And_Capability_Changes_Switch_It()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent);
		var factory = FactoryFor(e => e is Button ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var button = TemplatedButton();
		host.Children.Add(button);
		var content = new Border { Name = "content" };

		//Act + Assert: element content is hosted directly
		button.Content = content;
		button.ContentTemplateRoot.Should().BeSameAs(content);
		VisualTreeHelper.GetChild(button, 0).Should().BeSameAs(content);

		//Act + Assert: string content with OwnsVisuals only has no root
		handler.Capabilities = ElementHandlerCapabilities.OwnsVisuals;
		button.NotifyHandlerCapabilitiesChanged();
		button.ContentTemplateRoot.Should().BeNull();
		button.Content = "text";
		button.ContentTemplateRoot.Should().BeNull();
		VisualTreeHelper.GetChildrenCount(button).Should().Be(0);

		//Act + Assert: HostsContent again hosts the string through the implicit TextBlock
		handler.Capabilities = ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent;
		button.NotifyHandlerCapabilitiesChanged();
		button.ContentTemplateRoot.Should().BeOfType<ImplicitTextBlock>();
		button.HandlerCapabilities.Should().Be(ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.HostsContent);
	}

	// ---------------------------------------------------------------- T7 / T8: layout and hit-testing

	[Fact]
	public void When_A_Handler_Measures_Natively_Then_Core_Keeps_Margins_And_Min_Max_And_Arranges_It_At_Its_Rect()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.MeasuresNatively) { MeasureResult = new Size(40, 20) };
		var factory = FactoryFor(e => e is FrameworkElement { Name: "native" } ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var element = new Border
		{
			Name = "native",
			Margin = new Thickness(5),
			MinWidth = 60,
			MaxHeight = 15,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = new Border { Width = 500, Height = 500 },
		};
		host.Children.Add(element);

		//Act
		element.Measure(new Size(200, 100));
		element.Arrange(new Rect(10, 20, 200, 100));

		//Assert
		handler.Measures.Should().Equal(new Size(190, 15));
		element.DesiredSize.Should().Be(new Size(70, 25));
		handler.Arranges.Should().Equal(new Rect(15, 25, 60, 20)); // arranged at its unclipped desired size, clipped to MaxHeight
		element.RenderSize.Should().Be(new Size(60, 20));
		element.Child.DesiredSize.Should().Be(new Size(0, 0)); // Border.MeasureOverride (which measures its child) was not called
	}

	[Fact]
	public void When_A_Handler_Only_Mirrors_An_Element_Then_Core_Lays_It_Out_And_Tells_The_Handler_Where()
	{
		//Arrange
		var handler = new FakeElementHandler();
		var factory = FactoryFor(e => e is FrameworkElement { Name: "mirrored" } ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var element = new Border { Name = "mirrored", Width = 30, Height = 10, Margin = new Thickness(2, 3, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		host.Children.Add(element);

		//Act
		element.Measure(new Size(100, 100));
		element.Arrange(new Rect(0, 0, 100, 100));

		//Assert
		handler.Measures.Should().BeEmpty();
		element.DesiredSize.Should().Be(new Size(32, 13));
		handler.Arranges.Should().Equal(new Rect(2, 3, 30, 10));
	}

	[Fact]
	public void When_A_Handler_Owns_The_Visuals_Then_Hit_Testing_Asks_The_Handler()
	{
		//Arrange
		var owning = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals) { HitTestResult = true };
		var mirroring = new FakeElementHandler();
		var factory = FactoryFor(e => e is FrameworkElement { Name: "owning" } ? owning : e is FrameworkElement { Name: "mirroring" } ? mirroring : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var owningElement = new Border { Name = "owning" };
		var mirroringElement = new Border { Name = "mirroring" };
		host.Children.Add(owningElement);
		host.Children.Add(mirroringElement);

		//Act
		var owningHit = owningElement.HitTest(new Point(3, 4));
		var mirroringHit = mirroringElement.HitTest(new Point(3, 4));

		//Assert
		owningHit.Should().BeTrue();
		owning.HitTests.Should().Equal(new Point(3, 4));
		mirroringHit.Should().BeFalse(); // the test platform's composition visuals hit nothing
		mirroring.HitTests.Should().BeEmpty();
	}

	// ---------------------------------------------------------------- T9: raise entry points

	[Fact]
	public void When_A_Platform_Raises_A_Click_Then_Click_And_The_Command_Run_And_A_CheckBox_Toggles()
	{
		//Arrange
		var button = new Button();
		var clicks = 0;
		button.Click += (_, _) => clicks++;
		var command = new CountingCommand();
		button.Command = command;
		var checkBox = new CheckBox { IsChecked = false };

		//Act
		button.RaiseClickFromPlatform();
		checkBox.RaiseClickFromPlatform();
		button.SetPressedFromPlatform(true);
		var pressed = button.IsPressed;
		button.SetPressedFromPlatform(false);
		button.SetPointerOverFromPlatform(true);

		//Assert
		clicks.Should().Be(1);
		command.Executions.Should().Be(1);
		checkBox.IsChecked.Should().Be(true);
		pressed.Should().BeTrue();
		button.IsPressed.Should().BeFalse();
		button.IsPointerOver.Should().BeTrue();
	}

	[Fact]
	public async Task When_The_Platform_Presents_A_ContentDialog_Then_Core_Popup_Stays_Closed_And_The_Primary_Button_Completes_It()
	{
		//Arrange
		var presenter = new FakeOverlayPresenter();
		using var _ = ElementHandlerTestPlatform.Activate(null, presenter);
		var dialog = new ContentDialog { Title = "t", PrimaryButtonText = "Yes", CloseButtonText = "No" };
		var events = new List<string>();
		dialog.Opened += (_, _) => events.Add("Opened");
		dialog.PrimaryButtonClick += (_, _) => events.Add("PrimaryButtonClick");
		var cancelNext = true;
		dialog.Closing += (_, e) =>
		{
			events.Add("Closing");
			e.Cancel = cancelNext;
		};
		dialog.Closed += (_, e) => events.Add($"Closed {e.Result}");
		var popupEverOpened = false;
		dialog._popup.Opened += (_, _) => popupEverOpened = true;

		//Act
		var show = dialog.ShowAsync().AsTask();
		dialog.RaiseOpenedFromPlatform();
		dialog.RaiseButtonFromPlatform(ContentDialogButton.Primary);
		var completedWhileCancelled = show.IsCompleted;
		cancelNext = false;
		dialog.RaiseButtonFromPlatform(ContentDialogButton.Primary);
		var result = await show.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		//Assert
		completedWhileCancelled.Should().BeFalse();
		result.Should().Be(ContentDialogResult.Primary);
		popupEverOpened.Should().BeFalse();
		dialog._popup.IsOpen.Should().BeFalse();
		presenter.Calls.Should().Equal("ShowDialog", "HideDialog Primary");
		events.Should().Equal("Opened", "PrimaryButtonClick", "Closing", "PrimaryButtonClick", "Closing", "Closed Primary");
	}

	[Fact]
	public void When_A_Platform_Drags_A_Thumb_Then_The_Drag_Events_Follow_With_The_Total_Change()
	{
		//Arrange
		var thumb = new Thumb();
		var events = new List<string>();
		thumb.DragStarted += (_, e) => events.Add($"Started {e.HorizontalOffset},{e.VerticalOffset}");
		thumb.DragDelta += (_, e) => events.Add($"Delta {e.HorizontalChange},{e.VerticalChange}");
		thumb.DragCompleted += (_, e) => events.Add($"Completed {e.HorizontalChange},{e.VerticalChange},{e.Canceled}");

		//Act
		thumb.RaiseDragDeltaFromPlatform(9, 9); // ignored: no drag yet
		thumb.RaiseDragStartedFromPlatform();
		var draggingDuringDrag = thumb.IsDragging;
		thumb.RaiseDragDeltaFromPlatform(3, 4);
		thumb.RaiseDragDeltaFromPlatform(1, 1);
		thumb.RaiseDragCompletedFromPlatform(isCanceled: false);

		//Assert
		draggingDuringDrag.Should().BeTrue();
		thumb.IsDragging.Should().BeFalse();
		events.Should().Equal("Started 0,0", "Delta 3,4", "Delta 1,1", "Completed 4,5,False");
	}

	[Fact]
	public void When_A_Platform_Submits_An_AutoSuggestBox_Query_And_Edits_Its_Text_Then_Its_Events_Are_Raised()
	{
		//Arrange
		var box = new AutoSuggestBox();
		var events = new List<string>();
		box.TextChanged += (_, e) => events.Add($"TextChanged {box.Text} {e.Reason}");
		box.QuerySubmitted += (_, e) => events.Add($"QuerySubmitted {e.QueryText} {e.ChosenSuggestion ?? "none"}");

		//Act
		box.SetTextFromPlatform("abc", AutoSuggestionBoxTextChangeReason.UserInput);
		box.SubmitQueryFromPlatform(null);
		box.SubmitQueryFromPlatform("chosen");

		//Assert
		events.Should().Equal("TextChanged abc UserInput", "QuerySubmitted abc none", "QuerySubmitted abc chosen");
	}

	[Fact]
	public void When_A_Platform_Invokes_Expands_And_Collapses_TreeView_Items_Then_The_TreeView_Events_Are_Raised()
	{
		//Arrange
		var tree = new TreeView();
		var node = new TreeViewNode { Content = "n" };
		var events = new List<string>();
		tree.ItemInvoked += (_, e) => events.Add($"ItemInvoked {e.InvokedItem}");
		tree.Expanding += (_, e) => events.Add($"Expanding {e.Node.Content}");
		tree.Collapsed += (_, e) => events.Add($"Collapsed {e.Node.Content}");

		//Act
		tree.RaiseItemInvokedFromPlatform("item");
		tree.RaiseExpandingFromPlatform(node);
		tree.RaiseCollapsedFromPlatform(node);

		//Assert
		events.Should().Equal("ItemInvoked item", "Expanding n", "Collapsed n");
	}

	[Fact]
	public void When_A_Platform_Clicks_The_TabView_Add_Button_Then_AddTabButtonClick_Is_Raised()
	{
		//Arrange
		var tabView = new TabView();
		var clicks = 0;
		tabView.AddTabButtonClick += (_, _) => clicks++;

		//Act
		tabView.RaiseAddTabButtonClickFromPlatform();

		//Assert
		clicks.Should().Be(1);
	}

	[Fact]
	public void When_A_Platform_Edits_A_TextBox_Then_The_Text_Events_Come_In_WinUI_Order_And_Coercion_Is_Returned()
	{
		//Arrange
		var textBox = new TextBox { MaxLength = 5 };
		var events = new List<string>();
		textBox.BeforeTextChanging += (_, e) => events.Add($"BeforeTextChanging {e.NewText}");
		textBox.TextChanging += (_, _) => events.Add($"TextChanging {textBox.Text}");
		textBox.TextChanged += (_, _) => events.Add($"TextChanged {textBox.Text}");
		textBox.SelectionChanged += (_, _) => events.Add($"SelectionChanged {textBox.SelectionStart}");

		//Act
		var accepted = textBox.ApplyTextFromPlatform("hello", 5, 0);
		var rejected = textBox.ApplyTextFromPlatform("hello world", 11, 0);
		var pasteHandled = textBox.RaisePasteFromPlatform();

		//Assert
		accepted.Should().Be("hello");
		rejected.Should().Be("hello"); // longer than MaxLength: Core keeps its text and the handler writes it back
		textBox.Text.Should().Be("hello");
		events.Take(4).Should().Equal("BeforeTextChanging hello", "TextChanging hello", "TextChanged hello", "SelectionChanged 5");
		pasteHandled.Should().BeFalse();
	}

	[Fact]
	public void When_A_Platform_Edits_A_PasswordBox_Then_PasswordChanged_Is_Raised()
	{
		//Arrange
		var box = new PasswordBox();
		var changes = new List<string>();
		box.PasswordChanged += (_, _) => changes.Add(box.Password);

		//Act
		var effective = box.ApplyPasswordFromPlatform("s3cret");

		//Assert
		effective.Should().Be("s3cret");
		box.Password.Should().Be("s3cret");
		changes.Should().Contain("s3cret");
	}

	[Fact]
	public void When_A_Platform_Submits_Editable_ComboBox_Text_Then_TextSubmitted_Is_Raised_And_The_Value_Is_Kept()
	{
		//Arrange
		var comboBox = new ComboBox { IsEditable = true };
		comboBox.Items.Add("alpha");
		comboBox.Items.Add("beta");
		var submitted = new List<string>();
		comboBox.TextSubmitted += (_, e) => submitted.Add(e.Text);

		//Act
		var matchHandled = comboBox.RaiseTextSubmittedFromPlatform("beta");
		var matchIndex = comboBox.SelectedIndex;
		comboBox.TextSubmitted += (_, e) => e.Handled = e.Text == "handled";
		var appHandled = comboBox.RaiseTextSubmittedFromPlatform("handled");
		var blankHandled = comboBox.RaiseTextSubmittedFromPlatform("   ");

		//Assert
		submitted.Should().Equal("beta", "handled");
		matchHandled.Should().BeFalse();
		matchIndex.Should().Be(1);
		appHandled.Should().BeTrue();
		comboBox.SelectedIndex.Should().Be(1); // a handled submission leaves the selection to the application
		blankHandled.Should().BeFalse();
	}

	[Fact]
	public void When_A_Platform_Commits_NumberBox_Text_Then_Value_And_The_Formatted_Text_Follow()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals);
		var factory = FactoryFor(e => e is NumberBox ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var box = new NumberBox { AcceptsExpression = true };
		host.Children.Add(box);
		var values = new List<double>();
		box.ValueChanged += (_, e) => values.Add(e.NewValue);

		//Act
		box.CommitTextFromPlatform("1 + 2");

		//Assert
		box.Handler.Should().BeSameAs(handler);
		box.Value.Should().Be(3);
		values.Should().Equal(3d);
		box.Text.Should().Be("3");
		handler.Updates.Should().Contain(NumberBox.TextProperty);
	}

	[Fact]
	public void When_A_Platform_Drives_A_NavigationView_Pane_And_Back_Button_Then_Its_Events_Are_Raised()
	{
		//Arrange
		var navigationView = new NavigationView();
		var events = new List<string>();
		navigationView.BackRequested += (_, _) => events.Add("BackRequested");
		navigationView.PaneOpening += (_, _) => events.Add("PaneOpening");
		navigationView.PaneOpened += (_, _) => events.Add("PaneOpened");
		navigationView.PaneClosing += (_, e) =>
		{
			events.Add("PaneClosing");
			e.Cancel = events.Count(x => x == "PaneClosing") == 1;
		};
		navigationView.PaneClosed += (_, _) => events.Add("PaneClosed");

		//Act
		navigationView.RaiseBackRequestedFromPlatform();
		navigationView.RaisePaneOpeningFromPlatform();
		navigationView.RaisePaneOpenedFromPlatform();
		var firstCloseCancelled = navigationView.RaisePaneClosingFromPlatform();
		var secondCloseCancelled = navigationView.RaisePaneClosingFromPlatform();
		navigationView.RaisePaneClosedFromPlatform();

		//Assert
		firstCloseCancelled.Should().BeTrue();
		secondCloseCancelled.Should().BeFalse();
		events.Should().Equal("BackRequested", "PaneOpening", "PaneOpened", "PaneClosing", "PaneClosing", "PaneClosed");
	}

	// ---------------------------------------------------------------- T10: overlay presenter (flyout)

	[Fact]
	public void When_The_Platform_Presents_A_Flyout_Then_Core_Popup_Stays_Closed_And_The_Flyout_Events_Stay_Cores()
	{
		//Arrange
		var presenter = new FakeOverlayPresenter();
		using var _ = ElementHandlerTestPlatform.Activate(null, presenter);
		var target = new Button { Name = "target" };
		var flyout = new Flyout { Content = new Border() };
		var events = new List<string>();
		flyout.Opening += (_, _) => events.Add("Opening");
		flyout.Opened += (_, _) => events.Add("Opened");
		flyout.Closing += (_, _) => events.Add("Closing");
		flyout.Closed += (_, _) => events.Add("Closed");

		//Act
		flyout.ShowAt(target);
		var openWhileShown = flyout.IsOpen;
		var popupOpenWhileShown = flyout._popup?.IsOpen ?? false;
		flyout.Hide();

		//Assert
		openWhileShown.Should().BeTrue();
		popupOpenWhileShown.Should().BeFalse();
		flyout.IsOpen.Should().BeFalse();
		presenter.Calls.Should().Equal("ShowFlyout target", "HideFlyout");
		events.Should().Equal("Opening", "Opened", "Closing", "Closed");
	}

	// ---------------------------------------------------------------- T11: native scrolling

	[Fact]
	public void When_A_Handler_Owns_Scrolling_Then_ChangeView_Goes_To_The_Handler_And_The_Presenter_Is_Not_Moved()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.OwnsScrolling) { InvokeResult = true };
		var factory = FactoryFor(e => e is ScrollViewer ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var scrollViewer = new ScrollViewer { Content = new Border { Height = 1000, Width = 100 } };
		host.Children.Add(scrollViewer);
		scrollViewer.Measure(new Size(100, 100));
		scrollViewer.Arrange(new Rect(0, 0, 100, 100));

		//Act
		var accepted = scrollViewer.ChangeView(null, 50, null, disableAnimation: true);

		//Assert
		accepted.Should().BeTrue();
		var (command, args) = handler.Invokes.Single();
		command.Should().Be(ElementHandlerCommands.ChangeView);
		var request = args.Should().BeOfType<ChangeViewRequest>().Subject;
		request.HorizontalOffset.Should().BeNull();
		request.VerticalOffset.Should().Be(50);
		request.DisableAnimation.Should().BeTrue();
		scrollViewer.VerticalOffset.Should().Be(0);
	}

	// ---------------------------------------------------------------- T12: input

	[Fact]
	public void When_A_Handler_Owns_Input_Then_The_Managed_Gesture_Recognizer_Produces_No_Tapped_For_It()
	{
		//Arrange
		var factory = FactoryFor(e => e is FrameworkElement { Name: "owned" } ? new FakeElementHandler(ElementHandlerCapabilities.OwnsInput) : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var managed = new Border { Name = "managed", Width = 50, Height = 50 };
		var owned = new Border { Name = "owned", Width = 50, Height = 50 };
		host.Children.Add(managed);
		host.Children.Add(owned);
		var taps = new List<string>();
		managed.Tapped += (_, _) => taps.Add("managed");
		owned.Tapped += (_, _) => taps.Add("owned");

		//Act
		Tap(managed, pointerId: 1, frameId: 10);
		Tap(owned, pointerId: 2, frameId: 20);

		//Assert
		owned.HasHandlerCapability(ElementHandlerCapabilities.OwnsInput).Should().BeTrue();
		taps.Should().Equal("managed");
	}

	/// <summary>Feeds a touch press and release at (10, 10) into the element's managed pointer handling.</summary>
	private static void Tap(UIElement element, uint pointerId, uint frameId)
	{
		var device = Windows.Devices.Input.PointerDevice.For(Windows.Devices.Input.PointerDeviceType.Touch);
		var position = new Point(10, 10);

		Windows.UI.Input.PointerPoint Point(uint frame, ulong timestamp, bool inContact)
		{
			var properties = new Windows.UI.Input.PointerPointProperties { IsPrimary = true, IsInRange = true, IsLeftButtonPressed = inContact };
			return new Windows.UI.Input.PointerPoint(frame, timestamp, device, pointerId, position, position, inContact, properties);
		}

		Microsoft.UI.Xaml.Input.PointerRoutedEventArgs Args(Windows.UI.Input.PointerPoint point)
			=> new(new Windows.UI.Core.PointerEventArgs(point, Windows.System.VirtualKeyModifiers.None), element);

		element.OnPointerDown(Args(Point(frameId, 1_000_000, inContact: true)));
		element.OnPointerUp(Args(Point(frameId + 1, 1_050_000, inContact: false)));
	}

	// ---------------------------------------------------------------- the stand-in platform, end to end

	[Fact]
	public void When_A_Stand_In_Platform_Owns_A_Button_Then_It_Suppresses_The_Template_Mirrors_Changes_Lays_It_Out_And_Raises_Its_Click()
	{
		//Arrange
		var handler = new FakeElementHandler(ElementHandlerCapabilities.OwnsVisuals | ElementHandlerCapabilities.MeasuresNatively | ElementHandlerCapabilities.OwnsInput)
		{
			MeasureResult = new Size(88, 32),
		};
		var factory = FactoryFor(e => e is Button ? handler : null);
		using var _ = ElementHandlerTestPlatform.Activate(factory);
		var host = LiveHost();
		var button = TemplatedButton("Save");
		button.Margin = new Thickness(4);
		var clicks = 0;
		button.Click += (_, _) => clicks++;
		host.Children.Add(button);

		//Act
		button.Content = "Save all";
		button.Measure(new Size(300, 100));
		button.Arrange(new Rect(0, 0, 300, 100));
		button.RaiseClickFromPlatform(); // the native widget was clicked

		//Assert
		button.Handler.Should().BeSameAs(handler);
		handler.ConnectCount.Should().Be(1);
		VisualTreeHelper.GetChildrenCount(button).Should().Be(0);
		handler.SuppressedTemplates.Should().ContainSingle();
		handler.Updates.Should().Contain(ContentControl.ContentProperty);
		handler.Measures.Should().ContainSingle();
		button.DesiredSize.Should().Be(new Size(96, 40));
		handler.Arranges.Should().ContainSingle();
		clicks.Should().Be(1);
	}

	private sealed class NotifyingModel : INotifyPropertyChanged
	{
		private string? _value;

		public event PropertyChangedEventHandler? PropertyChanged;

		public string? Value
		{
			get => _value;
			set
			{
				_value = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			}
		}
	}

	private sealed class CountingCommand : ICommand
	{
		public event EventHandler? CanExecuteChanged { add { } remove { } }

		public int Executions { get; private set; }

		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => Executions++;
	}
}
