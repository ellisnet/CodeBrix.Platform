using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI;
using System.Linq;
using CodeBrix.Platform.Extensions.Specialized;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void RequestLayoutPartial()
		{
			InvalidateMeasure();
		}
	}
}
