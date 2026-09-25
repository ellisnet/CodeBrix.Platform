using System;

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

/// <summary>
/// Decides, from the CodeBrix.Platform head the application is running on, whether an
/// <b>off-screen</b> OpenGL context created by <see cref="OffscreenGLContext"/> should be driven as
/// OpenGL ES (EGL/GLES) or desktop OpenGL when building a SkiaSharp <c>GRGlInterface</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the single place the per-head GL-vs-GLES branch lives (see
/// <see cref="OffscreenGLContext.CreateGrContext"/>). Centralizing it means a head change is a
/// one-line fix here rather than an edit in every GPU-Skia consumer. The head is identified the same
/// way the PolyHaven sample's <c>VulkanPlatformSupport</c> does it — by scanning the loaded
/// <c>CodeBrix.Platform.UI.Runtime.Skia.*</c> head-runtime assembly — so this add-in never has to
/// reference the head runtimes.
/// </para>
/// <para>
/// The mapping (which matches the off-screen context each head hands back):
/// <list type="bullet">
/// <item><description>X11 (off-screen is always GLX), Win32-Skia and WPF-Skia (WGL): desktop GL.</description></item>
/// <item><description>Wayland, macOS (ANGLE) and Frame Buffer: GLES.</description></item>
/// </list>
/// An unrecognized host (for example a unit-test host or real WinUI on Windows, which uses WGL) is
/// treated as desktop GL. Because <see cref="OffscreenGLContext.CreateGrContext"/> falls back to the
/// other flavor when the first fails, a misclassification is self-correcting rather than fatal.
/// </para>
/// </remarks>
internal static class Graphics3DGLHeadDetection
{
	// Every head's Program.cs loads exactly one of these runtime assemblies, so scanning the loaded
	// assemblies identifies the head without referencing any of them. Checked as a prefix so
	// satellite assemblies (e.g. ...Skia.Win32.Support) still match their head.
	private const string HeadAssemblyPrefix = "CodeBrix.Platform.UI.Runtime.Skia.";

	/// <summary>
	/// Whether the current head's off-screen OpenGL context should be treated as GLES (rather than
	/// desktop GL) when assembling a SkiaSharp <c>GRGlInterface</c>.
	/// </summary>
	/// <returns><see langword="true"/> for the GLES heads (Wayland, macOS, Frame Buffer); otherwise <see langword="false"/>.</returns>
	public static bool CurrentHeadUsesGles()
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (TryClassifyUsesGles(assembly.GetName().Name, out var usesGles))
			{
				return usesGles;
			}
		}

		// No head runtime found (unit-test host, or real WinUI/WGL on Windows): desktop GL.
		return false;
	}

	/// <summary>
	/// Classifies a single assembly name. Returns <see langword="true"/> from the method when
	/// <paramref name="assemblyName"/> is a recognized head runtime, with <paramref name="usesGles"/>
	/// set to that head's flavor; returns <see langword="false"/> when the name is not a head runtime.
	/// </summary>
	/// <param name="assemblyName">The assembly simple name to classify.</param>
	/// <param name="usesGles">On a recognized head, whether that head uses GLES.</param>
	/// <returns><see langword="true"/> when the name is a recognized head runtime; otherwise <see langword="false"/>.</returns>
	internal static bool TryClassifyUsesGles(string? assemblyName, out bool usesGles)
	{
		usesGles = false;
		if (assemblyName is null || !assemblyName.StartsWith(HeadAssemblyPrefix, StringComparison.Ordinal))
		{
			return false;
		}

		var head = assemblyName[HeadAssemblyPrefix.Length..];

		// GLES heads. The Emulated frame buffer serves the same offscreen EGL/GLES context as the
		// real one, so it classifies identically (an exact match — NOT a prefix — so any future
		// "Linux.FrameBuffer.<something>" sibling has to decide its flavor here deliberately).
		if (head == "Wayland"
			|| head == "MacOS"
			|| head == "Linux.FrameBuffer"
			|| head == "Linux.FrameBuffer.Emulated")
		{
			usesGles = true;
			return true;
		}

		// Desktop-GL heads (X11 off-screen is GLX; Win32/WPF are WGL). Win32 has satellite
		// assemblies (e.g. ...Win32.Support), so match it as a prefix too.
		if (head == "X11"
			|| head == "Wpf"
			|| head == "Win32"
			|| head.StartsWith("Win32.", StringComparison.Ordinal))
		{
			usesGles = false;
			return true;
		}

		return false;
	}
}
