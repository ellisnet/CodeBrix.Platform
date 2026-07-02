#nullable enable

// Ported from the CodeBrix.Platform (Uno-based) Lottie package:
// src/AddIns/Platform.UI.Lottie/LottieVisualSourceBase.cs
// Adapted for native WinUI 3 (Windows App SDK): same URI schemes, loading pipeline
// and measure logic; the player contract is the CodeBrix.Platform.WinUI.Lottie
// IAnimatedVisualSource interfaces instead of the Uno-fork internal ones.

using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CodeBrix.Platform.WinUI.Lottie;

/// <summary>
/// Base class for Lottie animated visual sources. Loads the animation JSON payload
/// from <see cref="UriSource"/> (embedded://, ms-appx:///, ms-appdata:///) and drives
/// the Skottie-based rendering hosted by an <see cref="AnimatedVisualPlayer"/>.
/// </summary>
public abstract partial class LottieVisualSourceBase : DependencyObject, IAnimatedVisualSource, IAnimatedVisualSourceWithUri
{
    /// <summary>Callback invoked when the (possibly re-processed) animation JSON is available.</summary>
    public delegate void UpdatedAnimation(string animationJson, string cacheKey);

    private static HttpClient? _httpClient;

    private AnimatedVisualPlayer? _player;

    /// <summary>Identifies the <see cref="UriSource"/> dependency property.</summary>
    public static DependencyProperty UriSourceProperty { get; } = DependencyProperty.Register(
        nameof(UriSource),
        typeof(Uri),
        typeof(LottieVisualSourceBase),
        new PropertyMetadata(default(Uri), OnUriSourceChanged));

    Uri? IAnimatedVisualSourceWithUri.UriSource { get => UriSource; set => UriSource = value; }

    /// <summary>The URI of the animation JSON payload.</summary>
    public Uri? UriSource
    {
        get => (Uri?)GetValue(UriSourceProperty);
        set => SetValue(UriSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="Options"/> dependency property.</summary>
    public static DependencyProperty OptionsProperty { get; } = DependencyProperty.Register(
        nameof(Options), typeof(LottieVisualOptions), typeof(LottieVisualSourceBase), new PropertyMetadata(LottieVisualOptions.None));

    /// <summary>
    /// Lottie translation options. Declared for API parity with the CodeBrix.Platform
    /// Lottie package, where it is also not implemented.
    /// </summary>
    public LottieVisualOptions Options
    {
        get => (LottieVisualOptions)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    /// <summary>
    /// Not implemented. Declared for API parity with the CodeBrix.Platform Lottie package,
    /// where it is also not implemented.
    /// </summary>
    public static LottieVisualSource CreateFromString(string uri)
    {
        throw new NotImplementedException();
    }

    private static void OnUriSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is LottieVisualSourceBase source)
        {
            source.Update(source._player);
        }
    }

    /// <summary>Sets <see cref="UriSource"/>.</summary>
    public Task SetSourceAsync(Uri sourceUri)
    {
        UriSource = sourceUri;

        // TODO: this method should not return before the animation is ready.
        // (Same behavior as the CodeBrix.Platform Lottie package.)

        return Task.CompletedTask;
    }

    private readonly SerialDisposable _updateDisposable = new SerialDisposable();

    /// <summary>
    /// Attaches this source to (or detaches it from, with <c>null</c>) the given player,
    /// triggering the load of the animation payload.
    /// </summary>
    public void Update(AnimatedVisualPlayer? player)
    {
        _updateDisposable.Disposable = null;

        _player = player;
        if (_player != null)
        {
            // Re-host the render surface if this source was previously attached
            // to a player and already has a loaded animation.
            AttachRenderSurface();

            var cts = new CancellationDisposable();
            _updateDisposable.Disposable = cts;
            _ = InnerUpdate(cts.Token);
        }
    }

    /// <summary>
    /// If the payload needs to be altered before being fed to the player
    /// </summary>
    protected abstract bool IsPayloadNeedsToBeUpdated { get; }

    /// <summary>
    /// Load the animation json payload
    /// </summary>
    protected virtual IDisposable? LoadAndObserveAnimationData(
        IInputStream sourceJson,
        string sourceCacheKey,
        UpdatedAnimation updateCallback)
    {
        var cts = new CancellationTokenSource();

        async Task Load(CancellationToken ct)
        {
            string json;
            using (var reader = new StreamReader(sourceJson.AsStreamForRead(0)))
            {
                json = await reader.ReadToEndAsync(ct);
            }

            // close the input stream
            sourceJson.Dispose();

            // load the stream (not dynamic: won't produce another version)
            updateCallback(json, sourceCacheKey);
        }

        _ = Load(cts.Token);

        return Disposable.Create(() =>
        {
            cts.Cancel();
            cts.Dispose();
        });
    }

    private void SetIsPlaying(bool isPlaying) => _player?.SetValue(AnimatedVisualPlayer.IsPlayingProperty, isPlaying);

    Size IAnimatedVisualSource.Measure(Size availableSize)
    {
        if (_player == null)
        {
            return default;
        }

        var compositionSize = CompositionSize;
        if (compositionSize == default)
        {
            return default;
        }

        var stretch = _player.Stretch;

        if (stretch == Microsoft.UI.Xaml.Media.Stretch.None)
        {
            return compositionSize;
        }

        var availableWidth = availableSize.Width;
        var availableHeight = availableSize.Height;

        var resultSize = availableSize;

        if (double.IsInfinity(availableWidth))
        {
            if (double.IsInfinity(availableHeight))
            {
                return compositionSize;
            }

            resultSize = new Size(availableHeight * compositionSize.Width / compositionSize.Height, availableHeight);
        }

        if (double.IsInfinity(availableHeight))
        {
            resultSize = new Size(availableWidth, availableWidth * compositionSize.Height / compositionSize.Width);
        }

        return resultSize;
    }

    private async Task<IInputStream?> TryLoadDownloadJson(Uri uri, CancellationToken ct)
    {
        if (TryLoadEmbeddedJson(uri) is { } json)
        {
            return json;
        }

        if (uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask(ct);
            var value = await file.OpenAsync(FileAccessMode.Read).AsTask(ct);

            return value;
        }
        else if (uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase))
        {
            var fileStream = File.OpenRead(AppDataUriToPath(uri));

            return fileStream.AsInputStream();
        }

        return IsPayloadNeedsToBeUpdated
            ? await DownloadJsonFromUri(uri, ct)
            : null;
    }

