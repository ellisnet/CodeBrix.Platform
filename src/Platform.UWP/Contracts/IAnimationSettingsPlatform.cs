namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The operating system's "show animations" setting, behind <see cref="Windows.UI.ViewManagement.UISettings.AnimationsEnabled"/>
/// (and, through it, every framework control that skips its animations when the user turned them off: ScrollViewer,
/// ScrollView, ScrollBar, AnimatedIcon).
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered - animations stay enabled, as before.
/// <para>
/// OPTIONAL contract: resolved once by <c>UISettings</c> (lazily, on the first read of AnimationsEnabled) through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>; when no implementation is registered,
/// AnimationsEnabled is <see langword="true"/>.
/// </para>
/// </remarks>
internal interface IAnimationSettingsPlatform
{
	/// <summary>
	/// Gets a value indicating whether the user allows animations. Read on every access of
	/// <c>UISettings.AnimationsEnabled</c>, so an implementation may report a setting that changes while the application runs.
	/// </summary>
	bool AnimationsEnabled { get; }
}
