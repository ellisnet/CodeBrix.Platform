using SkiaSharp;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.UI.Xaml.Media //Was previously: Uno.UI.UI.Xaml.Media
{
	internal static class FillRuleExtensions
	{
		internal static SKPathFillType ToSkiaFillType(this FillRule fillRule)
		{
			return fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
		}
	}
}
