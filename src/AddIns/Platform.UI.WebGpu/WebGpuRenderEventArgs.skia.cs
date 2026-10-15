using System;

namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// Event args for <see cref="WebGpuRenderView.Render"/>. The view reuses ONE instance for every frame: read
/// <see cref="Frame"/> inside the handler and keep nothing.
/// </summary>
public sealed class WebGpuRenderEventArgs : EventArgs
{
	internal WebGpuRenderEventArgs(WebGpuFrame frame)
	{
		Frame = frame;
	}

	/// <summary>The open frame to record into.</summary>
	public WebGpuFrame Frame { get; }
}
