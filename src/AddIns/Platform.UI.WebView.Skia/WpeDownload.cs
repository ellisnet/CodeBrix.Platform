using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// Wraps one WebKitDownload: keeps the GObject alive for the download's lifetime, listens to its
/// signals, and exposes the engine-side download flow. The destination decision is asynchronous -
/// the decide-destination handler returns TRUE without setting a destination (supported since
/// WebKit 2.40), and the download stays parked until <see cref="SetDestination"/> or
/// <see cref="Cancel"/> is called. All events are raised on the WPE thread; the public methods
/// can be called from any thread.
/// </summary>
internal sealed unsafe class WpeDownload
{
	private readonly IntPtr _download;
	private GCHandle _selfHandle;
	private volatile bool _cancelRequested;
	private bool _failureReported;
	private bool _finished;

	/// <summary>
	/// Raised (once) when the engine needs a destination decision. Metadata properties are
	/// populated when this fires. The consumer must eventually call <see cref="SetDestination"/>
	/// or <see cref="Cancel"/>; the download does not proceed until then.
	/// </summary>
	public event Action<string>? DestinationRequested;

	/// <summary>Raised as data arrives, with the total number of bytes received so far.</summary>
	public event Action<long>? ProgressChanged;

	/// <summary>Raised when the download completed successfully.</summary>
	public event Action? Completed;

	/// <summary>Raised when the download failed or was canceled (wasCanceled, isDestinationFailure, message).</summary>
	public event Action<bool, bool, string>? Failed;

	/// <summary>The URI the download is fetched from.</summary>
	public string Uri { get; private set; } = "";

	/// <summary>The MIME type of the response, or an empty string when unknown.</summary>
	public string MimeType { get; private set; } = "";

	/// <summary>The raw Content-Disposition response header, or an empty string when absent.</summary>
	public string ContentDisposition { get; private set; } = "";

	/// <summary>The expected total size in bytes per Content-Length, or 0 when unknown.</summary>
	public long TotalBytesToReceive { get; private set; }

	public WpeDownload(IntPtr download)
	{
		_download = download;
		GLibInterop.g_object_ref(download);
		_selfHandle = GCHandle.Alloc(this);

		ConnectSignal("decide-destination", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int>)&OnDecideDestinationNative);
		ConnectSignal("received-data", (delegate* unmanaged[Cdecl]<IntPtr, ulong, IntPtr, void>)&OnReceivedDataNative);
		ConnectSignal("finished", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&OnFinishedNative);
		ConnectSignal("failed", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&OnFailedNative);
	}

	private void ConnectSignal(string signal, void* handler)
		=> GLibInterop.g_signal_connect_data(_download, signal, (IntPtr)handler, GCHandle.ToIntPtr(_selfHandle), IntPtr.Zero, 0);

	/// <summary>
	/// Directs the parked download to write to <paramref name="absolutePath"/> (overwriting an
	/// existing file - the collision-free default name is chosen by the caller) and lets it proceed.
	/// </summary>
	public void SetDestination(string absolutePath) => WpeThread.Post(() =>
	{
		if (_finished)
		{
			return;
		}
		WebKitInterop.webkit_download_set_allow_overwrite(_download, 1);
		WebKitInterop.webkit_download_set_destination(_download, absolutePath);
	});

	/// <summary>Cancels the download (usable both before and after the destination decision).</summary>
	public void Cancel()
	{
		_cancelRequested = true;
		WpeThread.Post(() =>
		{
			if (!_finished)
			{
				WebKitInterop.webkit_download_cancel(_download);
			}
		});
	}

	private static WpeDownload? FromUserData(IntPtr userData)
		=> userData == IntPtr.Zero ? null : GCHandle.FromIntPtr(userData).Target as WpeDownload;

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int OnDecideDestinationNative(IntPtr download, IntPtr suggestedFilename, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return 0;
		}

		try
		{
			self.ReadResponseMetadata();
			var suggested = Marshal.PtrToStringUTF8(suggestedFilename) ?? "";
			self.DestinationRequested?.Invoke(suggested);
		}
		catch (Exception e)
		{
			typeof(WpeDownload).Log().Error("decide-destination handler failed; the download was canceled.", e);
			self.Cancel();
		}
		// TRUE: the destination will be provided asynchronously (or the download canceled).
		return 1;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnReceivedDataNative(IntPtr download, ulong dataLength, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			self.ProgressChanged?.Invoke((long)WebKitInterop.webkit_download_get_received_data_length(download));
		}
		catch (Exception e)
		{
			typeof(WpeDownload).Log().Error("received-data handler failed.", e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnFailedNative(IntPtr download, IntPtr error, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			// "failed" is always followed by "finished"; remember that the failure was
			// already surfaced so the finished handler does not also report success.
			self._failureReported = true;
			var code = GLibInterop.GetErrorCode(error);
			var wasCanceled = self._cancelRequested || code == WebKitInterop.DownloadErrorCancelledByUser;
			var isDestinationFailure = code == WebKitInterop.DownloadErrorDestination;
			var message = GLibInterop.GetErrorMessage(error) ?? "The download failed.";
			self.Failed?.Invoke(wasCanceled, isDestinationFailure, message);
		}
		catch (Exception e)
		{
			typeof(WpeDownload).Log().Error("download failed handler failed.", e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnFinishedNative(IntPtr download, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			self._finished = true;
			if (!self._failureReported)
			{
				self.Completed?.Invoke();
			}
		}
		catch (Exception e)
		{
			typeof(WpeDownload).Log().Error("download finished handler failed.", e);
		}
		finally
		{
			// "finished" is the download's last signal - release the GObject ref and the
			// managed handle that kept this wrapper reachable from the native callbacks.
			GLibInterop.g_object_unref(self._download);
			if (self._selfHandle.IsAllocated)
			{
				self._selfHandle.Free();
			}
		}
	}

	private void ReadResponseMetadata()
	{
		var response = WebKitInterop.webkit_download_get_response(_download);
		if (response != IntPtr.Zero)
		{
			Uri = Marshal.PtrToStringUTF8(WebKitInterop.webkit_uri_response_get_uri(response)) ?? "";
			MimeType = Marshal.PtrToStringUTF8(WebKitInterop.webkit_uri_response_get_mime_type(response)) ?? "";
			TotalBytesToReceive = (long)WebKitInterop.webkit_uri_response_get_content_length(response);

			var headers = WebKitInterop.webkit_uri_response_get_http_headers(response);
			if (headers != IntPtr.Zero)
			{
				ContentDisposition = Marshal.PtrToStringUTF8(WebKitInterop.soup_message_headers_get_one(headers, "Content-Disposition")) ?? "";
			}
		}
		else
		{
			var request = WebKitInterop.webkit_download_get_request(_download);
			if (request != IntPtr.Zero)
			{
				Uri = Marshal.PtrToStringUTF8(WebKitInterop.webkit_uri_request_get_uri(request)) ?? "";
			}
		}
	}
}