    private IInputStream? TryLoadEmbeddedJson(Uri uri)
    {
        if (uri.Scheme != "embedded")
        {
            return null;
        }

        var assemblyName = uri.Host;

        var assembly = assemblyName == "."
            ? Application.Current.GetType().Assembly
            : Assembly.Load(assemblyName);

        if (assembly == null)
        {
            return null;
        }

        var resourceName = uri.AbsolutePath.Substring(1).Replace("(assembly)", assembly.GetName().Name);
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Debug.WriteLine($"[LottieVisualSource] Unable to find embedded resource named '{resourceName}' to load.");
            return null;
        }

        return stream.AsInputStream();
    }

    private static string AppDataUriToPath(Uri uri)
    {
        // ms-appdata:///local/..., ms-appdata:///roaming/..., ms-appdata:///temp/...
        var path = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
        var separatorIndex = path.IndexOf('/');
        var folderName = separatorIndex < 0 ? path : path[..separatorIndex];
        var relativePath = separatorIndex < 0 ? string.Empty : path[(separatorIndex + 1)..];

        var basePath = folderName.ToLowerInvariant() switch
        {
            "local" => ApplicationData.Current.LocalFolder.Path,
            "roaming" => ApplicationData.Current.RoamingFolder.Path,
            "temp" => ApplicationData.Current.TemporaryFolder.Path,
            _ => throw new InvalidOperationException($"Unsupported ms-appdata folder in URI: {uri}"),
        };

        return Path.Combine(basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private async Task<IInputStream?> DownloadJsonFromUri(Uri uri, CancellationToken ct)
    {
        _httpClient ??= new HttpClient();

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.Content.Headers.ContentLength is { } length && length < 2)
        {
            return null;
        }

        var stream = await response.Content.ReadAsStreamAsync(ct);

        return stream.AsInputStream();
    }
}
