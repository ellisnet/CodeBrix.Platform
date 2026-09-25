#nullable enable

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The argument of <see cref="ElementHandlerCommands.ChangeView"/>: the parameters of the ScrollViewer.ChangeView
/// call Core forwards to a handler that has <see cref="ElementHandlerCapabilities.OwnsScrolling"/>.
/// </summary>
/// <remarks>
/// Implementers: none (a value passed to Android and Mobile handlers). Platform (Skia): not used.
/// </remarks>
internal sealed class ChangeViewRequest
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ChangeViewRequest"/> class.
	/// </summary>
	/// <param name="horizontalOffset">The requested horizontal offset, or <see langword="null"/> to keep it.</param>
	/// <param name="verticalOffset">The requested vertical offset, or <see langword="null"/> to keep it.</param>
	/// <param name="zoomFactor">The requested zoom factor, or <see langword="null"/> to keep it.</param>
	/// <param name="disableAnimation">Whether the change must happen without animation.</param>
	internal ChangeViewRequest(double? horizontalOffset, double? verticalOffset, double? zoomFactor, bool disableAnimation)
	{
		HorizontalOffset = horizontalOffset;
		VerticalOffset = verticalOffset;
		ZoomFactor = zoomFactor;
		DisableAnimation = disableAnimation;
	}

	/// <summary>Gets the requested horizontal offset, or <see langword="null"/> to keep the current one.</summary>
	internal double? HorizontalOffset { get; }

	/// <summary>Gets the requested vertical offset, or <see langword="null"/> to keep the current one.</summary>
	internal double? VerticalOffset { get; }

	/// <summary>Gets the requested zoom factor, or <see langword="null"/> to keep the current one.</summary>
	internal double? ZoomFactor { get; }

	/// <summary>Gets a value indicating whether the change must happen without animation.</summary>
	internal bool DisableAnimation { get; }
}
