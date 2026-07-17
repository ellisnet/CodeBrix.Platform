#nullable enable

using System;
using System.Globalization;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a single download and reports its progress and state. Instances are provided
/// through <see cref="CoreWebView2DownloadStartingEventArgs.DownloadOperation"/>. All members
/// must be accessed on the UI thread.
/// </summary>
public partial class CoreWebView2DownloadOperation
{
	private readonly Action? _cancelRequested;
	private readonly DateTimeOffset _startTimeUtc = DateTimeOffset.UtcNow;
	private string _estimatedEndTime = "";

	internal CoreWebView2DownloadOperation(
		string uri,
		string contentDisposition,
		string mimeType,
		long totalBytesToReceive,
		string resultFilePath,
		Action? cancelRequested)
	{
		Uri = uri;
		ContentDisposition = contentDisposition;
		MimeType = mimeType;
		TotalBytesToReceive = totalBytesToReceive;
		ResultFilePath = resultFilePath;
		_cancelRequested = cancelRequested;
		State = CoreWebView2DownloadState.InProgress;
		InterruptReason = CoreWebView2DownloadInterruptReason.None;
	}

	/// <summary>
	/// Gets the URI of the download.
	/// </summary>
	public string Uri { get; }

	/// <summary>
	/// Gets the Content-Disposition header value from the download's HTTP response, or an empty
	/// string when the response carried none.
	/// </summary>
	public string ContentDisposition { get; }

	/// <summary>
	/// Gets the MIME type of the downloaded content.
	/// </summary>
	public string MimeType { get; }

	/// <summary>
	/// Gets the expected size of the download in total number of bytes, based on the HTTP
	/// Content-Length header. Zero when the size is unknown.
	/// </summary>
	public long TotalBytesToReceive { get; private set; }

	/// <summary>
	/// Gets the number of bytes that have been written to the download file.
	/// </summary>
	public long BytesReceived { get; private set; }

	/// <summary>
	/// Gets the estimated end time of the download as an RFC 3339 UTC timestamp, or an empty
	/// string while no estimate is available (for example before any data has arrived, or when
	/// the total size is unknown).
	/// </summary>
	public string EstimatedEndTime => _estimatedEndTime;

	/// <summary>
	/// Gets the absolute path of the file the download is written to, including the file name.
	/// The path is decided when the download starts and can be overridden through
	/// <see cref="CoreWebView2DownloadStartingEventArgs.ResultFilePath"/>.
	/// </summary>
	public string ResultFilePath { get; private set; }

	/// <summary>
	/// Gets the state of the download; the <see cref="StateChanged"/> event is raised when
	/// this value changes.
	/// </summary>
	public CoreWebView2DownloadState State { get; private set; }

	/// <summary>
	/// Gets the reason the download was interrupted; <see cref="CoreWebView2DownloadInterruptReason.None"/>
	/// while the download is in progress or after it completed successfully.
	/// </summary>
	public CoreWebView2DownloadInterruptReason InterruptReason { get; private set; }

	/// <summary>
	/// Always false on the CodeBrix.Platform Skia heads: interrupted downloads cannot be resumed
	/// (pausing and resuming downloads is not supported - see Pause and Resume).
	/// </summary>
	public bool CanResume => false;

	/// <summary>
	/// Raised when the <see cref="BytesReceived"/> value changes.
	/// </summary>
	public event TypedEventHandler<CoreWebView2DownloadOperation, object>? BytesReceivedChanged;

	/// <summary>
	/// Raised when the <see cref="EstimatedEndTime"/> value changes.
	/// </summary>
	public event TypedEventHandler<CoreWebView2DownloadOperation, object>? EstimatedEndTimeChanged;

	/// <summary>
	/// Raised when the <see cref="State"/> value changes.
	/// </summary>
	public event TypedEventHandler<CoreWebView2DownloadOperation, object>? StateChanged;

	/// <summary>
	/// Cancels the download. The <see cref="State"/> transitions to
	/// <see cref="CoreWebView2DownloadState.Interrupted"/> with <see cref="InterruptReason"/>
	/// set to <see cref="CoreWebView2DownloadInterruptReason.UserCanceled"/>, and the partially
	/// downloaded file is removed.
	/// </summary>
	public void Cancel() => _cancelRequested?.Invoke();

	internal void SetResultFilePath(string resultFilePath) => ResultFilePath = resultFilePath;

	internal void ReportProgress(long bytesReceived, long? totalBytesToReceive = null)
	{
		if (totalBytesToReceive is { } total && total != TotalBytesToReceive)
		{
			TotalBytesToReceive = total;
		}

		if (bytesReceived != BytesReceived)
		{
			BytesReceived = bytesReceived;
			BytesReceivedChanged?.Invoke(this, this);
		}

		UpdateEstimatedEndTime();
	}

	internal void ReportStateChanged(CoreWebView2DownloadState state, CoreWebView2DownloadInterruptReason interruptReason = CoreWebView2DownloadInterruptReason.None)
	{
		if (State == state && InterruptReason == interruptReason)
		{
			return;
		}

		State = state;
		InterruptReason = interruptReason;
		StateChanged?.Invoke(this, this);
	}

	private void UpdateEstimatedEndTime()
	{
		var newEstimate = "";
		var elapsed = DateTimeOffset.UtcNow - _startTimeUtc;
		if (TotalBytesToReceive > 0 && BytesReceived > 0 && elapsed > TimeSpan.Zero)
		{
			var remainingBytes = Math.Max(0, TotalBytesToReceive - BytesReceived);
			var secondsRemaining = remainingBytes * elapsed.TotalSeconds / BytesReceived;
			newEstimate = DateTimeOffset.UtcNow.AddSeconds(secondsRemaining)
				.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
		}

		if (newEstimate != _estimatedEndTime)
		{
			_estimatedEndTime = newEstimate;
			EstimatedEndTimeChanged?.Invoke(this, this);
		}
	}
}
