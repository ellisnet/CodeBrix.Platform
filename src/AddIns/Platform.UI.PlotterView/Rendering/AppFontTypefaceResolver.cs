#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.UI.PlotterView.Contracts;
using SkiaSharp;

namespace CodeBrix.Platform.UI.PlotterView.Rendering;

/// <summary>
/// Resolves the font families a <see cref="CodeBrix.Plotter.PlotModel"/> names into typefaces
/// from the application's OWN fonts, through the same typeface cache the rest of the
/// framework's text uses: the platform's font source
/// (<see cref="IFontSourcePlatform{TTypeface}"/>, the Foundation.Core contract the shared text
/// engine resolves its typefaces through; WPE1 C7 - no TextLayout dependency). Never the
/// host's fonts: a family name that is not an application font URI - including the plot
/// library's built-in "Segoe UI" default - resolves to the control's plot font instead, which
/// itself defaults to the application's default font
/// (<see cref="IFontSourcePlatform{TTypeface}.DefaultTextFontFamily"/>). Part of the chart
/// engine (Engine/PlotHost): it names no XAML type.
/// </summary>
/// <remarks>
/// Font loads are asynchronous. A family still loading resolves to the framework's interim
/// typeface for now, and <see cref="FontLoaded"/> is raised (on an arbitrary thread) once the
/// real face is ready - the control then clears the render context's typeface cache and
/// repaints, the same fallback-then-swap behavior every TextBlock has. Resolved typefaces are
/// owned by the framework's font cache and must never be disposed, which matches the
/// CodeBrix.Plotter TypefaceResolver ownership contract exactly.
/// </remarks>
internal sealed class AppFontTypefaceResolver
{
    //FontStretch.Normal and FontStyle.Normal (the values the text engine passes for them): the chart asks for upright,
    //  normal-width faces only, as it always did.
    private const int NormalStretch = 5;
    private const int NormalStyle = 0;

    private static IFontSourcePlatform<SKTypeface>? _source;

    private readonly Dictionary<(string Family, ushort Weight), SKTypeface> _resolved = new();
    private readonly HashSet<(string Family, ushort Weight)> _pendingLoads = new();

    /// <summary>
    /// The font family (an application font URI such as
    /// <c>ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/Roboto.ttf</c>) that every
    /// non-URI family name resolves to. Null means the application's default font.
    /// </summary>
    public string? PlotFontFamily { get; set; }

    /// <summary>
    /// Raised when an asynchronous font load completes, possibly on a non-UI thread. The
    /// subscriber marshals to the UI thread, clears the render context's typeface cache and
    /// invalidates the plot.
    /// </summary>
    public event Action? FontLoaded;

    /// <summary>
    /// Resolves a font family and weight to a typeface. This is the delegate handed to
    /// <c>SkiaRenderContext.TypefaceResolver</c>; it runs on the UI thread only.
    /// </summary>
    /// <param name="fontFamily">The font family the plot model asked for.</param>
    /// <param name="fontWeight">The numeric font weight (400 normal, 700 bold).</param>
    /// <returns>The resolved typeface; never null.</returns>
    public SKTypeface Resolve(string fontFamily, double fontWeight)
    {
        var family = SelectFamily(fontFamily);
        var weightValue = (ushort)Math.Clamp(
            double.IsNaN(fontWeight) ? 400.0 : fontWeight, 1.0, 999.0);
        var key = (family, weightValue);

        if (_resolved.TryGetValue(key, out var cached))
        {
            return cached;
        }

        //Resolved through the platform's font source; pendingLoad is null when the family is already loaded.
        var typeface = GetTypeface(family, weightValue, out var pendingLoad);

        if (pendingLoad is null)
        {
            _resolved[key] = typeface;
            return typeface;
        }

        //Still loading: hand back the framework's interim face WITHOUT caching it here, and
        //  arrange a single repaint for when the real one is ready (the render context caches
        //  the interim face until the control clears it in response to FontLoaded).
        if (_pendingLoads.Add(key))
        {
            pendingLoad.ContinueWith(
                _ => FontLoaded?.Invoke(),
                TaskScheduler.Default);
        }

        return typeface;
    }

    /// <summary>
    /// Forgets every resolution, so families re-resolve on the next paint. Called on the UI
    /// thread when the plot font changes or a pending load completes.
    /// </summary>
    public void Reset()
    {
        _resolved.Clear();
        _pendingLoads.Clear();
    }

    /// <summary>The platform's font source, resolved once for the process (Contracts/PlatformContract.ResolveFontSource).</summary>
    private static IFontSourcePlatform<SKTypeface> Source => _source ??= PlatformContract.ResolveFontSource();

    //The text engine's typeface selection (FontDetailsCache.GetFont), minus the sized font the chart never used: the
    //  font source's typeface for the family, or - while it is still loading, or when it cannot be found - the engine's
    //  interim face (the loaded embedded default, else the default family, else the host default). pendingLoad is the
    //  load to wait for, null when the family's own typeface (or its final fallback) is in hand.
    private static SKTypeface GetTypeface(string fontFamily, ushort weight, out Task? pendingLoad)
    {
        var source = Source;
        if (string.Equals(fontFamily, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
        {
            fontFamily = source.DefaultTextFontFamily;
        }

        var typefaceTask = source.GetTypefaceAsync(fontFamily, weight, NormalStretch, NormalStyle);
        var canChange = !typefaceTask.IsCompleted; //read once: it could complete in between
        var typeface = !canChange ? typefaceTask.Result : null;
        pendingLoad = canChange ? typefaceTask : null;

        return typeface
            ?? source.GetLoadedEmbeddedDefaultTypeface(weight, NormalStretch, NormalStyle)
            ?? SKTypeface.FromFamilyName(source.DefaultTextFontFamily, (SKFontStyleWeight)weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.FromFamilyName(null, (SKFontStyleWeight)weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.FromFamilyName(null);
    }

    private string SelectFamily(string fontFamily)
    {
        //An application font URI passes through as-is; any bare family name (or nothing)
        //  becomes the plot font. Uri.TryCreate mirrors the framework's own test for
        //  "this is a loadable font source" in FontDetailsCache.GetFontInternal.
        if (!string.IsNullOrEmpty(fontFamily) && Uri.TryCreate(fontFamily, UriKind.Absolute, out _))
        {
            return fontFamily;
        }

        return PlotFontFamily ?? Source.DefaultTextFontFamily;
    }
}
