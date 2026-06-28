using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// An element that should be notified when the template in which it exists is being reused.
	/// </summary>
	[CodeBrixOnly]
	internal interface IFrameworkTemplatePoolAware
	{
		/// <summary>
		/// A call in which to execute any logic that should take place when template is recycled.
		/// </summary>
		void OnTemplateRecycled();
	}
}
