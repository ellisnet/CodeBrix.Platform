#nullable enable
using System;

namespace CodeBrix.Platform.Foundation.Logging //Was previously: Uno.Foundation.Logging
{
	internal interface IExternalLogger
	{
		void Log(LogLevel logLevel, string? message, Exception? exception = null);
		LogLevel LogLevel { get; }
	}
}
