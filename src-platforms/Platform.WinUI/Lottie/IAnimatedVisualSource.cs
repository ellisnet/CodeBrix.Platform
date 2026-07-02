#nullable enable

using System;
using Windows.Foundation;
using Windows.UI;

namespace CodeBrix.Platform.WinUI.Lottie;

/// <summary>
/// An animated visual source that an <see cref="AnimatedVisualPlayer"/> can display
/// and drive. Mirrors the source-facing player contract of the CodeBrix.Platform
/// (Uno-based) Lottie package, where the player delegates playback to its source.
/// </summary>
public interface IAnimatedVisualSource
{
    /// <summary>
    /// Attaches this source to (or detaches it from, with <c>null</c>) the given player,
    /// triggering the load of the animation payload.
    /// </summary>
    void Update(AnimatedVisualPlayer? player);

    /// <summary>Measures the animation for the given available size, honoring the player's Stretch.</summary>
    Size Measure(Size availableSize);

    /// <summary>Starts playback from/to the given progress values (0..1), optionally looping.</summary>
    void Play(double fromProgress, double toProgress, bool looped);

    /// <summary>Stops playback.</summary>
    void Stop();

    /// <summary>Pauses playback, retaining the current position.</summary>
    void Pause();

    /// <summary>Resumes playback after a <see cref="Pause"/>.</summary>
    void Resume();

    /// <summary>Moves the animation to the given progress value (0..1) without playing.</summary>
    void SetProgress(double progress);

    /// <summary>Notifies the source that its player has been loaded into the visual tree.</summary>
    void Load();

    /// <summary>Notifies the source that its player has been unloaded from the visual tree.</summary>
    void Unload();
}

/// <summary>
/// An <see cref="IAnimatedVisualSource"/> whose animation payload is identified by a URI.
/// </summary>
public interface IAnimatedVisualSourceWithUri : IAnimatedVisualSource
{
    /// <summary>The URI of the animation payload.</summary>
    Uri? UriSource { get; set; }
}

/// <summary>
/// An <see cref="IAnimatedVisualSource"/> whose named colors can be changed at runtime
/// (see <see cref="ThemableLottieVisualSource"/>).
/// </summary>
public interface IThemableAnimatedVisualSource : IAnimatedVisualSource
{
    /// <summary>Sets the color bound to <paramref name="propertyName"/> in the animation.</summary>
    void SetColorThemeProperty(string propertyName, Color? color);

    /// <summary>Gets the color currently bound to <paramref name="propertyName"/>, if any.</summary>
    Color? GetColorThemeProperty(string propertyName);
}
