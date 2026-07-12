using System;
using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

[TestClass]
public class Given_SimpleCommand
{
	//Regression tests for the CanExecute fall-through fix (2026-07-10): a command built
	//with any action-only constructor (no can-execute delegate) must be executable.

	[TestMethod]
	public void When_ActionOnly_SyncNoParam_CanExecute() =>
		new SimpleCommand(() => { }).CanExecute(null).Should().Be(true);

	[TestMethod]
	public void When_ActionOnly_SyncWithParam_CanExecute() =>
		new SimpleCommand((Action<object>)(_ => { })).CanExecute(null).Should().Be(true);

	[TestMethod]
	public void When_ActionOnly_AsyncNoParam_CanExecute() =>
		new SimpleCommand((Func<Task>)(() => Task.CompletedTask)).CanExecute(null).Should().Be(true);

	[TestMethod]
	public void When_ActionOnly_AsyncWithParam_CanExecute() =>
		new SimpleCommand((Func<object, Task>)(_ => Task.CompletedTask)).CanExecute(null).Should().Be(true);

	[TestMethod]
	public void When_CanExecute_Delegate_Returns_False() =>
		new SimpleCommand(() => false, () => { }).CanExecute(null).Should().Be(false);

	[TestMethod]
	public void When_CanExecute_Delegate_Returns_True() =>
		new SimpleCommand(() => true, () => { }).CanExecute(null).Should().Be(true);

	[TestMethod]
	public void When_CanExecute_Receives_Parameter()
	{
		//Arrange
		object seen = null;
		var command = new SimpleCommand(p => { seen = p; return true; }, (Action<object>)(_ => { }));

		//Act
		command.CanExecute("the parameter");

		//Assert
		seen.Should().Be("the parameter");
	}

	[TestMethod]
	public void When_Execute_SyncNoParam_Runs()
	{
		//Arrange
		var ran = false;
		var command = new SimpleCommand(() => ran = true);

		//Act
		command.Execute(null);

		//Assert
		Assert.IsTrue(ran);
	}

	[TestMethod]
	public void When_Execute_SyncWithParam_Receives_Parameter()
	{
		//Arrange
		object seen = null;
		var command = new SimpleCommand((Action<object>)(p => seen = p));

		//Act
		command.Execute("payload");

		//Assert
		seen.Should().Be("payload");
	}

	[TestMethod]
	public async Task When_Execute_AsyncNoParam_Runs()
	{
		//Arrange
		var completion = new TaskCompletionSource();
		var command = new SimpleCommand((Func<Task>)(() =>
		{
			completion.SetResult();
			return Task.CompletedTask;
		}));

		//Act
		command.Execute(null);

		//Assert
		await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestMethod]
	public async Task When_Execute_AsyncWithParam_Receives_Parameter()
	{
		//Arrange
		var completion = new TaskCompletionSource<object>();
		var command = new SimpleCommand((Func<object, Task>)(p =>
		{
			completion.SetResult(p);
			return Task.CompletedTask;
		}));

		//Act
		command.Execute("payload");

		//Assert
		(await completion.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be("payload");
	}

	[TestMethod]
	public void When_RaiseCanExecuteChanged_Raises_Event()
	{
		//Arrange
		var raised = 0;
		var command = new SimpleCommand(() => { }) { ShouldRaiseCanExecuteOnMainThread = false };
		command.CanExecuteChanged += (_, _) => raised++;

		//Act
		command.RaiseCanExecuteChanged();

		//Assert
		raised.Should().Be(1);
	}

	[TestMethod]
	public void When_RaiseCanExecuteChanged_Without_Subscribers_Does_Not_Throw()
	{
		//Arrange
		var command = new SimpleCommand(() => { }) { ShouldRaiseCanExecuteOnMainThread = false };

		//Act + Assert (no exception)
		command.RaiseCanExecuteChanged();
	}

	[TestMethod]
	public void When_Disposed_CanExecute_Is_False()
	{
		//Arrange
		var command = new SimpleCommand(() => true, () => { });

		//Act
		command.Dispose();

		//Assert
		command.CanExecute(null).Should().Be(false);
	}

	[TestMethod]
	public void When_Disposed_Execute_Does_Not_Run()
	{
		//Arrange
		var ran = false;
		var command = new SimpleCommand(() => ran = true);

		//Act
		command.Dispose();
		command.Execute(null);

		//Assert
		Assert.IsFalse(ran);
	}

	[TestMethod]
	public void When_Disposed_CanExecuteChanged_Handlers_Are_Removed()
	{
		//Arrange
		var raised = 0;
		var command = new SimpleCommand(() => { }) { ShouldRaiseCanExecuteOnMainThread = false };
		command.CanExecuteChanged += (_, _) => raised++;

		//Act
		command.Dispose();
		command.RaiseCanExecuteChanged();

		//Assert
		raised.Should().Be(0);
	}
}
