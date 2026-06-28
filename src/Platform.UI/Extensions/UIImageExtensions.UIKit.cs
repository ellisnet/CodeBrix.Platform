using CoreGraphics;
using UIKit;

namespace CodeBrix.Platform.UI.Extensions //Was previously: Uno.UI.Extensions
{
	internal static partial class NSUIImageExtensions
	{
		internal static UIImage FromCGImage(CGImage cgImage)
		{
			return UIImage.FromImage(cgImage);
		}
	}
}
