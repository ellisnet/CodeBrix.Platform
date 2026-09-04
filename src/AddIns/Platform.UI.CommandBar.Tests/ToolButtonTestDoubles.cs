using System;
using System.Windows.Input;
using CodeBrix.Platform.UI.CommandBar;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// A tool bar icon source that stands in for the real icon sources.
/// </summary>
/// <remarks>
/// The button family only ever asks an icon source for an element to put in its icon slot, so an
/// element of a known size answers the question the button asks. It also keeps this suite
/// independent of the SVG and raster sources, whose own behaviour is tested where they live.
/// </remarks>
internal sealed class FakeToolIconSource : ToolIconSource
{
	/// <summary>The edge length the fake icon reports when it is measured.</summary>
	public const double NaturalSize = 16d;

	/// <summary>Counts the elements handed out, so a test can tell a rebuild from a reuse.</summary>
	public int CreatedElementCount { get; private set; }

	/// <inheritdoc/>
	protected override IconElement CreateIconElementCore()
	{
		CreatedElementCount++;
		return new FakeIconElement();
	}
}

/// <summary>An icon element of a fixed size, so a measure has something definite to measure.</summary>
internal sealed class FakeIconElement : IconElement
{
	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
		=> new(FakeToolIconSource.NaturalSize, FakeToolIconSource.NaturalSize);

	/// <inheritdoc/>
	protected override Size ArrangeOverride(Size finalSize) => finalSize;
}

/// <summary>
/// A command built from an action alone, with no way of saying no.
/// </summary>
/// <remarks>
/// This is the shape of <c>SimpleCommand</c>'s action-only constructor, which the platform's own
/// view-model helpers offer and which applications reach for most often. It once answered
/// CanExecute false and disabled every button bound to it; the shape is kept under test here so a
/// tool bar button is never the place that regression shows up again.
/// </remarks>
internal sealed class ActionOnlyCommand : ICommand
{
	private readonly Action _execute;

	/// <summary>Initializes a command that always can execute.</summary>
	/// <param name="execute">What the command does.</param>
	public ActionOnlyCommand(Action execute)
	{
		_execute = execute;
	}

	/// <inheritdoc/>
	public event EventHandler? CanExecuteChanged;

	/// <summary>Gets how many times the command was executed.</summary>
	public int ExecutionCount { get; private set; }

	/// <inheritdoc/>
	public bool CanExecute(object? parameter) => true;

	/// <inheritdoc/>
	public void Execute(object? parameter)
	{
		ExecutionCount++;
		_execute();
	}

	/// <summary>Announces that the answer to CanExecute may have changed.</summary>
	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A command whose answer to CanExecute a test can change at will.
/// </summary>
internal sealed class SwitchableCommand : ICommand
{
	private bool _canExecute;

	/// <summary>Initializes the command in the given state.</summary>
	/// <param name="canExecute">Whether the command can execute to begin with.</param>
	public SwitchableCommand(bool canExecute = true)
	{
		_canExecute = canExecute;
	}

	/// <inheritdoc/>
	public event EventHandler? CanExecuteChanged;

	/// <summary>Gets how many times the command was executed.</summary>
	public int ExecutionCount { get; private set; }

	/// <summary>Gets the parameter the command was last executed with.</summary>
	public object? LastParameter { get; private set; }

	/// <summary>Gets how many handlers are currently subscribed to CanExecuteChanged.</summary>
	public int SubscriberCount => CanExecuteChanged?.GetInvocationList().Length ?? 0;

	/// <summary>Sets whether the command can execute, and announces the change.</summary>
	/// <param name="value">The new answer.</param>
	public void SetCanExecute(bool value)
	{
		_canExecute = value;
		CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc/>
	public bool CanExecute(object? parameter) => _canExecute;

	/// <inheritdoc/>
	public void Execute(object? parameter)
	{
		ExecutionCount++;
		LastParameter = parameter;
	}
}
