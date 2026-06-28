#nullable enable

using System;

namespace CodeBrix.Platform.Globalization.NumberFormatting; //Was previously: Uno.Globalization.NumberFormatting

internal static class ExceptionHelper
{
	public static void ThrowArgumentException(string parameterName)
	{
		throw new ArgumentException($"The parameter is incorrect.\r\n\r\n{parameterName}");
	}

	public static void ThrowNullReferenceException(string parameterName)
	{
		throw new NullReferenceException($"Invalid pointer\r\n\r\n{parameterName}");
	}
}
