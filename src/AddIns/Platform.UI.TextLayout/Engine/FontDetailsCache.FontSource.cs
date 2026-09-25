#nullable enable

using CodeBrix.Platform.Foundation.Contracts;
using CodeBrix.Platform.UI.TextLayout.Contracts;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

// This assembly's part of the shared text engine's font cache (WPE1 C5; the engine source is link-compiled from
// src/Platform.UI/UI/Xaml/Documents, see the csproj). The framework's copy uses its own font source directly; this copy
// resolves the platform's registered one, once, so both copies share one typeface cache and measure identically.
internal static partial class FontDetailsCache
{
	private static partial IFontSourcePlatform<SKTypeface> ResolveFontSource() =>
		PlatformContract.Resolve<IFontSourcePlatform<SKTypeface>>();
}
