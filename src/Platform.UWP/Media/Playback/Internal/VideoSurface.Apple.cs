using System;
using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using Foundation;
#if false
using UIKit;
using _View = UIKit.UIView;
#else
using AppKit;
using _View = AppKit.NSView;
using CoreAnimation;
#endif

namespace CodeBrix.Platform.Media.Playback //Was previously: Uno.Media.Playback
{
	public class VideoSurface : _View, IVideoSurface
	{
#if false
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
#else
		public override void Layout()
		{
			base.Layout();
#endif
			if (Layer.Sublayers == null || Layer.Sublayers.Length == 0)
			{
				return;
			}

			foreach (var layer in Layer.Sublayers)
			{
				var avPlayerLayer = layer as AVPlayerLayer;
				if (avPlayerLayer != null)
				{
					CATransaction.Begin();
					CATransaction.AnimationDuration = 0;
					avPlayerLayer.Frame = Bounds;
					CATransaction.Commit();
				}
			}
		}
	}
}
