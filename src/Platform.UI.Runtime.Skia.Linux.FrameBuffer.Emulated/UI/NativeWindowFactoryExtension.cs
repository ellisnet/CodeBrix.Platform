#nullable enable

using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.UI; //Was previously: Uno.UI.Runtime.Skia.Linux.FrameBuffer.UI

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly IXamlRootHost _host;

	private Window? _initialWindow;

	internal NativeWindowFactoryExtension(IXamlRootHost host)
	{
		_host = host;
	}

	public bool SupportsClosingCancellation => false;

	public bool SupportsMultipleWindows => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		if (_initialWindow is not null && _initialWindow != window)
		{
			throw new InvalidOperationException("FrameBuffer currently supports single window only");
		}

		_initialWindow = window;
		FrameBufferWindowWrapper.Instance.SetWindow(window, xamlRoot);
		XamlRootMap.Register(xamlRoot, _host);

		return FrameBufferWindowWrapper.Instance;
	}
}
