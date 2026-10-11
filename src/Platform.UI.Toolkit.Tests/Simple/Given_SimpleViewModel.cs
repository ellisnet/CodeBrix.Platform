using System;
using System.Collections.Generic;
using System.ComponentModel;
using CodeBrix.Platform.Simple;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

[TestClass]
public class Given_SimpleViewModel
{
	private sealed class TestViewModel : SimpleViewModel
	{
		private string _name;
		private List<string> _items;
		private DayOfWeek _day = DayOfWeek.Monday;
		private string _source;
		private bool _canSave;
		private bool _everything;

		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		public List<string> Items
		{
			get => _items;
			set => SetProperty(ref _items, value);
		}

		public DayOfWeek Day
		{
			get => _day;
			set => SetEnumProperty(ref _day, value);
		}

		[AffectsProperties(nameof(SourceDisplay))]
		public string Source
		{
			get => _source;
			set => SetProperty(ref _source, value);
		}

		public string SourceDisplay => $"[{Source}]";

		[AffectsCommands(nameof(SaveCommand))]
		public bool CanSave
		{
			get => _canSave;
			set => SetProperty(ref _canSave, value);
		}

		[AffectsAllCommands]
		public bool Everything
		{
			get => _everything;
			set => SetProperty(ref _everything, value);
		}

		public SimpleCommand SaveCommand { get; } =
			new(() => { }) { ShouldRaiseCanExecuteOnMainThread = false };

		public SimpleCommand OtherCommand { get; } =
			new(() => { }) { ShouldRaiseCanExecuteOnMainThread = false };

		public void NotifyByCallerMemberName() => ThisPropertyChanged();

		public static Visibility InvokeGetVisibility(bool isVisible) => GetVisibility(isVisible);

		public static string InvokeBuildErrorText(string message, string details) => BuildErrorText(message, details);

		public static string InvokeDescribeException(Exception exception) => DescribeException(exception);
	}

	private static List<string> Changes(TestViewModel viewModel)
	{
		var changes = new List<string>();
		viewModel.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
		return changes;
	}

	[TestMethod]
	public void When_SetProperty_Raises_PropertyChanged()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Name = "first";

