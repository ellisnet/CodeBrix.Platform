using Microsoft.UI.Xaml.Media;
using CoreAnimation;
using Foundation;

namespace CodeBrix.Platform.UI.UI.Xaml.Media //Was previously: Uno.UI.UI.Xaml.Media
{
	internal static class FillRuleExtensions
	{
		internal static NSString ToCAShapeLayerFillRule(this FillRule fillRule)
		{
			return fillRule == FillRule.EvenOdd ? CAShapeLayer.FillRuleEvenOdd : CAShapeLayer.FillRuleNonZero;
		}
	}
}
