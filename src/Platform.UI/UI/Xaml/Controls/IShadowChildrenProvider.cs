#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;

#if false
using View = UIKit.UIView;
#elif false
using View = Android.Views.View;
#endif

namespace CodeBrix.Platform.UI.Controls //Was previously: Uno.UI.Controls
{
	/// <summary>
	/// Provides access to the shadowed children list for a native view
	/// inheriting control. Used to improve the children enumeration performance.
	/// </summary>
	internal interface IShadowChildrenProvider
	{
		/// <summary>
		/// An enumerable of children views.
		/// </summary>
		/// <remarks>
		/// This property is exposed as a concrete <see cref="List{T}"/> to benefit from
		/// allocation-less enumeration of the shadow children.
		/// </remarks>
		List<View> ChildrenShadow { get; }
	}
}
#endif
