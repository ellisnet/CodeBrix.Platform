#nullable enable

using System;
using System.Text;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineLinkText.cs in the AvalonEdit repo (MIT).
//The link styling moved from CreateTextRun into BuildLayoutText; OnMouseDown became
//OnPointerPressed, with the control-key test read from the pointer event's KeyModifiers (so
//LinkIsClickable now takes the modifiers instead of querying the keyboard device); OnQueryCursor
//(hand cursor) is dropped with the rest of the per-element cursor protocol. Navigation raises the
//RequestNavigate event first, then falls back to a fire-and-forget
//Windows.System.Launcher.LaunchUriAsync instead of Process.Start.

/// <summary>
/// Provides data for the <see cref="VisualLineLinkText.RequestNavigate"/> event.
/// </summary>
public sealed class RequestNavigateEventArgs : EventArgs
{
	/// <summary>
	/// Creates a new RequestNavigateEventArgs instance.
	/// </summary>
	/// <param name="uri">The URI to navigate to.</param>
	/// <param name="targetName">The name of the target window or frame, or null.</param>
	public RequestNavigateEventArgs(Uri uri, string? targetName)
	{
		Uri = uri ?? throw new ArgumentNullException(nameof(uri));
		TargetName = targetName;
	}

	/// <summary>
	/// Gets the URI to navigate to.
	/// </summary>
	public Uri Uri { get; }

	/// <summary>
	/// Gets the name of the target window or frame, or null.
	/// </summary>
	public string? TargetName { get; }

	/// <summary>
	/// Gets/Sets whether the navigation request was handled. When a handler sets this to true,
	/// the default navigation (launching the system handler for the URI) is skipped.
	/// </summary>
	public bool Handled { get; set; }
}

/// <summary>
/// VisualLineElement that represents a piece of text and is a clickable link.
/// </summary>
public class VisualLineLinkText : VisualLineText
{
	/// <summary>
	/// Gets/Sets the URL that is navigated to when the link is clicked.
	/// </summary>
	public Uri? NavigateUri { get; set; }

	/// <summary>
	/// Gets/Sets the window name where the URL will be opened.
	/// </summary>
	public string? TargetName { get; set; }

	/// <summary>
	/// Gets/Sets whether the user needs to press Control to click the link.
	/// The default value is true.
	/// </summary>
	public bool RequireControlModifierForClick { get; set; }

	/// <summary>
	/// Raised when the link is clicked, before the default navigation runs. Set
	/// <see cref="RequestNavigateEventArgs.Handled"/> to true to suppress the default navigation.
	/// </summary>
	public event EventHandler<RequestNavigateEventArgs>? RequestNavigate;

	/// <summary>
	/// Creates a visual line text element with the specified length.
	/// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
	/// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
	/// </summary>
	public VisualLineLinkText(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
	{
		this.RequireControlModifierForClick = true;
	}

	/// <inheritdoc/>
	public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		var properties = TextRunProperties;
		if (properties != null)
		{
			properties.SetForegroundBrush(context.TextView.LinkTextForegroundBrush);
			properties.SetBackgroundBrush(context.TextView.LinkTextBackgroundBrush);
			if (context.TextView.LinkTextUnderline)
				properties.SetTextDecorations(TextDecorations.Underline);
		}
		return base.BuildLayoutText(layoutText, context);
	}

	/// <summary>
	/// Gets whether the link is currently clickable.
	/// </summary>
	/// <param name="modifiers">The keyboard modifiers active for the pointer event.</param>
	/// <remarks>Returns true when control is pressed; or when
	/// <see cref="RequireControlModifierForClick"/> is disabled.</remarks>
	protected virtual bool LinkIsClickable(VirtualKeyModifiers modifiers)
	{
		if (NavigateUri == null)
			return false;
		if (RequireControlModifierForClick)
			return (modifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;
		else
			return true;
	}

	/// <inheritdoc/>
	protected internal override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		if (e == null)
			throw new ArgumentNullException(nameof(e));
		if (e.Handled || !LinkIsClickable(e.KeyModifiers))
			return;
		var point = e.GetCurrentPoint(null);
		if (!point.Properties.IsLeftButtonPressed)
			return;

		var navigateUri = this.NavigateUri;
		if (navigateUri == null)
			return;
		var args = new RequestNavigateEventArgs(navigateUri, this.TargetName);
		RequestNavigate?.Invoke(this, args);
		if (!args.Handled)
		{
			try
			{
				// Fire-and-forget: navigation failures (no handler registered for the scheme,
				// misconfigured browser or mail client) are deliberately ignored.
				_ = Launcher.LaunchUriAsync(navigateUri);
			}
			catch
			{
				// ignore all kinds of errors during browser start
			}
		}
		e.Handled = true;
	}

	/// <inheritdoc/>
	protected override VisualLineText CreateInstance(int length)
	{
		return new VisualLineLinkText(ParentVisualLine, length) {
			NavigateUri = this.NavigateUri,
			TargetName = this.TargetName,
			RequireControlModifierForClick = this.RequireControlModifierForClick
		};
	}
}
