using Foundation;
using CodeBrix.Platform.Extensions;
using System;
using System.Collections.Generic;
using System.Text;


namespace CodeBrix.Platform.UI.Extensions; //Was previously: Uno.UI.Extensions

public static class NSUrlExtensions
{
	public static Uri ToUri(this NSUrl nsUrl) => CodeBrix.Platform.Extensions.NSUrlExtensions.ToUri(nsUrl);
}
