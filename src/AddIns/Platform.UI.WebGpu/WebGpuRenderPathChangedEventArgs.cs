using System;

namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// Event args for <c>WebGpuRenderView.RenderPathChanged</c>: the render path before and after the change.
/// </summary>
public sealed class WebGpuRenderPathChangedEventArgs : EventArgs
{
	/// <summary>Creates the args.</summary>
	/// <param name="oldPath">The path that was running.</param>
	/// <param name="newPath">The path that is running now.</param>
	public WebGpuRenderPathChangedEventArgs(WebGpuRenderPath oldPath, WebGpuRenderPath newPath)
	{
		OldPath = oldPath;
		NewPath = newPath;
	}

	/// <summary>The path that was running.</summary>
	public WebGpuRenderPath OldPath { get; }

	/// <summary>The path that is running now.</summary>
	public WebGpuRenderPath NewPath { get; }
}
