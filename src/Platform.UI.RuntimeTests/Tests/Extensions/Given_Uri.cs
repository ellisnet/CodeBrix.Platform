using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.Extensions;
using Windows.Web.Http;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Extensions //Was previously: Uno.UI.RuntimeTests.Tests.Extensions
{
	[TestClass]
	public class Given_Uri
	{

#if !WINAPPSDK
		[TestMethod]
		public void When_Do_TrimEndUriSlash()
		{
			var uri = new Uri("http://localhost/url/");

			var initialLen = uri.ToString().Length;

			uri = uri.TrimEndUriSlash();

			var actualLen = uri.ToString().Length;

			Assert.AreNotEqual(initialLen, actualLen);
		}

		[TestMethod]
		public void When_DoNot_Remove_TrimEndUriSlash()
		{
			var uri = new Uri("http://localhost/url");

			var initialLen = uri.ToString().Length;

			uri = uri.TrimEndUriSlash();

			var actualLen = uri.ToString().Length;

			Assert.AreEqual(initialLen, actualLen);
		}

#endif

	}
}
