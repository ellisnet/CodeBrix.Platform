using System;
using System.Globalization;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Diagnostics.Eventing;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using CodeBrix.Platform.Extensions.Disposables;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;

using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	partial class Image : FrameworkElement
	{
		partial void OnSourceChanged(ImageSource newValue, bool forceReload = false);

		private void OnStretchChanged(Stretch newValue, Stretch oldValue) => InvalidateArrange();

		internal override bool IsViewHit() => Source != null || base.IsViewHit();
	}
}
