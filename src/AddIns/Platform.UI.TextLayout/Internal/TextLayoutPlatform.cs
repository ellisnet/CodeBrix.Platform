#nullable enable

using CodeBrix.Platform.UI.TextLayout.Contracts;
using CodeBrix.Platform.UI.TextLayout.Engine;

namespace CodeBrix.Platform.UI.TextLayout.Internal;

/// <summary>
/// The add-in's text engine (<see cref="ITextLayoutPlatform"/>): this assembly's own copy of the shared text engine
/// (WPE1 C5), so there is nothing to resolve from the registry; only the engine's font source is platform-provided.
/// </summary>
internal static class TextLayoutPlatform
{
	/// <summary>The text engine.</summary>
	internal static ITextLayoutPlatform Engine => TextLayoutEnginePlatform.Instance;
}
