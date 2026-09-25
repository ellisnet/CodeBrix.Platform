#nullable enable

using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Graphics;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IApplicationPlatform"/>.
/// </summary>
internal sealed class ApplicationSkiaPlatform : IApplicationPlatform
{
	/// <inheritdoc />
	public void RegisterExtensions()
	{
		ApiExtensibility.Register(typeof(SKCanvasVisualBaseFactory), _ => new SKCanvasVisualFactory());
	}
}
