using System;
using Foundation;

namespace CodeBrix.Platform.Extensions;

internal static class NSUrlExtensions
{
	public static Uri ToUri(this NSUrl nsUrl)
	{
		if (nsUrl.AbsoluteString.IsNullOrEmpty())
		{
			return null;
		}
		return new Uri(nsUrl.AbsoluteString);
	}
}
