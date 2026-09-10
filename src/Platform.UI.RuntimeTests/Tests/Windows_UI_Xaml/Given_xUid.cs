#nullable enable
#if !WINAPPSDK
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using CodeBrix.Platform.UI.Extensions;
using CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml; //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml

[TestClass]
[RunsOnUIThread]
public class Given_xUid
{
	//TEST FULLY REPLACED - by src/Platform.UI.Tests/Windows_UI_Xaml_Markup/XUidTests/Given_xUid.cs
	[TestMethod]
	public void When_xUid()
	{
		var SUT = new When_xUid();

		TestServices.WindowHelper.WindowContent = SUT;

		Assert.AreEqual("en-US Value for When_xUid", SUT.defaultResolver.Text);
		Assert.AreEqual("en-US Value for When_xUid_Explicit in TopLevelNamedRuntimeTests", SUT.namedResolver.Text);
		Assert.AreEqual("en-US Value for SomePrefix/When_xUid_With_Prefix", SUT.defaultResolverWithPrefix.Text);
	}

	//TEST FULLY REPLACED - by src/Platform.UI.Tests/Windows_UI_Xaml_Markup/XUidTests/Given_xUid.cs
	[TestMethod]
	public void When_xUid_On_Root()
	{
		var SUT = new When_xUid_On_Root();
		Assert.AreEqual("en-US Value for When_xUid_Root.Title", SUT.Title);
	}
}
#endif
