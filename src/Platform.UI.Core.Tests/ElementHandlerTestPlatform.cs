#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// A stand-in for a non-Skia platform's element handler services (the seam CodeBrix.Android implements): a factory
/// and an overlay presenter that record every call Core makes. They are registered with the registry once, through
/// builders that return the CURRENT stand-in - none by default, so the process keeps the Skia configuration (no
/// handler service registered, <see cref="UIElement.AreHandlersActive"/> false) except inside an
/// <see cref="Activate"/> scope.
/// </summary>
internal static class ElementHandlerTestPlatform
{
	private static readonly object _gate = new();
	private static bool _registered;
	private static FakeElementHandlerFactory? _factory;
	private static FakeOverlayPresenter? _presenter;

	/// <summary>
	/// Makes <paramref name="factory"/> and <paramref name="presenter"/> the platform's handler services until the
	/// returned scope is disposed (the suite runs one test at a time, AssemblyInfo.cs).
	/// </summary>
	/// <param name="factory">The factory to register, or <see langword="null"/>.</param>
	/// <param name="presenter">The overlay presenter to register, or <see langword="null"/>.</param>
	/// <returns>The scope; disposing it returns to the Skia configuration.</returns>
	internal static IDisposable Activate(FakeElementHandlerFactory? factory, FakeOverlayPresenter? presenter = null)
	{
		lock (_gate)
		{
			if (!_registered)
			{
				// A builder that returns null means "not registered" to the registry (CreateInstance returns false).
				ApiExtensibility.Register(typeof(IElementHandlerFactoryPlatform), _ => _factory!);
				ApiExtensibility.Register(typeof(IOverlayPresenterPlatform), _ => _presenter!);
				_registered = true;
			}

			_factory = factory;
			_presenter = presenter;
			UIElement.RefreshElementHandlerServices();
		}

		return new Scope();
	}

	private sealed class Scope : IDisposable
	{
		public void Dispose()
		{
			lock (_gate)
			{
				_factory = null;
				_presenter = null;
				UIElement.RefreshElementHandlerServices();
			}
		}
	}
}

/// <summary>A factory that creates the handler <see cref="Select"/> chooses for each element, and logs it.</summary>
internal sealed class FakeElementHandlerFactory : IElementHandlerFactoryPlatform
{
	internal FakeElementHandlerFactory(Func<UIElement, FakeElementHandler?> select, List<string>? log = null)
	{
		Select = select;
		Log = log ?? new List<string>();
	}

	internal Func<UIElement, FakeElementHandler?> Select { get; }

	internal List<string> Log { get; }

	internal List<FakeElementHandler> Created { get; } = new();

	internal int CreateCalls { get; private set; }

	public IElementHandler? CreateHandler(UIElement element)
	{
		CreateCalls++;
		var handler = Select(element);
		if (handler is not null)
		{
			handler.Log = Log;
			Created.Add(handler);
			Log.Add($"Create {FakeElementHandler.NameOf(element)}");
		}

		return handler;
	}
}

/// <summary>A handler that records every call Core makes and answers with configurable values.</summary>
internal class FakeElementHandler : IElementHandler
{
	internal FakeElementHandler(ElementHandlerCapabilities capabilities = ElementHandlerCapabilities.None)
	{
		Capabilities = capabilities;
	}

	internal List<string> Log { get; set; } = new();

	public ElementHandlerCapabilities Capabilities { get; set; }

	public object? PlatformView { get; } = new object();

	internal UIElement? Element { get; private set; }

	internal int ConnectCount { get; private set; }

	internal int DisconnectCount { get; private set; }

	internal List<DependencyProperty> Updates { get; } = new();

	internal List<string> ChildEvents { get; } = new();

	internal List<Size> Measures { get; } = new();

	internal List<Rect> Arranges { get; } = new();

	internal List<Point> HitTests { get; } = new();

	internal List<FrameworkTemplate?> SuppressedTemplates { get; } = new();

	internal List<(string Command, object? Args)> Invokes { get; } = new();

	internal Size MeasureResult { get; set; }

	internal bool HitTestResult { get; set; } = true;

	internal bool InvokeResult { get; set; } = true;

	internal bool ChildCountAtConnectWasRead { get; private set; }

	internal int ChildCountAtConnect { get; private set; }

	internal bool ParentHandlerConnectedAtConnect { get; private set; }

	/// <summary>Runs at the end of <see cref="Connect"/> (a handler that changes its capabilities while it connects).</summary>
	internal Action<FakeElementHandler, UIElement>? OnConnect { get; set; }

	public void Connect(UIElement element)
	{
		Element = element;
		ConnectCount++;
		ChildCountAtConnect = VisualTreeHelper.GetChildrenCount(element);
		ChildCountAtConnectWasRead = true;
		ParentHandlerConnectedAtConnect = VisualTreeHelper.GetParent(element) is UIElement { Handler: not null };
		Log.Add($"Connect {NameOf(element)}");
		OnConnect?.Invoke(this, element);
	}

	public void Disconnect()
	{
		DisconnectCount++;
		Log.Add($"Disconnect {NameOf(Element)}");
	}