		//Assert
		CollectionAssert.Contains(changes, nameof(TestViewModel.Name));
	}

	[TestMethod]
	public void When_SetProperty_Value_Unchanged_Does_Not_Raise()
	{
		//Arrange
		var viewModel = new TestViewModel { Name = "same" };
		var changes = Changes(viewModel);

		//Act
		viewModel.Name = "same";

		//Assert
		changes.Count.Should().Be(0);
	}

	[TestMethod]
	public void When_SetProperty_Class_Overload_Raises()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Items = ["a"];

		//Assert
		CollectionAssert.Contains(changes, nameof(TestViewModel.Items));
	}

	[TestMethod]
	public void When_SetEnumProperty_Raises()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Day = DayOfWeek.Friday;

		//Assert
		CollectionAssert.Contains(changes, nameof(TestViewModel.Day));
	}

	[TestMethod]
	public void When_SetEnumProperty_Unchanged_Does_Not_Raise()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Day = DayOfWeek.Monday;

		//Assert
		changes.Count.Should().Be(0);
	}

	[TestMethod]
	public void When_AffectsProperties_Raises_For_Dependent_Property()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Source = "data";

		//Assert
		CollectionAssert.Contains(changes, nameof(TestViewModel.Source));
		CollectionAssert.Contains(changes, nameof(TestViewModel.SourceDisplay));
	}

	[TestMethod]
	public void When_AffectsCommands_Raises_CanExecuteChanged_On_Named_Command()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var saveRaised = 0;
		var otherRaised = 0;
		viewModel.SaveCommand.CanExecuteChanged += (_, _) => saveRaised++;
		viewModel.OtherCommand.CanExecuteChanged += (_, _) => otherRaised++;

		//Act
		viewModel.CanSave = true;

		//Assert
		saveRaised.Should().Be(1);
		otherRaised.Should().Be(0);
	}

	[TestMethod]
	public void When_AffectsAllCommands_Raises_CanExecuteChanged_On_Every_Command()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var saveRaised = 0;
		var otherRaised = 0;
		viewModel.SaveCommand.CanExecuteChanged += (_, _) => saveRaised++;
		viewModel.OtherCommand.CanExecuteChanged += (_, _) => otherRaised++;

		//Act
		viewModel.Everything = true;

		//Assert
		saveRaised.Should().Be(1);
		otherRaised.Should().Be(1);
	}

	[TestMethod]
	public void When_ThisPropertyChanged_Uses_CallerMemberName()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.NotifyByCallerMemberName();

		//Assert
		CollectionAssert.Contains(changes, nameof(TestViewModel.NotifyByCallerMemberName));
	}

	[TestMethod]
	public void When_GetVisibility_Maps_Bool()
	{
		TestViewModel.InvokeGetVisibility(true).Should().Be(Visibility.Visible);
		TestViewModel.InvokeGetVisibility(false).Should().Be(Visibility.Collapsed);
	}

	[TestMethod]
	public void When_Disposed_PropertyChanged_Handlers_Are_Removed()
	{
		//Arrange
		var viewModel = new TestViewModel();
		var changes = Changes(viewModel);

		//Act
		viewModel.Dispose();
		viewModel.Name = "after dispose";

		//Assert
		changes.Count.Should().Be(0);
	}

	private static Exception Thrown(Func<Exception> create)
	{
		try { throw create(); }
		catch (Exception e) { return e; }
	}

	private static Exception NestedFailure() => Thrown(() => new InvalidOperationException(
		"The document could not be created.",
		Thrown(() => new NotImplementedException("FontResolver not implemented for this platform (FontResolver.cs)."))));

	private static void WithConciseErrorDetails(bool concise, Action test)
	{
		var previous = FeatureConfiguration.SimpleViewModel.UseConciseErrorDetails;
		FeatureConfiguration.SimpleViewModel.UseConciseErrorDetails = concise;
		try { test(); }
		finally { FeatureConfiguration.SimpleViewModel.UseConciseErrorDetails = previous; }
	}

	[TestMethod]
	public void When_ConciseErrorDetails_Off_Details_Are_ToString_Cut_At_195()
	{
		WithConciseErrorDetails(false, () =>
		{
			//Arrange
			var exception = NestedFailure();
			var details = TestViewModel.InvokeDescribeException(exception);

			//Act
			var text = TestViewModel.InvokeBuildErrorText("Could not save.", details);

			//Assert
			details.Should().Be(exception.ToString());
			text.Should().EndWith(exception.ToString().Trim()[..195].Trim() + "[...]");
		});
	}

	[TestMethod]
	public void When_ConciseErrorDetails_On_Details_List_The_Exception_Chain_Without_Stack()
	{
		WithConciseErrorDetails(true, () =>
		{
			//Act
			var details = TestViewModel.InvokeDescribeException(NestedFailure());

			//Assert
			details.Should().Be(
				"System.InvalidOperationException: The document could not be created.\n" +
				"System.NotImplementedException: FontResolver not implemented for this platform (FontResolver.cs).");
			details.Should().NotContain("   at ");
		});
	}

	[TestMethod]
	public void When_ConciseErrorDetails_On_Long_Details_Are_Cut_Between_Words()
	{
		WithConciseErrorDetails(true, () =>
		{
			//Arrange
			var details = string.Join(" ", System.Linq.Enumerable.Repeat("abcdefghij", 30));

			//Act
			var text = TestViewModel.InvokeBuildErrorText("Could not save.", details);

			//Assert
			var shown = text[(text.LastIndexOf('\n') + 1)..];
			shown.Should().EndWith("abcdefghij [...]");
			(shown.Length - " [...]".Length).Should().BeLessThanOrEqualTo(195);
		});
	}

	[TestMethod]
	public void When_ConciseErrorDetails_On_A_Path_Is_Cut_After_A_Separator()
	{
		WithConciseErrorDetails(true, () =>
		{
			//Arrange
			var details = "Access to the path '/" + string.Join("/", System.Linq.Enumerable.Repeat("folder0123456789", 15)) + "' is denied.";

			//Act
			var text = TestViewModel.InvokeBuildErrorText("Could not save.", details);

			//Assert
			text.Should().EndWith("folder0123456789/ [...]");
		});
	}

	[TestMethod]
	public void When_ConciseErrorDetails_On_One_Long_Token_Still_Gets_The_Hard_Cut()
	{
		WithConciseErrorDetails(true, () =>
		{
			//Arrange
			var details = new string('x', 300);

			//Act
			var text = TestViewModel.InvokeBuildErrorText("Could not save.", details);

			//Assert
			text.Should().EndWith(new string('x', 195) + "[...]");
		});
	}

	[TestMethod]
	public void When_Details_Are_Short_They_Are_Shown_Whole_Either_Way()
	{
		foreach (var concise in new[] { false, true })
		{
			WithConciseErrorDetails(concise, () =>
			{
				var text = TestViewModel.InvokeBuildErrorText("Could not save.", "  Disk full.  ");
				text.Should().Contain("Could not save.");
				text.Should().EndWith("\nDisk full.");
			});
		}
	}
}
