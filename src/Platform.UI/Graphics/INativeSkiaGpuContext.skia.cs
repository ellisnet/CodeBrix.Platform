#nullable enable

using System;
using SkiaSharp;

namespace CodeBrix.Platform.Graphics; //Was previously: Uno.Graphics

/// <summary>
/// A per-head provider of a GPU-accelerated Skia <see cref="GRContext"/>, resolved through
/// <c>ApiExtensibility</c> exactly like <see cref="INativeOpenGLWrapper"/>. A head registers one
/// when it can build a <see cref="GRContext"/> through a graphics API <b>other than</b> the generic
/// off-screen-OpenGL path — today only the macOS head, which supplies Skia-on-Metal. Heads that
/// provide no such context register nothing, and the backend-neutral <c>SkiaGpuContext</c> facade
/// (in the Graphics3DGL add-in) falls back to the off-screen OpenGL path.
/// </summary>
internal interface INativeSkiaGpuContext : IDisposable
{
	/// <summary>The backend this provider builds.</summary>
	SkiaGpuBackend Backend { get; }

	/// <summary>
	/// Builds the GPU <see cref="GRContext"/>. May throw; the facade treats any throw as
	/// "GPU unavailable on this head" and lets the caller fall back to CPU rendering (for example on
	/// macOS in software-rendering mode).
	/// </summary>
	GRContext CreateGrContext();

	/// <summary>
	/// Returns a scope in which GPU work on the <see cref="GRContext"/> is valid. Metal has no
	/// thread-current concept, so its scope is a no-op; a GL-based provider would make its context
	/// current and restore the previous one on dispose.
	/// </summary>
	IDisposable BeginFrame();
}
