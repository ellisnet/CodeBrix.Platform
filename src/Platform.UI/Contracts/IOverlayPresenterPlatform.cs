#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Presents ContentDialogs and flyouts with the platform's own dialog and popup controls instead of Core's Popup.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered.
/// <para>
/// OPTIONAL service, resolved once with <see cref="PlatformContract.TryResolve{TContract}"/> into
/// <see cref="PlatformServices.OverlayPresenter"/>. When it is absent, or when a Try method returns
/// <see langword="false"/>, Core uses its Popup path unchanged. Core keeps the events and the state: for a dialog,
/// ShowAsync's result task, the button click and Closing deferrals (the presenter reports the user's button through
/// <c>ContentDialog.RaiseButtonFromPlatform</c> and the dialog being shown through
/// <c>ContentDialog.RaiseOpenedFromPlatform</c>); for a flyout, Opening, Opened, Closing, Closed and IsOpen (a
/// dismissal by the user is reported by calling the flyout's public Hide()).
/// </para>
/// </remarks>
internal interface IOverlayPresenterPlatform
{
	/// <summary>
	/// Shows <paramref name="dialog"/> with a platform dialog. Called from ShowAsync, before Core would open its
	/// popup; the dialog's result task already exists.
	/// </summary>
	/// <param name="dialog">The dialog to show.</param>
	/// <returns><see langword="true"/> when the platform presents it (Core's popup is then not opened).</returns>
	bool TryShowContentDialog(ContentDialog dialog);

	/// <summary>
	/// Closes the platform dialog of <paramref name="dialog"/>. Called once the dialog is closing for good (after
	/// the Closing event and its deferral, when not cancelled), before Closed is raised.
	/// </summary>
	/// <param name="dialog">The dialog that closes.</param>
	/// <param name="result">The dialog's result.</param>
	void HideContentDialog(ContentDialog dialog, ContentDialogResult result);

	/// <summary>
	/// Shows <paramref name="flyout"/> with a platform popup, at the point where Core would open its own popup
	/// (after Opening was raised and not cancelled).
	/// </summary>
	/// <param name="flyout">The flyout to show.</param>
	/// <param name="placementTarget">The element the flyout is shown for.</param>
	/// <param name="options">The show options passed to ShowAt, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when the platform presents it (Core's popup is then not opened).</returns>
	bool TryShowFlyout(FlyoutBase flyout, FrameworkElement placementTarget, FlyoutShowOptions? options);

	/// <summary>
	/// Closes the platform popup of <paramref name="flyout"/>. Called when the flyout hides (not when a Closing
	/// handler cancelled the hide).
	/// </summary>
	/// <param name="flyout">The flyout that hides.</param>
	void HideFlyout(FlyoutBase flyout);
}