	public void UpdateValue(DependencyProperty property) => Updates.Add(property);

	public bool Invoke(string command, object? args)
	{
		Invokes.Add((command, args));
		return InvokeResult;
	}

	public void OnChildAdded(UIElement child, int index) => ChildEvents.Add($"+{NameOf(child)}@{index}");

	public void OnChildRemoved(UIElement child) => ChildEvents.Add($"-{NameOf(child)}");

	public void OnChildMoved(int oldIndex, int newIndex) => ChildEvents.Add($"~{oldIndex}->{newIndex}");

	public Size Measure(Size availableSize)
	{
		Measures.Add(availableSize);
		return MeasureResult;
	}

	public void Arrange(Rect finalRect) => Arranges.Add(finalRect);

	public bool HitTest(Point relativeLocation)
	{
		HitTests.Add(relativeLocation);
		return HitTestResult;
	}

	public void OnTemplateSuppressed(FrameworkTemplate? template) => SuppressedTemplates.Add(template);

	internal static string NameOf(UIElement? element)
		=> element is FrameworkElement { Name: { Length: > 0 } name } ? name : element?.GetType().Name ?? "null";
}

/// <summary>An overlay presenter that accepts (or declines) every dialog and flyout, and records the calls.</summary>
internal sealed class FakeOverlayPresenter : IOverlayPresenterPlatform
{
	internal bool Accept { get; set; } = true;

	internal List<string> Calls { get; } = new();

	internal Action<ContentDialog>? OnShowDialog { get; set; }

	public bool TryShowContentDialog(ContentDialog dialog)
	{
		Calls.Add("ShowDialog");
		if (Accept)
		{
			OnShowDialog?.Invoke(dialog);
		}

		return Accept;
	}

	public void HideContentDialog(ContentDialog dialog, ContentDialogResult result) => Calls.Add($"HideDialog {result}");

	public bool TryShowFlyout(FlyoutBase flyout, FrameworkElement placementTarget, FlyoutShowOptions? options)
	{
		Calls.Add($"ShowFlyout {FakeElementHandler.NameOf(placementTarget)}");
		return Accept;
	}

	public void HideFlyout(FlyoutBase flyout) => Calls.Add("HideFlyout");
}

/// <summary>
/// The platform side of a text box as a platform whose native field does the editing would have it
/// (<see cref="IsManagedEditing"/> false): it keeps the selection it is told about and records nothing else.
/// </summary>
internal sealed class FakeTextBoxPlatform : ITextBoxPlatform
{
	public bool IsManagedEditing => false;

	public int SelectionStart { get; private set; }

	public int SelectionLength { get; private set; }

	public bool IsBackwardSelection => false;

	public TextBox.CaretDisplayMode CaretMode => default;

	public int SelectionStartBeforeKeyDown => SelectionStart;

	public void Initialize() { }

	public void OnUnloaded() { }

	public void ResetTextView() { }

	public void UpdateTextView() { }

	public void SetTextNative(string text) { }

	public void OnTextChanged() { }

	public void OnTextInputProcessed(string oldText) { }

	public string CoerceMultilineText(string text) => text;

	public void ClampPendingSelection(int textLength) { }

	public void ClearPendingSelection() { }

	public void OnBeforeTextChangingCanceled() { }

	public void Select(int start, int length)
	{
		SelectionStart = start;
		SelectionLength = length;
	}

	public void OnPasteFromClipboard(string adjustedClipboardText, int selectionStart, string newText) { }

	public void OnPasteStarting() { }

	public void OnPasteFinished() { }

	public void OnCutSelectionToClipboard() { }

	public void OnCutStarting() { }

	public void OnCutFinished() { }

	public void Undo() { }

	public void Redo() { }

	public void ClearUndoRedoHistory() { }

	public void OnKeyDown(KeyRoutedEventArgs args) { }

	public void OnPostKeyDown(KeyRoutedEventArgs args) { }

	public void OnPointerPressed(PointerRoutedEventArgs args) { }

	public void OnPointerReleased(PointerRoutedEventArgs args, bool wasFocused) { }

	public void OnPointerCaptureLost(PointerRoutedEventArgs args) { }

	public void OnPointerMoved(PointerRoutedEventArgs args) { }

	public void OnRightTapped(RightTappedRoutedEventArgs args) { }

	public void OnFocusedByPointer() { }

	public void OnFocusStateChanged(FocusState focusState, bool initial) { }

	public void DispatchUpdateScrolling() { }

	public void OnForegroundColorChanged(Brush newValue) { }

	public void OnSelectionHighlightColorChanged(SolidColorBrush brush) { }

	public void UpdateFont() { }

	public void UpdateTextViewProperties() { }

	public void OnMaxLengthChanged() { }

	public void OnFlowDirectionChanged() { }

	public void OnTextWrappingChanged() { }

	public void OnTextAlignmentChanged() { }

	public void InvalidateOverlayLayout() { }

	public void OnPasswordCharChanged() { }

	public void SetPasswordRevealState(PasswordRevealState state) { }
}
