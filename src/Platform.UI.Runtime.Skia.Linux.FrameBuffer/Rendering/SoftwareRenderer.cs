using System;
using System.Threading;
using Windows.Foundation;
using SkiaSharp;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia //Was previously: Uno.UI.Runtime.Skia
{
	internal class SoftwareRenderer : FrameBufferRenderer
	{
		private FrameBufferDevice _fbDev;
		private readonly AutoResetEvent _renderInvalidationEvent = new(false);

		public SoftwareRenderer(IXamlRootHost host, MouseIndicatorOptions mouseIndicatorOptions) : base(host, mouseIndicatorOptions)
		{
			_fbDev = new FrameBufferDevice();
			_fbDev.Init();
			FrameBufferWindowWrapper.Instance.SetSize(new Size(_fbDev.ScreenSize.Width, _fbDev.ScreenSize.Height));

			new Thread(_ =>
			{
				while (true)
				{
					try
					{
						_renderInvalidationEvent.WaitOne();
						Render();
						_fbDev.VSync();
						_surface?.ReadPixels(
							new SKImageInfo((int)_fbDev.ScreenSize.Width, (int)_fbDev.ScreenSize.Height, _fbDev.PixelFormat, SKAlphaType.Premul),
							_fbDev.BufferAddress,
							_fbDev.RowBytes,
							0,
							0);
					}
					catch (Exception ex)
					{
						this.LogError()?.Error("Error during software rendering", ex);
					}
				}
			})
			{
				IsBackground = true,
				Name = "FrameBuffer software rendering thread"
			}.Start();
		}

		public override void InvalidateRender() => _renderInvalidationEvent.Set();

		protected override IDisposable MakeCurrent() => Disposable.Empty;

		protected override SKSurface UpdateSize(int width, int height)
			=> SKSurface.Create(new SKImageInfo(width, height, _fbDev.PixelFormat, SKAlphaType.Premul));
	}
}
