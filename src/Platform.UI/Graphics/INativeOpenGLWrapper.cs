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
}
