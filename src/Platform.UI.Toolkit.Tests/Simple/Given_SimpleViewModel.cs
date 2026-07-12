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
}
