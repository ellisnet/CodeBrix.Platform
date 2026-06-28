#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg; //Was previously: Uno.UI.Xaml.Media.Imaging.Svg

/// <summary>
/// This interface is used internally by CodeBrix Platform
/// to allow the installation of SVG Addin.
/// Avoid using this interface directly, as its signature
/// may change.
/// </summary>
public interface ISvgProvider
{
	UIElement GetCanvas();

	bool IsParsed { get; }

	Size SourceSize { get; }

	event EventHandler? SourceLoaded;

	Task<bool> TryLoadSvgDataAsync(byte[] imageData);

	/// <returns>An SKPicture on Skia, otherwise null.</returns>
	object? TryGetLoadedDataAsPictureAsync() => default;

	void Unload();
}
