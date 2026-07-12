using System;
using CodeBrix.Platform.Simple;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

public enum TestFruit
{
	[SimpleEnumAttribute<TestFruitInfo>(nameof(TestFruitInfo.Apple))]
	Apple,

	[SimpleEnumAttribute<TestFruitInfo>(nameof(TestFruitInfo.Banana))]
	Banana,

	//Deliberately carries no info attribute
	Cherry,
}

public class TestFruitInfo : SimpleEnumInfo<TestFruit>
{
	private TestFruitInfo(TestFruit member, string description)
		: base(member)
	{
		Description = description;
	}

	public static TestFruitInfo Apple { get; } = new(TestFruit.Apple, "A crisp apple");
	public static TestFruitInfo Banana { get; } = new(TestFruit.Banana, "A yellow banana");
}

[TestClass]
public class Given_SimpleEnum
{
	[TestMethod]
	public void When_FindMemberInfo_By_Member()
	{
		//Act
		var info = SimpleEnumHelper.FindMemberInfo<TestFruit, TestFruitInfo>(TestFruit.Apple);

		//Assert
		info.Should().Be(TestFruitInfo.Apple);
		info.Description.Should().Be("A crisp apple");
		info.Member.Should().Be(TestFruit.Apple);
		info.EnumType.Should().Be(typeof(TestFruit));
	}

	[TestMethod]
	public void When_FindMemberInfo_By_Name_Is_Case_Insensitive() =>
		SimpleEnumHelper.FindMemberInfo<TestFruitInfo>("banana").Should().Be(TestFruitInfo.Banana);

	[TestMethod]
	public void When_Member_Has_No_Attribute_Returns_Null() =>
		Assert.IsNull(SimpleEnumHelper.FindMemberInfo<TestFruit, TestFruitInfo>(TestFruit.Cherry));

	[TestMethod]
	public void When_Name_Is_Unknown_Returns_Null() =>
		Assert.IsNull(SimpleEnumHelper.FindMemberInfo<TestFruitInfo>("durian"));

	[TestMethod]
	public void When_GetInfoDictionary_Covers_Every_Member()
	{
		//Act
		var dictionary = SimpleEnumHelper.GetInfoDictionary<TestFruit, TestFruitInfo>();

		//Assert
		dictionary.Count.Should().Be(3);
		dictionary[TestFruit.Apple].Should().Be(TestFruitInfo.Apple);
		dictionary[TestFruit.Banana].Should().Be(TestFruitInfo.Banana);
		Assert.IsNull(dictionary[TestFruit.Cherry]);
	}

	[TestMethod]
	public void When_GetPossibleValues_Skips_Members_Without_Info()
	{
		//Act
		var values = SimpleEnumHelper.GetPossibleValues<TestFruit, TestFruitInfo>();

		//Assert
		values.Count.Should().Be(2);
		CollectionAssert.Contains((System.Collections.ICollection)values, TestFruitInfo.Apple);
		CollectionAssert.Contains((System.Collections.ICollection)values, TestFruitInfo.Banana);
	}

	[TestMethod]
	public void When_Repeated_Lookups_Return_The_Same_Cached_Instance() =>
		ReferenceEquals(
			SimpleEnumHelper.FindMemberInfo<TestFruit, TestFruitInfo>(TestFruit.Apple),
			SimpleEnumHelper.FindMemberInfo<TestFruit, TestFruitInfo>(TestFruit.Apple)).Should().Be(true);

	[TestMethod]
	public void When_Info_Constructed_With_Undefined_Member_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new BrokenFruitInfo((TestFruit)99));

	private sealed class BrokenFruitInfo : SimpleEnumInfo<TestFruit>
	{
		public BrokenFruitInfo(TestFruit member) : base(member) { }
	}
}
