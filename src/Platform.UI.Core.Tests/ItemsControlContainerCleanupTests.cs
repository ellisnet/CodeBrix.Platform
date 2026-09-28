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
/// WPE1-17: an ItemsControl's item template must never be evaluated against the control's own DataContext. Cleaning up a
/// ContentPresenter container clears its Content and with it the item DataContext; while the container was still in the
/// panel, its materialized template inherited the page view model and re-evaluated {Binding Label}/{Binding Value} against
/// it (an ERROR line per binding when the page view model has no such property). The containers now leave the panel
/// before they are cleaned up. Each test counts the reads of the page view model's Label/Value, which stay at zero.
/// </summary>
public class ItemsControlContainerCleanupTests
{
	/// <summary>An item of the list.</summary>
	public sealed class Fact
	{
		/// <summary>Builds an item.</summary>
		public Fact(string label, string value)
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

	private static DataTemplate FactTemplate()
		=> new(() =>
		{
			var row = new StackPanel();
			var label = new TextBlock();
			label.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(Fact.Label)) });
			var value = new TextBlock();
			value.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(Fact.Value)) });
			row.Children.Add(label);
			row.Children.Add(value);
			return row;
		});

	/// <summary>
	/// A live page (DataContext = the page view model) holding an ItemsControl with a plain panel and the item template. The
	/// panel is in the page's tree, as the ItemsPresenter puts it in an application, so its containers inherit the page's
	/// DataContext whenever they have none of their own.
	/// </summary>
	private static (PageViewModel Page, ItemsControl Items, StackPanel Panel) LivePage(ObservableCollection<Fact> facts)
	{
		var page = new PageViewModel();
		var panel = new StackPanel();
		var items = new ItemsControl { ItemTemplate = FactTemplate(), ItemsPanelRoot = panel, InternalItemsPanelRoot = panel };
		var host = new Grid { DataContext = page };
		host.Children.Add(items);
		host.Children.Add(panel); // host-free there is no ItemsPresenter: the panel inherits the page DataContext from the host
		host.Enter(new EnterParams(isLive: true), 0);
		items.ItemsSource = facts;
		return (page, items, panel);
	}

	private static string[] Texts(StackPanel panel)
		=> panel.Children
			.OfType<ContentPresenter>()
			.SelectMany(presenter => ((StackPanel)presenter.ContentTemplateRoot).Children.OfType<TextBlock>())
			.Select(text => text.Text)
			.ToArray();

	[Fact]
	public void When_The_Items_Are_Cleared_And_Refilled_Then_The_Item_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var facts = new ObservableCollection<Fact> { new("Kind", "Model"), new("Size", "12 KB"), new("Format", "glb") };
		var (page, _, panel) = LivePage(facts);
		var readsBefore = page.Reads;

		//Act (what a viewer does when it shows another asset)
		facts.Clear();
		var readsAfterClear = page.Reads;
		facts.Add(new Fact("Kind", "Image"));
		facts.Add(new Fact("Size", "3 KB"));

		//Assert
		readsBefore.Should().Be(0);
		readsAfterClear.Should().Be(0);
		page.Reads.Should().Be(0);
		Texts(panel).Should().Equal("Kind", "Image", "Size", "3 KB");
	}

	[Fact]
	public void When_An_Item_Is_Replaced_Then_The_Item_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var facts = new ObservableCollection<Fact> { new("Kind", "Model"), new("Size", "12 KB") };
		var (page, items, panel) = LivePage(facts);

		//Act
		facts[0] = new Fact("Kind", "Sound");

		//Assert
		page.Reads.Should().Be(0);
		Texts(panel).Should().Equal("Kind", "Sound", "Size", "12 KB");
		items.IndexFromContainer(items.ContainerFromIndex(1)).Should().Be(1);
	}

	[Fact]
	public void When_One_Item_Is_Removed_Then_The_Item_Template_Never_Reads_The_Page_DataContext()
	{
		//Arrange
		var facts = new ObservableCollection<Fact> { new("Kind", "Model"), new("Size", "12 KB") };
		var (page, _, panel) = LivePage(facts);

		//Act
		facts.RemoveAt(0);

		//Assert
		page.Reads.Should().Be(0);
		Texts(panel).Should().Equal("Size", "12 KB");
	}
}
