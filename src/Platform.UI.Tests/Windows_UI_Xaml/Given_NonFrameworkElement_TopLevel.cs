using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.UI.Tests.App.Xaml;
using CodeBrix.Platform.UI.Tests.Helpers;
using CodeBrix.Platform.UI.Tests.Windows_UI_Xaml.Controls;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.Extensions;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml //Was previously: Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_NonFrameworkElement_TopLevel
	{
		[TestMethod]
		public void When_AttachingToEvent()
		{
			var SUT = new When_NonFrameworkElement_Event();

			SUT.RaiseMyEvent();

			Assert.AreEqual(1, SUT.MyEventCalled);
		}
	}
}
