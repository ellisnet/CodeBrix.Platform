#nullable enable

using System;
using CodeBrix.Platform.UI.Xaml.Core;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using WinUICoreServices = CodeBrix.Platform.UI.Xaml.Core.CoreServices;

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

internal class CoreWindowWindow : BaseWindowImplementation
{
	private readonly ContentManager _contentManager;

	public CoreWindowWindow(Window window) : base(window)
	{
		_contentManager = new ContentManager(window, true);
		CoreWindow = CoreWindow.GetOrCreateForCurrentThread();
	}

#pragma warning disable CS0067
	public override CoreWindow CoreWindow { get; }

	public override UIElement? Content
	{
		get => _contentManager.Content;
		set => _contentManager.Content = value;
	}

	public override XamlRoot? XamlRoot => WinUICoreServices.Instance.MainVisualTree?.GetOrCreateXamlRoot();
}
