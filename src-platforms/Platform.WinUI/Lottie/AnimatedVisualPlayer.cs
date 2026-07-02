#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CodeBrix.Platform.WinUI.Lottie;

/// <summary>
/// A player element that displays and controls an <see cref="IAnimatedVisualSource"/>
/// (such as <see cref="LottieVisualSource"/>). Provides the same XAML surface as the
/// AnimatedVisualPlayer used with the CodeBrix.Platform (Uno-based) Lottie package:
/// <code>
/// &lt;lottie:AnimatedVisualPlayer AutoPlay="True"&gt;
///     &lt;lottie:LottieVisualSource UriSource="ms-appx:///Assets/animation.json" /&gt;
/// &lt;/lottie:AnimatedVisualPlayer&gt;
/// </code>
/// <para>
/// The native Windows App SDK <c>AnimatedVisualPlayer</c> requires Composition/Win2D-based
/// sources, so this player instead follows the CodeBrix.Platform model: the source renders
/// through Skia (SkiaSharp.Skottie) onto a render surface hosted by this element, and the
/// player delegates playback to the source.
/// </para>
/// </summary>
[ContentProperty(Name = nameof(Source))]
public partial class AnimatedVisualPlayer : Panel
{
    private TaskCompletionSource? _playCompletion;

    /// <summary>Initializes a new <see cref="AnimatedVisualPlayer"/>.</summary>
    public AnimatedVisualPlayer()
    {
        Loaded += (_, _) => Source?.Load();
        Unloaded += (_, _) => Source?.Unload();
    }

    #region Dependency properties

    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source), typeof(IAnimatedVisualSource), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(null, OnSourceChanged));

    /// <summary>The animated visual source to display.</summary>
    public IAnimatedVisualSource? Source
    {
        get => (IAnimatedVisualSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Identifies the <see cref="AutoPlay"/> dependency property.</summary>
    public static readonly DependencyProperty AutoPlayProperty =
        DependencyProperty.Register(
            nameof(AutoPlay), typeof(bool), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(true));

    /// <summary>
    /// Whether the animation starts playing (looped, over its full range) as soon as it
    /// is loaded. Defaults to true.
    /// </summary>
    public bool AutoPlay
    {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    /// <summary>Identifies the <see cref="Stretch"/> dependency property.</summary>
    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch), typeof(Stretch), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(Stretch.Uniform, OnLayoutAffectingPropertyChanged));

    /// <summary>How the animation is resized to fill its allocated space. Defaults to Uniform.</summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>Identifies the <see cref="PlaybackRate"/> dependency property.</summary>
    public static readonly DependencyProperty PlaybackRateProperty =
        DependencyProperty.Register(
            nameof(PlaybackRate), typeof(double), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(1.0d));

    /// <summary>The rate at which the animation plays. Defaults to 1.0.</summary>
    public double PlaybackRate
    {
        get => (double)GetValue(PlaybackRateProperty);
        set => SetValue(PlaybackRateProperty, value);
    }

    /// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(
            nameof(Duration), typeof(TimeSpan), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(TimeSpan.Zero));

    /// <summary>The duration of the loaded animation. Set by the source; treat as read-only.</summary>
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Identifies the <see cref="IsAnimatedVisualLoaded"/> dependency property.</summary>
    public static readonly DependencyProperty IsAnimatedVisualLoadedProperty =
        DependencyProperty.Register(
            nameof(IsAnimatedVisualLoaded), typeof(bool), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(false));

    /// <summary>Whether an animated visual is loaded. Set by the source; treat as read-only.</summary>
    public bool IsAnimatedVisualLoaded
    {
        get => (bool)GetValue(IsAnimatedVisualLoadedProperty);
        set => SetValue(IsAnimatedVisualLoadedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPlaying"/> dependency property.</summary>
    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying), typeof(bool), typeof(AnimatedVisualPlayer),
            new PropertyMetadata(false, OnIsPlayingChanged));

    /// <summary>Whether the animation is currently playing. Set by the source; treat as read-only.</summary>
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    #endregion

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var player = (AnimatedVisualPlayer)d;

        if (e.OldValue is IAnimatedVisualSource oldSource)
        {
            oldSource.Update(null);
        }

        player.SetRenderSurface(null);

        if (e.NewValue is IAnimatedVisualSource newSource)
        {
            newSource.Update(player);
        }
    }

    private static void OnLayoutAffectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var player = (AnimatedVisualPlayer)d;
        player.InvalidateMeasure();
        player.InvalidateArrange();
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var player = (AnimatedVisualPlayer)d;
        if (e.NewValue is false)
        {
            player._playCompletion?.TrySetResult();
            player._playCompletion = null;
        }
    }

    /// <summary>
    /// Called by the source to host (or clear, with <c>null</c>) its render surface.
    /// </summary>
    internal void SetRenderSurface(UIElement? surface)
    {
        Children.Clear();
        if (surface != null)
        {
            Children.Add(surface);
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var measuredSize = Source?.Measure(availableSize) ?? default;

        foreach (var child in Children)
        {
            child.Measure(measuredSize == default ? availableSize : measuredSize);
        }

        return measuredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }

        return finalSize;
    }

    #region Playback

    /// <summary>
    /// Starts playing the animation from <paramref name="fromProgress"/> to
    /// <paramref name="toProgress"/> (0..1). The returned task completes when playback
    /// stops (for a looped animation, that is when <see cref="Stop"/> is called).
    /// </summary>
    public Task PlayAsync(double fromProgress, double toProgress, bool looped)
    {
        if (Source is not { } source)
        {
            return Task.CompletedTask;
        }

        _playCompletion?.TrySetResult();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _playCompletion = completion;

        source.Play(fromProgress, toProgress, looped);

        return completion.Task;
    }

    /// <summary>Stops playback and releases any pending <see cref="PlayAsync"/> task.</summary>
    public void Stop() => Source?.Stop();

    /// <summary>Pauses playback at the current position.</summary>
    public void Pause() => Source?.Pause();

    /// <summary>Resumes playback after a <see cref="Pause"/>.</summary>
    public void Resume() => Source?.Resume();

    /// <summary>Moves the animation to the given progress value (0..1) without playing.</summary>
    public void SetProgress(double progress) => Source?.SetProgress(progress);

    #endregion
}
