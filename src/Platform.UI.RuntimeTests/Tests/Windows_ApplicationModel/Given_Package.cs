#if false
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_ApplicationModel //Was previously: Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel
{
	[TestClass]
	public class Given_Package
	{
		[TestMethod]
		public void When_DisplayNameQueried()
		{
			var SUT = Package.Current;
			Assert.IsNotNull(SUT.DisplayName);
		}
	}
}
#endif
