using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.OpenGL;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.NativeElementHosting;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32; //Was previously: Uno.UI.Runtime.Skia.Win32

// Mostly a copy from WpfNativeOpenGLWrapper

internal class Win32NativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Type _type = typeof(Win32NativeOpenGLWrapper);
	private static readonly Lazy<IntPtr> _opengl32 = new Lazy<IntPtr>(() =>
	{
		if (!NativeLibrary.TryLoad("opengl32.dll", _type.Assembly, DllImportSearchPath.UserDirectories, out var _handle))
		{
			if (_type.Log().IsEnabled(LogLevel.Error))
			{
				_type.Log().Error("opengl32.dll was not loaded successfully.");
			}
		}
		return _handle;
	});

	// Dedicated context-host window class, registered lazily once per process (see the dedicated
	// fallback below). Kept alive for the process lifetime, like the clipboard helper window.
	private static readonly object _classGate = new();
	private static ushort _dedicatedClassAtom;
	private static Win32Helper.NativeNulTerminatedUtf16String? _dedicatedClassName;

	private readonly HDC _hdc;
	private readonly HGLRC _glContext;
	private readonly HWND _hwnd;
	// True when _hwnd is a private window this wrapper created (and must destroy on Dispose),
	// rather than the head's shared top-level window.
	private readonly bool _ownsWindow;

	public Win32NativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		if (XamlRootMap.GetHostForRoot(xamlRoot) is not Win32WindowWrapper wrapper)
		{
			throw new InvalidOperationException($"The XamlRoot and the XamlRootMap must be initialized before constructing a {_type.Name}.");
		}
		var sharedHwnd = (HWND)(wrapper.NativeWindow as Win32NativeWindow)!.Hwnd;

		// Primary path: share the head's top-level window. This is the path traditional desktop
		// OpenGL drivers (e.g. on x64) take and succeed on — re-using the window's already-set
		// pixel format on a fresh DC is fine there — so their behavior is unchanged.
		if (TryCreateContextOnWindow(sharedHwnd, out _hdc, out _glContext, out var sharedError))
		{
			_hwnd = sharedHwnd;
			_ownsWindow = false;
			return;
		}

		// Fallback: some OpenGL ICDs will NOT produce a context on the shared window's fresh DC —
		// notably Windows-on-ARM's "GLon12" mapping layer (Mesa over Direct3D 12, shipped by the
		// Microsoft OpenGL Compatibility Pack), where the window's pixel format neither carries over
		// to a fresh DC nor can be set a second time. Create a private, never-shown top-level window
		// that owns its own DC + pixel format and host the context there. The wrapper renders to an
		// off-screen framebuffer, so this window is only a context host; it is destroyed in Dispose
		// so it cannot outlive the app (no zombie process).
		if (_type.Log().IsEnabled(LogLevel.Information))
		{
			_type.Log().Info($"OpenGL context on the shared window was unavailable ({sharedError}); retrying on a dedicated context-host window.");
		}

		var dedicatedHwnd = CreateDedicatedWindow();
		if (TryCreateContextOnWindow(dedicatedHwnd, out _hdc, out _glContext, out var dedicatedError))
		{
			_hwnd = dedicatedHwnd;
			_ownsWindow = true;
			return;
		}

		// Neither path worked — destroy the dedicated window we just created and report; the caller
		// (OffscreenGLContext/GLCanvasElement/GameEngine) falls back to CPU/software rendering.
		if (!PInvoke.DestroyWindow(dedicatedHwnd))
		{
			this.LogError()?.Error($"{nameof(PInvoke.DestroyWindow)} failed for the dedicated GL window: {Win32Helper.GetErrorMessage()}");
		}
		throw new InvalidOperationException($"Could not create an OpenGL context on the shared window ({sharedError}) or a dedicated window ({dedicatedError}). Falling back to software rendering.");
	}

	// Runs the GetDC -> ChoosePixelFormat -> SetPixelFormat -> wglCreateContext sequence on the given
	// window. Returns true with the DC/context on success; on failure returns false with a reason and
	// leaves no DC leaked. Never throws, so the caller can try an alternative window.
	private static bool TryCreateContextOnWindow(HWND hwnd, out HDC hdc, out HGLRC glContext, out string? error)
	{
		hdc = default;
		glContext = default;
		error = null;

		var dc = PInvoke.GetDC(hwnd);
		if (dc == IntPtr.Zero)
		{
			error = $"{nameof(PInvoke.GetDC)} failed: {Win32Helper.GetErrorMessage()}";
			return false;
		}

		PIXELFORMATDESCRIPTOR pfd = new();
		pfd.nSize = (ushort)Marshal.SizeOf(pfd);
		pfd.nVersion = 1;
		pfd.dwFlags = PFD_FLAGS.PFD_DRAW_TO_WINDOW | PFD_FLAGS.PFD_SUPPORT_OPENGL | PFD_FLAGS.PFD_DOUBLEBUFFER;
		pfd.iPixelType = PFD_PIXEL_TYPE.PFD_TYPE_RGBA;
		pfd.cColorBits = 32;
		pfd.cRedBits = 8;
		pfd.cGreenBits = 8;
		pfd.cBlueBits = 8;
		pfd.cAlphaBits = 8;
		pfd.cDepthBits = 16;
		pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
		pfd.iLayerType = PFD_LAYER_TYPE.PFD_MAIN_PLANE;

		var pixelFormat = PInvoke.ChoosePixelFormat(dc, in pfd);
		if (pixelFormat == 0)
		{
			error = $"{nameof(PInvoke.ChoosePixelFormat)} failed: {Win32Helper.GetErrorMessage()}";
			ReleaseDc(hwnd, dc);
			return false;
		}

		if (PInvoke.SetPixelFormat(dc, pixelFormat, in pfd) == 0)
		{
			error = $"{nameof(PInvoke.SetPixelFormat)} failed: {Win32Helper.GetErrorMessage()}";
			ReleaseDc(hwnd, dc);
			return false;
		}

		var ctx = PInvoke.wglCreateContext(dc);
		if (ctx == IntPtr.Zero)
		{
			error = $"{nameof(PInvoke.wglCreateContext)} failed: {Win32Helper.GetErrorMessage()}";
			ReleaseDc(hwnd, dc);
			return false;
		}

		hdc = dc;
		glContext = ctx;
		return true;
	}

	private static void ReleaseDc(HWND hwnd, HDC hdc)
	{
		if (PInvoke.ReleaseDC(hwnd, hdc) != 1)
		{
			_type.LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	// Creates a never-shown top-level window to host an OpenGL context. It must be a real
	// (non-message-only) window so it has a device context that supports PFD_DRAW_TO_WINDOW.
	private static unsafe HWND CreateDedicatedWindow()
	{
		EnsureDedicatedWindowClassRegistered();

		var className = _dedicatedClassName!.Value; // set by EnsureDedicatedWindowClassRegistered
		using var windowTitle = new Win32Helper.NativeNulTerminatedUtf16String("");
		var hwnd = PInvoke.CreateWindowEx(
			0,
			className,
			windowTitle,
			WINDOW_STYLE.WS_OVERLAPPED, // never shown (no WS_VISIBLE, no ShowWindow) -> invisible, no taskbar
			0,
			0,
			8,
			8,
			HWND.Null, // top-level; NOT HWND_MESSAGE, which cannot host a GL-capable DC
			HMENU.Null,
			Win32Helper.GetHInstance(),
			null);

		if (hwnd == HWND.Null)
		{
			throw new InvalidOperationException($"{nameof(PInvoke.CreateWindowEx)} (dedicated GL window) failed: {Win32Helper.GetErrorMessage()}. Falling back to software rendering.");
		}

		return hwnd;
	}

	private static unsafe void EnsureDedicatedWindowClassRegistered()
	{
		if (_dedicatedClassAtom != 0)
		{
			return;
		}

		lock (_classGate)
		{
			if (_dedicatedClassAtom != 0)
			{
				return;
			}

			// Kept alive for the process lifetime (the class is never unregistered, like the
			// clipboard helper window's class).
			var className = new Win32Helper.NativeNulTerminatedUtf16String("CodeBrixPlatformOffscreenGLWindow");
			_dedicatedClassName = className;

			var windowClass = new WNDCLASSEXW
			{
				cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
				lpfnWndProc = &WndProc,
				hInstance = Win32Helper.GetHInstance(),
				lpszClassName = className,
			};

			var atom = PInvoke.RegisterClassEx(windowClass);
			if (atom == 0)
			{
				throw new InvalidOperationException($"{nameof(PInvoke.RegisterClassEx)} (dedicated GL window) failed: {Win32Helper.GetErrorMessage()}. Falling back to software rendering.");
			}

			_dedicatedClassAtom = atom;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
		=> PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	public bool TryGetProcAddress(string proc, out nint addr)
	{
		if (_opengl32.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_opengl32.Value, proc, out addr))
		{
			return true;
		}

		addr = PInvoke.wglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public nint GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var address))
		{
			return address;
		}

		throw new InvalidOperationException("No function was found with the name " + proc + ".");
	}

	public void Dispose()
	{
		var success = PInvoke.wglDeleteContext(_glContext);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.wglDeleteContext)} failed: {Win32Helper.GetErrorMessage()}"); }
		var success2 = PInvoke.ReleaseDC(_hwnd, _hdc) == 1;
		if (!success2) { this.LogError()?.Error($"{nameof(PInvoke.ReleaseDC)} failed: {Win32Helper.GetErrorMessage()}"); }

		// A dedicated context-host window we created must be destroyed here — on the same (UI) thread
		// that created it, which is where Dispose runs via the GLCanvasElement/GameEngine teardown —
		// so it never lingers after the app's window closes (which would leave a zombie process).
		if (_ownsWindow && _hwnd != HWND.Null)
		{
			if (!PInvoke.DestroyWindow(_hwnd))
			{
				this.LogError()?.Error($"{nameof(PInvoke.DestroyWindow)} failed for the dedicated GL window: {Win32Helper.GetErrorMessage()}");
			}
		}
	}

	public IDisposable MakeCurrent()
	{
		return new Win32Helper.WglCurrentContextDisposable(_hdc, _glContext);
	}
}
