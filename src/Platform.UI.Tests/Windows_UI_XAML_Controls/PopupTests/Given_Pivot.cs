using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Tests.Windows_UI_XAML_Controls.PopupTests.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CodeBrix.Platform.UI.Tests.PivotTests //Was previously: Uno.UI.Tests.PivotTests
{
	[TestClass]
	public class Given_Popup
	{
		[TestMethod]
		public void When_Popup()
		{
			var SUT = new When_Popup();

			Assert.IsInstanceOfType(SUT.FindName("myPopup"), typeof(Microsoft.UI.Xaml.Controls.Primitives.Popup));
		}
	}
}
