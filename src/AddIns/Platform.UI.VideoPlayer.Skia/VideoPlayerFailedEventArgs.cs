using System;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia;

/// <summary>
/// Event args for <see cref="VideoPlayer.MediaFailed"/>.
/// </summary>
public sealed class VideoPlayerFailedEventArgs : EventArgs
{
	internal VideoPlayerFailedEventArgs(string message, Exception? error)
	{
		Message = message;
		Error = error;
	}

	/// <summary>
	/// A description of what failed, written to be shown to a person word for word. When the cause
	/// is a decoder the application has to register, this names the NuGet package and the call.
	/// </summary>
	public string Message { get; }

	/// <summary>The underlying exception, or null when the failure carried none.</summary>
	public Exception? Error { get; }
}
