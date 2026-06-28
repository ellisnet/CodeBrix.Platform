using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.Platform.UI.Controls //Was previously: Uno.UI.Controls
{
	/// <summary>
	/// View which scales with iOS Accessibility font sizes
	/// </summary>
	internal interface IFontScalable
	{
		void RefreshFont();
	}
}
