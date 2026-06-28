using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.Helpers //Was previously: Uno.UI.Tests.Helpers
{
	public static class MuxVerify
	{
		public static void VerifyAreEqual(object actual, object expected)
		{
			// Switch parameter order to get the proper error messages.
			Assert.AreEqual(expected, actual);
		}
	}
}
