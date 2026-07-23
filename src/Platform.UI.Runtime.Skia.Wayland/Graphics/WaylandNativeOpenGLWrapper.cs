using System;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Offscreen EGL <see cref="INativeOpenGLWrapper"/> backing GLCanvasElement on the Wayland head.
/// The context is created with eglGetPlatformDisplay(EGL_PLATFORM_WAYLAND_KHR, wl_display*)
/// and renders into an offscreen pbuffer — or, on EGL implementations with no pbuffer-capable
/// configs (e.g. Mesa's Wayland platform on Raspberry Pi V3D), a surfaceless context
/// (EGL_KHR_surfaceless_context) made current with EGL_NO_SURFACE. Nothing is ever attached
/// to a wl_surface, so it works no matter which presentation backend the head is using (the
/// default Vulkan renderer, the opt-in EGL renderer, or the wl_shm software renderer) —
/// GLCanvasElement composites via CPU readback, never through the window's swapchain.
/// </summary>
internal sealed class WaylandNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const int EGL_PLATFORM_WAYLAND_KHR = 0x31D8;

	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface;

	public WaylandNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		if (XamlRootMap.GetHostForRoot(xamlRoot) is not WaylandXamlRootHost host || host.Connection is not { } connection)
		{
			throw new InvalidOperationException($"The XamlRoot and its XamlRootHost must be initialized on the element before constructing a {nameof(WaylandNativeOpenGLWrapper)}.");
		}

		_eglDisplay = EglHelper.EglGetPlatformDisplay(EGL_PLATFORM_WAYLAND_KHR, connection.Display.Handle, null);
		if (_eglDisplay == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(EglHelper.EglGetPlatformDisplay)} failed for the Wayland display: {Enum.GetName(EglHelper.EglGetError())}");
		}

		(_pBufferSurface, _glContext, var major, var minor, _, _) = EglHelper.InitializeGles2Context(_eglDisplay);
		this.LogInfo()?.Info($"Created a {nameof(WaylandNativeOpenGLWrapper)} instance using EGL {major}.{minor}.");
	}

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"A procedure named {proc} was not found in libEGL");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = EglHelper.EglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = EglHelper.EglGetCurrentContext();
		var display = EglHelper.EglGetCurrentDisplay();
		var readSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_READ);
		var drawSurface = EglHelper.EglGetCurrentSurface(EglHelper.EGL_DRAW);
		if (!EglHelper.EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!EglHelper.EglMakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglHelper.EglMakeCurrent)} failed.");
				}
			}
		});
	}

	public void Dispose()
	{
		// Deliberately no EglTerminate: the EGLDisplay for this wl_display is shared with any
		// other EGL user in the process (e.g. the opt-in WaylandEglRenderer).
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			EglHelper.EglDestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			EglHelper.EglDestroyContext(_eglDisplay, _glContext);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
	}
}
