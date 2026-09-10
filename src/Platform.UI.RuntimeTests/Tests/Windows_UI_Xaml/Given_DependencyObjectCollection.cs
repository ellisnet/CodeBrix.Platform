using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ObjectiveC;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml; //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml

[TestClass]
public class Given_DependencyObjectCollection
{
	//TEST FULLY REPLACED - by src/Platform.UI.Tests/DependencyProperty/Given_DependencyObjectCollection.cs
	[TestMethod]
	public void When_Add_Multiple_And_Invoke()
	{
		DependencyObjectCollection c = new();

		List<string> list = [];

		void One(object sender, object args) => list.Add("One");
		void Two(object sender, object args) => list.Add("Two");

		c.VectorChanged += One;
		c.VectorChanged += Two;
		c.VectorChanged += One;
		c.VectorChanged -= One;

		c.Add(c);

		Assert.IsTrue(list.SequenceEqual(["One", "Two"]));
	}

	//TEST FULLY REPLACED - by src/Platform.UI.Tests/DependencyProperty/Given_DependencyObjectCollection.cs
	[TestMethod]
	public void When_Indexer_Get_IndexOutOfRange()
	{
		Assert.IsNull(new DependencyObjectCollection()[int.MaxValue]);
	}

	//TEST FULLY REPLACED - by src/Platform.UI.Tests/DependencyProperty/Given_DependencyObjectCollection.cs
	[TestMethod]
	public void When_Indexer_Get_NegativeIndex()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new DependencyObjectCollection()[-1]);
	}
}
