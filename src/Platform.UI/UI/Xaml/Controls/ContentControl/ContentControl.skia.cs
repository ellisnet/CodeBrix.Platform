using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.DataBinding;
using CodeBrix.Platform.UI.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using CodeBrix.Platform.Extensions.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using CodeBrix.Platform.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
		}

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
	}
}
