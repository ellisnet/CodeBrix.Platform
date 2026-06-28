#nullable enable

namespace CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging //Was previously: Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using CodeBrix.Platform.Foundation.Logging;

	class MicrosoftLoggerFactory : Foundation.Logging.IExternalLoggerFactory
	{
		public IExternalLogger CreateLogger(string categoryName) => new MicrosoftLogger(CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(categoryName));
	}
}
