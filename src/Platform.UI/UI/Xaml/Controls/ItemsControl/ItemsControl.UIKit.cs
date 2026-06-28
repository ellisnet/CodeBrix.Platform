using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			SetNeedsLayout();
		}
	}
}

