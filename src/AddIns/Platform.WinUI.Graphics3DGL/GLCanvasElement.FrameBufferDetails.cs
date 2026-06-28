using System;
using Windows.Foundation;
using CodeBrix.Platform.OpenGL;

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

public abstract partial class GLCanvasElement
{
	private class FrameBufferDetails : IDisposable
	{
		private readonly GL _gl;
		private readonly uint _textureColorBuffer;
		private readonly uint _renderBuffer;

		public uint Framebuffer { get; }

		public unsafe FrameBufferDetails(GL gl, Size renderSize)
		{
			_gl = gl;

			Framebuffer = gl.GenBuffer();
			gl.BindFramebuffer(GLEnum.Framebuffer, Framebuffer);
			{
				_textureColorBuffer = gl.GenTexture();
				gl.BindTexture(GLEnum.Texture2D, _textureColorBuffer);
				{
					gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgb, (uint)renderSize.Width, (uint)renderSize.Height, 0, GLEnum.Rgb,
						GLEnum.UnsignedByte, (void*)0);
					// CodeBrix.Platform.OpenGL (Silk.NET.OpenGL 2.23.0 port) changed TexParameterI's
					// 3rd argument to `ref readonly`, which won't accept a cast expression — must be a variable.
					uint linearFilter = (uint)GLEnum.Linear;
					gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, in linearFilter);
					gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, in linearFilter);
					gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0,
						GLEnum.Texture2D, _textureColorBuffer, 0);
				}
				gl.BindTexture(GLEnum.Texture2D, 0);

				_renderBuffer = gl.GenRenderbuffer();
				gl.BindRenderbuffer(GLEnum.Renderbuffer, _renderBuffer);
				{
					gl.RenderbufferStorage(GLEnum.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)renderSize.Width, (uint)renderSize.Width);
					gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment,
						GLEnum.Renderbuffer, _renderBuffer);
				}
				gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);

				if (gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
				{
					throw new InvalidOperationException("Offscreen framebuffer is not complete");
				}
			}
			gl.BindFramebuffer(GLEnum.Framebuffer, 0);
		}

		public void Dispose()
		{
			_gl.DeleteFramebuffer(Framebuffer);
			_gl.DeleteTexture(_textureColorBuffer);
			_gl.DeleteRenderbuffer(_renderBuffer);
		}
	}
}
