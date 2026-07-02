using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// The single dedicated thread that hosts the WPE WebKit engine for this process. It owns a
/// private GLib main context (pushed thread-default before any engine initialization, so both
/// WPEBackend-fdo's host source and WebKit's run loop bind to it) and runs its main loop
/// forever. Every WebKit/WPE call in this AddIn must happen on this thread; use
/// <see cref="Post"/> to marshal onto it.
/// </summary>
internal static class WpeThread
{
	private static readonly object _gate = new();
	private static bool _started;
	private static Exception? _initException;
	private static IntPtr _context;
	[ThreadStatic] private static bool _isWpeThread;

	/// <summary>
	/// Starts the engine thread on first use. Returns null on success, otherwise the exception
	/// the WebView should surface (missing system libraries, engine init failure).
	/// </summary>
	public static Exception? EnsureStarted()
	{
		lock (_gate)
		{
			if (_started)
			{
				return _initException;
			}
			_started = true;

			_initException = NativeLibraries.Probe();
			if (_initException is not null)
			{
				typeof(WpeThread).Log().Error("WPE WebKit engine libraries are missing.", _initException);
				return _initException;
			}

			using var ready = new ManualResetEventSlim(false);
			Exception? startupError = null;

			var thread = new Thread(() =>
			{
				IntPtr loop = IntPtr.Zero;
				try
				{
					_isWpeThread = true;
					_context = GLibInterop.g_main_context_new();
					GLibInterop.g_main_context_push_thread_default(_context);

					// Debian ships no libWPEBackend-default.so: skipping the explicit loader
					// init would abort the whole process the moment WebKit touches libwpe.
					if (!LibWpe.wpe_loader_init(NativeLibraries.WpeBackendFdo))
					{
						throw new InvalidOperationException($"wpe_loader_init failed for {NativeLibraries.WpeBackendFdo}.");
					}

					// SHM mode permanently selects CPU-readable frame export for this process.
					if (!WpeBackendFdo.wpe_fdo_initialize_shm())
					{
						throw new InvalidOperationException("wpe_fdo_initialize_shm failed.");
					}

					loop = GLibInterop.g_main_loop_new(_context, 0);
				}
				catch (Exception e)
				{
					startupError = e;
					return;
				}
				finally
				{
					ready.Set();
				}

				GLibInterop.g_main_loop_run(loop); // runs for the process lifetime
			})
			{
				IsBackground = true,
				Name = "WPE WebKit thread",
			};

			thread.Start();
			ready.Wait();

			if (startupError is not null)
			{
				_initException = startupError;
				typeof(WpeThread).Log().Error("Failed to initialize the WPE WebKit engine.", startupError);
			}

			return _initException;
		}
	}

	/// <summary>Runs <paramref name="action"/> on the WPE thread (immediately when already on it).</summary>
	public static unsafe void Post(Action action)
	{
		if (_isWpeThread)
		{
			action();
			return;
		}

		var handle = GCHandle.Alloc(action);
		GLibInterop.g_main_context_invoke_full(_context, GLibInterop.GPriorityDefault, &InvokeTrampoline, GCHandle.ToIntPtr(handle), IntPtr.Zero);
	}

	/// <summary>Runs <paramref name="func"/> on the WPE thread and completes the returned task with its result.</summary>
	public static Task<T> InvokeAsync<T>(Func<T> func)
	{
		var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		Post(() =>
		{
			try
			{
				tcs.SetResult(func());
			}
			catch (Exception e)
			{
				tcs.SetException(e);
			}
		});
		return tcs.Task;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
	private static int InvokeTrampoline(IntPtr data)
	{
		var handle = GCHandle.FromIntPtr(data);
		try
		{
			((Action)handle.Target!).Invoke();
		}
		catch (Exception e)
		{
			// Exceptions must never propagate into native GLib dispatch.
			typeof(WpeThread).Log().Error("Unhandled exception on the WPE WebKit thread.", e);
		}
		finally
		{
			handle.Free();
		}
		return GLibInterop.GSourceRemove;
	}
}
