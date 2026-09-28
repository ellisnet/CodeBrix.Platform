#nullable enable

using System.Collections.ObjectModel;
using System.Linq;
using CodeBrix.Platform.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-20: a data template's content must receive its item DataContext before its bindings are first evaluated, on the
/// FIRST generation too (WPE1-17 fenced the Reset/Replace paths). A plain ContentControl has no control template on Skia,
/// so it presents its content itself: the materialized template root was added as its child first, inherited the page's
/// DataContext and evaluated {Binding Label}/{Binding Value} against it, and only then received the Content as its
/// DataContext (an ERROR line per binding when the page view model has no such property). The root now gets the Content
/// before it joins the tree. Each test counts the reads of the page view model's Label/Value, which stay at zero.
/// </summary>
public class ContentTemplateFirstGenerationTests
{
	/// <summary>The item a template presents.</summary>
	public sealed class Status
	{
		/// <summary>Builds an item.</summary>
		public Status(string label, string value)
		{
			Label = label;
			Value = value;
		}

		/// <summary>The label of the item.</summary>
		public string Label { get; }

		/// <summary>The value of the item.</summary>
		public string Value { get; }
	}

	/// <summary>A page view model with properties of the same names as the item's, which counts every read of them.</summary>
	public sealed class PageViewModel
	{
		/// <summary>How many times Label or Value was read.</summary>
		public int Reads { get; private set; }

		/// <summary>The page's label: an item template must never read it.</summary>
		public string Label
		{
			get
			{
				Reads++;
				return "PAGE";
			}
		}

		/// <summary>The page's value: an item template must never read it.</summary>
		public string Value
		{
			get
			{
				Reads++;
				return "PAGE";
			}
		}
	}

	private static DataTemplate StatusTemplate()
		=> new(() =>
		{
			var row = new StackPanel();
			var label = new TextBlock();
			label.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(Status.Label)) });
			var value = new TextBlock();
			value.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(Status.Value)) });
			row.Children.Add(label);
			row.Children.Add(value);
			return row;
		});

	/// <summary>A live page: a root whose DataContext is the page view model.</summary>
	private static (PageViewModel Page, Grid Root) LivePage()
	{
		var page = new PageViewModel();
		var root = new Grid { DataContext = page };
		root.Enter(new EnterParams(isLive: true), 0);
		return (page, root);
	}

	private static string[] Texts(UIElement templateRoot)
		=> ((StackPanel)templateRoot).Children.OfType<TextBlock>().Select(text => text.Text).ToArray();

	[Fact]
	public void When_A_ContentControl_With_Content_And_A_ContentTemplate_Is_Loaded_In_A_Page_Then_The_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange (a status strip built in code and added to the page)
		var (page, root) = LivePage();
		var status = new Status("Shell", "running");
		var strip = new ContentControl { Content = status, ContentTemplate = StatusTemplate() };

		//Act
		root.Children.Add(strip);
		root.RaiseLoaded();
		strip.RaiseLoaded();

		//Assert
		page.Reads.Should().Be(0);
		strip.ContentTemplateRoot.Should().NotBeNull();
		((FrameworkElement)strip.ContentTemplateRoot).DataContext.Should().BeSameAs(status);
		Texts(strip.ContentTemplateRoot).Should().Equal("Shell", "running");
	}

	[Fact]
	public void When_A_ContentTemplate_Is_Given_To_A_Live_ContentControl_That_Has_Content_Then_The_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var (page, root) = LivePage();
		var strip = new ContentControl();
		root.Children.Add(strip);
		strip.Content = new Status("Shell", "running");

		//Act
		strip.ContentTemplate = StatusTemplate();

		//Assert
		page.Reads.Should().Be(0);
		Texts(strip.ContentTemplateRoot).Should().Equal("Shell", "running");
	}

	[Fact]
	public void When_The_Content_Of_A_Loaded_ContentControl_Changes_Then_The_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var (page, root) = LivePage();
		var strip = new ContentControl { Content = new Status("Shell", "running"), ContentTemplate = StatusTemplate() };
		root.Children.Add(strip);
		root.RaiseLoaded();
		strip.RaiseLoaded();

		//Act
		strip.Content = new Status("Shell", "exited");

		//Assert
		page.Reads.Should().Be(0);
		Texts(strip.ContentTemplateRoot).Should().Equal("Shell", "exited");
	}

	[Fact]
	public void When_A_ContentPresenter_With_Content_And_A_ContentTemplate_Is_Loaded_In_A_Page_Then_The_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var (page, root) = LivePage();
		var presenter = new ContentPresenter { Content = new Status("Shell", "running"), ContentTemplate = StatusTemplate() };

		//Act
		root.Children.Add(presenter);
		root.RaiseLoaded();
		presenter.RaiseLoaded();

		//Assert
		page.Reads.Should().Be(0);
		Texts(presenter.ContentTemplateRoot).Should().Equal("Shell", "running");
	}

	[Fact]
	public void When_An_ItemsControl_Generates_Its_First_Containers_Then_The_Item_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var (page, root) = LivePage();
		var panel = new StackPanel();
		var items = new ItemsControl { ItemsPanelRoot = panel, InternalItemsPanelRoot = panel };
		root.Children.Add(items);
		root.Children.Add(panel); // host-free there is no ItemsPresenter: the panel inherits the page DataContext from the root
		var statuses = new ObservableCollection<Status>();
		items.ItemsSource = statuses;

		//Act (items first, the template afterwards, then more items)
		statuses.Add(new Status("Shell", "running"));
		items.ItemTemplate = StatusTemplate();
		statuses.Add(new Status("Size", "110 x 99"));
		items.Items.Count.Should().Be(2);

		//Assert
		page.Reads.Should().Be(0);
		panel.Children
			.OfType<ContentPresenter>()
			.SelectMany(presenter => Texts(presenter.ContentTemplateRoot))
			.Should().Equal("Shell", "running", "Size", "110 x 99");
	}
}
