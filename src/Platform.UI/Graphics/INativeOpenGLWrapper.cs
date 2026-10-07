#nullable enable

using System;

#if WINAPPSDK || WINDOWS_UWP
namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL
#else
namespace CodeBrix.Platform.Graphics; //Was previously: Uno.Graphics
#endif

internal interface INativeOpenGLWrapper : IDisposable
{
	public IntPtr GetProcAddress(string proc);

	public bool TryGetProcAddress(string proc, out IntPtr addr);

	/// <returns>A disposable that restores the OpenGL context to what it was at the time of this method call.</returns>
	public IDisposable MakeCurrent();

	/// <summary>
	/// The flavour of the context this wrapper hands out, when the wrapper knows it: <see langword="true"/> for
	/// OpenGL ES, <see langword="false"/> for desktop OpenGL, <see langword="null"/> (the default) when the
	/// consumer should decide from the running head instead.
	/// </summary>
	public bool? UsesGles => null;
}
