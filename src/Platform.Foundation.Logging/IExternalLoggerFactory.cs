#nullable enable

namespace CodeBrix.Platform.Foundation.Logging //Was previously: Uno.Foundation.Logging
{
	internal interface IExternalLoggerFactory
	{
		IExternalLogger CreateLogger(string categoryName);
	}
}
