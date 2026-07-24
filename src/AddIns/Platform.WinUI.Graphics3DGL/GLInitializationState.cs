using System;

namespace CodeBrix.Platform.WinUI.Graphics3DGL;

/// <summary>
/// The OpenGL initialization status of a <see cref="GLCanvasElement"/>.
/// </summary>
public enum GLInitializationStatus
{
	/// <summary>
	/// The element has not attempted OpenGL initialization yet. This is the state before the
	/// element is first loaded into the visual tree, and the state it returns to when unloaded.
	/// </summary>
	NotYetInitialized = 0,

	/// <summary>
	/// The element is currently attempting to create/acquire its OpenGL context. Because
	/// initialization happens synchronously on the UI thread during the Loaded event, callers
	/// on the UI thread will rarely observe this value.
	/// </summary>
	Initializing = 1,

	/// <summary>
	/// The OpenGL context was created successfully and the element is able to render.
	/// Equivalent to <see cref="GLCanvasElement.IsGLInitialized"/> being <see langword="true"/>.
	/// </summary>
	Initialized = 2,

	/// <summary>
	/// OpenGL initialization failed — the element will render nothing. See
	/// <see cref="GLInitializationState.FailedReason"/> for the reason. Equivalent to
	/// <see cref="GLCanvasElement.IsGLInitialized"/> being <see langword="false"/>.
	/// </summary>
	InitializationFailed = 3,
}

/// <summary>
/// A snapshot of a <see cref="GLCanvasElement"/>'s OpenGL initialization state, as returned by
/// <see cref="GLCanvasElement.GetGLInitializationState"/>. When initialization has failed, it
/// carries a human-readable reason (e.g. "no OpenGL support on this platform" or "the OpenGL
/// version found is below the required minimum") that an application can surface to the user
/// instead of showing an empty canvas.
/// </summary>
public sealed class GLInitializationState
{
	/// <summary>The initialization status of the element.</summary>
	public GLInitializationStatus Status { get; }

	/// <summary>
	/// A human-readable description of why OpenGL initialization failed. Non-null if and only
	/// if <see cref="Status"/> is <see cref="GLInitializationStatus.InitializationFailed"/>.
	/// </summary>
	public string? FailedReason { get; }

	internal GLInitializationState(GLInitializationStatus status, string? failedReason = null)
	{
		if ((status == GLInitializationStatus.InitializationFailed) == string.IsNullOrWhiteSpace(failedReason))
		{
			throw new ArgumentException(
				$"A {nameof(FailedReason)} must be provided when (and only when) the status is {nameof(GLInitializationStatus.InitializationFailed)}.",
				nameof(failedReason));
		}

		Status = status;
		FailedReason = failedReason;
	}
}
