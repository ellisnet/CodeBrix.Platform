using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

namespace CodeBrix.Platform.UI.RuntimeTests.Helpers //Was previously: Uno.UI.RuntimeTests.Helpers
{
	internal static class TestsResourceHelper
	{
		private static TestsResources _testsResources;

		/// <summary>
		/// Get resource defined in TestsResources.xaml (templates, styles etc)
		/// </summary>
		public static T GetResource<T>(string resourceName)
		{
			_testsResources ??= new TestsResources();
			return (T)_testsResources[resourceName];
		}
	}
}
