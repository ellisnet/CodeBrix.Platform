using CodeBrix.Platform.UI.Controls;
using CodeBrix.Platform.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Foundation.Logging;
using System.Drawing;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI;
using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Control
	{
		public Control()
		{
			InitializeControl();
		}

		partial void UnregisterSubView()
		{
			var child = this.GetChildren()?.FirstOrDefault();
			if (child != null)
			{
				RemoveChild(child);
			}
		}

		partial void RegisterSubView(View child)
		{
			AddChild(child);
		}

		/// <summary>
		/// Gets the first sub-view of this control or null if there is none
		/// </summary>
		internal IFrameworkElement GetTemplateRoot()
		{
			return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
		}
	}
}
