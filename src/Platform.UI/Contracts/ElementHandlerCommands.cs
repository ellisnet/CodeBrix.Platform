#nullable enable

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The imperative commands Core sends to an element handler through <see cref="IElementHandler.Invoke"/>.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile (as the command values their handlers accept). Platform (Skia): not registered.
/// <para>
/// A handler returns <see langword="false"/> for a command it does not support. Core sends
/// <see cref="ChangeView"/> (hook H10) and <see cref="ScrollIntoView"/> (hook H15) itself; the other names are reserved for platform-side callers (the
/// platform's own handlers, navigation and focus plumbing), so every platform uses the same names.
/// </para>
/// </remarks>
internal static class ElementHandlerCommands
{
	/// <summary>ScrollViewer.ChangeView with <see cref="ElementHandlerCapabilities.OwnsScrolling"/>; the argument is a <see cref="ChangeViewRequest"/>.</summary>
	internal const string ChangeView = "ChangeView";

	/// <summary>Brings a descendant's rectangle into view (reserved).</summary>
	internal const string BringIntoView = "BringIntoView";

	/// <summary>Gives the platform view the input focus (reserved).</summary>
	internal const string Focus = "Focus";

	/// <summary>Frame navigation to new content (reserved).</summary>
	internal const string Navigate = "Navigate";

	/// <summary>Frame back navigation (reserved).</summary>
	internal const string GoBack = "GoBack";

	/// <summary>ListViewBase.ScrollIntoView with <see cref="ElementHandlerCapabilities.OwnsItemsHost"/>; the argument is a <see cref="ScrollIntoViewRequest"/>.</summary>
	internal const string ScrollIntoView = "ScrollIntoView";

	/// <summary>Asks the platform view to redraw itself (reserved).</summary>
	internal const string InvalidateNative = "InvalidateNative";
}
