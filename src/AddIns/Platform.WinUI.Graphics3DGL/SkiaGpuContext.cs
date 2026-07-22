#if !WINAPPSDK && !WINDOWS_UWP
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;
using Microsoft.UI.Xaml;
using SkiaSharp;

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

/// <summary>
/// A <b>backend-neutral, GPU-accelerated Skia context</b> for off-screen rendering. It hands back a
/// <see cref="GRContext"/> plus a "frame scope" in which GPU work on it is valid, <b>without the
/// caller knowing which graphics API is underneath</b>: on macOS it resolves the head's
/// Skia-on-Metal provider; on every other head it falls back to an off-screen OpenGL context
/// (<see cref="OffscreenGLContext"/>). Where no GPU context can be built, <see cref="TryCreate"/>
/// returns <see langword="false"/> so the caller keeps its CPU-rendering fallback.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam that lets a consumer — for example the CodeBrix.Platform.GameEngine GPU render
/// tier — get GPU-accelerated Skia on every head that can provide it (OpenGL/GLES on Win32, WPF,
/// X11, Wayland, Frame Buffer; Metal on macOS) from one code path. The <see cref="Backend"/>
/// property reports which API was actually used, for diagnostics only.
/// </para>
/// <para>
/// Keep the returned <see cref="SkiaGpuContext"/> and its <see cref="GrContext"/> on the thread they
/// were created on, and wrap each frame's GPU work in a single <c>using</c> of
/// <see cref="BeginFrame"/>. On a GL head that scope makes the off-screen context current and
/// restores the previous one; on Metal it is a no-op (Metal has no thread-current context).
/// </para>
/// </remarks>
public sealed class SkiaGpuContext : IDisposable
{
	private readonly INativeSkiaGpuContext? _native;
	private readonly OffscreenGLContext? _glContext;
	private bool _disposed;

	private SkiaGpuContext(GRContext grContext, SkiaGpuBackend backend, INativeSkiaGpuContext? native, OffscreenGLContext? glContext)
	{
		GrContext = grContext;
		Backend = backend;
		_native = native;
		_glContext = glContext;
	}

	/// <summary>The GPU <see cref="GRContext"/>. Only issue GPU work on it inside a <see cref="BeginFrame"/> scope.</summary>
	public GRContext GrContext { get; }

	/// <summary>The graphics backend behind <see cref="GrContext"/> (informational/diagnostic).</summary>
	public SkiaGpuBackend Backend { get; }

	/// <summary>
	/// Tries to create a backend-neutral GPU Skia context for the given XAML root.
	/// </summary>
	/// <param name="xamlRoot">The XAML root of the window the context is associated with.</param>
	/// <param name="context">
	/// On success, the created context; otherwise <see langword="null"/> (for example when the running
	/// head provides no GPU context, or macOS is in software-rendering mode).
	/// </param>
	/// <returns><see langword="true"/> when a GPU context was created; otherwise <see langword="false"/>.</returns>
	public static bool TryCreate(XamlRoot xamlRoot, [NotNullWhen(true)] out SkiaGpuContext? context)
	{
		context = null;
		if (xamlRoot is null)
		{
			return false;
		}

		// 1. A head-provided GPU context (macOS registers a Skia-on-Metal one). A head that registers a
		//    provider OWNS the GPU decision, so a failure here does NOT fall through to the OpenGL path —
		//    it means "use CPU rendering" (for example macOS in software mode, where CreateGrContext throws).
		if (ApiExtensibility.CreateInstance<INativeSkiaGpuContext>(xamlRoot, out var native) && native is not null)
		{
			try
			{
				var gr = native.CreateGrContext();
				context = new SkiaGpuContext(gr, native.Backend, native, glContext: null);
				return true;
			}
			catch
			{
				native.Dispose();
				return false;
			}
		}

		// 2. Fallback: an off-screen OpenGL context, which every head that provides native OpenGL registers.
		if (OffscreenGLContext.TryCreate(xamlRoot, out var glContext))
		{
			try
			{
				var gr = glContext.CreateGrContext();
				context = new SkiaGpuContext(gr, SkiaGpuBackend.OpenGL, native: null, glContext);
				return true;
			}
			catch
			{
				glContext.Dispose();
				return false;
			}
		}

		return false;
	}

	/// <summary>
	/// Enters a scope in which GPU work on <see cref="GrContext"/> is valid, returning a disposable
	/// that closes it. On a GL head this makes the off-screen context current and restores the
	/// previously-current one on dispose; on Metal it is a no-op scope. Wrap each frame's GPU work in a
	/// single <c>using</c> of the returned value.
	/// </summary>
	public IDisposable BeginFrame() => _native?.BeginFrame() ?? _glContext!.MakeCurrent();

	/// <summary>Disposes the <see cref="GRContext"/> (inside a frame scope) and the underlying context.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		// Dispose the GRContext inside a frame scope (GL requires the context current for teardown;
		// Metal's scope is a no-op), then release the underlying provider/context afterwards.
		using (BeginFrame())
		{
			GrContext.Dispose();
		}

		_native?.Dispose();
		_glContext?.Dispose();
	}
}
#endif
