#nullable enable

using SkiaSharp;

namespace CodeBrix.Platform.UI.Rendering; //Was previously: Uno.UI.Rendering

internal interface IRenderer
{
	SKColor BackgroundColor { get; set; }
}
