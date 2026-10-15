#nullable enable

namespace CodeBrix.Platform.Graphics;

/// <summary>
/// A per-launch gate for add-in elements that create their OWN GPU device for an API the head does not render
/// with (for example a WebGPU device), resolved through <c>ApiExtensibility</c> from the element's <c>XamlRoot</c>
/// before the element creates its device. The desktop heads register nothing - the element simply proceeds. A
/// test head registers one to decide, per launch, whether such elements get a device at all (the PlayTest head's
/// <c>PlayTestOptions.WebGpu</c>), and to fail a test whose launch is missing the provider package it needs.
/// </summary>
/// <remarks>
/// The OpenGL elements have no gate of their own: they already resolve <see cref="INativeOpenGLWrapper"/> from the
/// head for every context, and the PlayTest head answers that resolution instead.
/// </remarks>
internal interface IOwnedGpuDeviceGate
{
	/// <summary>
	/// Decides whether an element may create its own device for <paramref name="api"/>.
	/// </summary>
	/// <param name="api">The API, one of the <see cref="OwnedGpuDeviceApis"/> names.</param>
	/// <returns><see langword="null"/> when the element may create its device (or the gate has no opinion about
	/// <paramref name="api"/>); otherwise the reason it must report the device as unavailable instead.</returns>
	/// <exception cref="System.Exception">The launch cannot serve <paramref name="api"/> at all (for example a test
	/// launch with no provider registered); the element reports initialization failure, and the gate's owner has
	/// recorded the error so the test fails with its message.</exception>
	string? GetUnavailableReason(string api);

	/// <summary>
	/// An opaque object the gate's provider produced for <paramref name="api"/> (for example a device it probed at
	/// launch), or <see langword="null"/>. Only the add-in that defines <paramref name="api"/> knows its type.
	/// </summary>
	/// <param name="api">The API, one of the <see cref="OwnedGpuDeviceApis"/> names.</param>
	/// <returns>The provider's object, or null.</returns>
	object? GetProviderState(string api);
}

/// <summary>The API names an <see cref="IOwnedGpuDeviceGate"/> is asked about.</summary>
internal static class OwnedGpuDeviceApis
{
	/// <summary>WebGPU (the WebGpu add-in's own device).</summary>
	internal const string WebGpu = "WebGpu";
}
