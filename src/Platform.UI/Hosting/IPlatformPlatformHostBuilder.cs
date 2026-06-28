#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public interface ICodeBrixPlatformHostBuilder
{
	internal Func<Application>? AppBuilder { get; set; }

	internal void SetAppType<TApplication>()
		where TApplication : Microsoft.UI.Xaml.Application;

	internal Action? AfterInitAction { get; set; }

	internal void AddHostBuilder(Func<IPlatformHostBuilder> hostBuilder);

	public CodeBrixPlatformHost Build();
}
