#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The font source a platform supplies to the shared text engine (IFontSourcePlatform, WPE1 C5), resolving families
/// through Skia's own font manager: the text engine in CodeBrix.Platform.UI.TextLayout.Core then runs exactly as a
/// CodeBrix.Mobile app would run it, with no framework assembly in the process to supply the application's fonts.
/// </summary>
internal sealed class TestFontSourcePlatform : IFontSourcePlatform<SKTypeface>
{
	private static readonly object _gate = new();
	private static TestFontSourcePlatform? _instance;

	private readonly Dictionary<(string Family, ushort Weight, int Stretch, int Style), Task<SKTypeface?>> _cache = new();
	private int _typefaceRequests;

	/// <summary>Registers one font source for the process (as a platform bootstrap would) and returns it.</summary>
	/// <returns>The registered font source.</returns>
	internal static TestFontSourcePlatform EnsureRegistered()
	{
		lock (_gate)
		{
			if (_instance is null)
			{
				var source = new TestFontSourcePlatform();
				ApiExtensibility.Register(typeof(IFontSourcePlatform<SKTypeface>), _ => source);
				_instance = source;
			}

			return _instance;
		}
	}

	/// <summary>How many typeface requests the engine has made of this source.</summary>
	internal int TypefaceRequests => Volatile.Read(ref _typefaceRequests);

	public string DefaultTextFontFamily => "sans-serif";

	public string SymbolsFont => "sans-serif";

	public bool RestrictToEmbeddedFonts => false;

	public IReadOnlyList<string>? FallbackFontFamilies => null;

	public Task<SKTypeface?> GetTypefaceAsync(string familyName, ushort weight, int stretch, int style)
	{
		Interlocked.Increment(ref _typefaceRequests);
		lock (_cache)
		{
			var key = (familyName, weight, stretch, style);
			if (!_cache.TryGetValue(key, out var task))
			{
				var slant = style switch { 1 => SKFontStyleSlant.Oblique, 2 => SKFontStyleSlant.Italic, _ => SKFontStyleSlant.Upright };
				var width = stretch == 0 ? SKFontStyleWidth.Normal : (SKFontStyleWidth)stretch;
				_cache[key] = task = Task.FromResult<SKTypeface?>(
					SKTypeface.FromFamilyName(familyName, (SKFontStyleWeight)weight, width, slant));
			}

			return task;
		}
	}

	public SKTypeface? GetLoadedEmbeddedDefaultTypeface(ushort weight, int stretch, int style) => null;
}
