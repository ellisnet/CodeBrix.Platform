#nullable enable

using System;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.UI.Extensions;
using CodeBrix.Platform.UI.Xaml;
using CodeBrix.Platform.UI.Xaml.Core;
using CodeBrix.Platform.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>Exercises focus while the real Core visual tree removes a focused subtree.</summary>
public sealed class RemovedElementFocusTests
{
	/// <summary>Removal moves focus out; the opt-in guard prevents a stale reference from taking it back.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RemovedSubtreeCannotRegainFocusWhenEnabled(bool enabled)
	{
		using var tree = new FocusTree(enabled);
		var removed = new StackPanel();
		var first = new Button();
		var second = new Button();
		var remaining = new Button();
		removed.Children.Add(first);
		removed.Children.Add(second);
		tree.Page.Children.Add(removed);
		tree.Page.Children.Add(remaining);
		tree.Load();
		Assert.True(tree.Focus(first));

		tree.Page.Children.Remove(removed);

		Assert.Same(remaining, tree.Manager.FocusedElement);
		Assert.Equal(FocusState.Unfocused, first.FocusState);
		Assert.Equal(FocusState.Unfocused, second.FocusState);
		Assert.Equal(!enabled, tree.Focus(first));
		Assert.Same(enabled ? remaining : first, tree.Manager.FocusedElement);
		if (enabled)
		{
			Assert.False(tree.Focus(removed));
			Assert.False(tree.Focus(second));
		}
	}

	/// <summary>A replaced page and all of its descendants stay unfocused.</summary>
	[Fact]
	public void ReplacedPageCannotRegainFocus()
	{
		using var tree = new FocusTree(true);
		var oldButton = new Button();
		tree.Page.Children.Add(oldButton);
		tree.Load();
		Assert.True(tree.Focus(oldButton));
		var newPage = new StackPanel();
		var newButton = new Button();
		newPage.Children.Add(newButton);

		tree.Root.VisualTree.SetPublicRootVisual(newPage, null, null);
		tree.Load();

		Assert.NotSame(oldButton, tree.Manager.FocusedElement);
		Assert.Equal(FocusState.Unfocused, oldButton.FocusState);
		Assert.False(tree.Focus(oldButton));
		Assert.False(tree.Focus(tree.Page));
		Assert.True(tree.Focus(newButton));
		Assert.Same(newButton, tree.Manager.FocusedElement);
	}

	/// <summary>The target is checked again after synchronous focus-changing handlers run.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CandidateRemovedDuringGettingFocusIsRejectedWhenEnabled(bool enabled)
	{
		using var tree = new FocusTree(enabled);
		var original = new Button();
		var candidate = new Button();
		tree.Page.Children.Add(original);
		tree.Page.Children.Add(candidate);
		tree.Load();
		Assert.True(tree.Focus(original));
		var handlerRan = false;
		candidate.GettingFocus += (_, _) =>
		{
			handlerRan = true;
			tree.Page.Children.Remove(candidate);
		};

		Assert.Equal(!enabled, tree.Focus(candidate));

		Assert.True(handlerRan);
		Assert.Same(enabled ? original : candidate, tree.Manager.FocusedElement);
	}

	/// <summary>A real content root with explicit load ticks instead of a window or render loop.</summary>
	private sealed class FocusTree : IDisposable
	{
		private readonly FocusApplicationScope _application = new();
		private readonly bool _previousFlag = CodeBrix.Platform.UI.FeatureConfiguration.FocusManager.RestrictFocusToLiveTree;
		internal ContentRoot Root { get; }
		internal StackPanel Page { get; } = new();
		internal FocusManager Manager => Root.FocusManager;

		internal FocusTree(bool enabled)
		{
			CodeBrix.Platform.UI.FeatureConfiguration.FocusManager.RestrictFocusToLiveTree = enabled;
			var host = new Grid();
			Root = new ContentRoot(ContentRootType.CoreWindow, Colors.Transparent, host, new CoreServices());
			host.SetVisualTree(Root.VisualTree);
			host.Enter(new EnterParams(true), 0);
			Root.VisualTree.SetPublicRootVisual(Page, null, null);
		}

		internal void Load() => RaiseLoaded(Root.VisualTree.RootElement);
		internal bool Focus(DependencyObject element) => Manager.SetFocusedElement(
			new FocusMovement(element, FocusNavigationDirection.None, FocusState.Programmatic)).WasMoved;

		public void Dispose()
		{
			try
			{
				Manager.ClearFocus();
				Root.VisualTree.SetPublicRootVisual(null, null, null);
				Root.VisualTree.RootElement.LeaveImpl(new LeaveParams(true));
			}
			finally
			{
				CodeBrix.Platform.UI.FeatureConfiguration.FocusManager.RestrictFocusToLiveTree = _previousFlag;
				_application.Dispose();
			}
		}
	}

	private static void RaiseLoaded(UIElement element)
	{
		element.RaiseLoaded();
		foreach (var child in element.GetChildren()) RaiseLoaded(child);
	}

	/// <summary>Supplies application resources and focus-visual settings without starting an application host.</summary>
	private sealed class FocusApplicationScope : IDisposable
	{
		private readonly Application? _previous = Application.Current;

		internal FocusApplicationScope()
		{
			var application = (Application)RuntimeHelpers.GetUninitializedObject(typeof(Application));
			application.Resources = new ResourceDictionary();
			application.FocusVisualKind = FocusVisualKind.DottedLine;
			typeof(Application).GetProperty(nameof(Application.Current))!.SetValue(null, application);
		}

		public void Dispose() => typeof(Application).GetProperty(nameof(Application.Current))!.SetValue(null, _previous);
	}
}
