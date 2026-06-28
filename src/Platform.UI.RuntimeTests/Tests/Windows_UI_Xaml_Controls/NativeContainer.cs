using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	/// <summary>
	/// A non-UIElement type (on Android and iOS) that has a managed <see cref="Content"/> element.
	/// </summary>
	/// <remarks>On other platforms this is just a <see cref="Microsoft.UI.Xaml.Controls.ContentControl"/> for transparent testing.</remarks>
	partial class NativeContainer
	{
	}
}
