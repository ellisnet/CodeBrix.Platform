#nullable enable

using System;
using CodeBrix.Platform.UI.Runtime.Skia;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

internal interface IPlatformHostBuilder
{
	bool IsSupported { get; }

	CodeBrixPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type applicationType);
}
