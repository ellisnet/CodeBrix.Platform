#nullable enable

using CodeBrix.Platform.UI.Xaml.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using SilverAssertions.Execution;
using Xunit;

namespace CodeBrix.Platform.UI.Headless.Tests;

/// <summary>
/// <see cref="DependencyPropertyHelper"/>: looking a dependency property up by name, reading its
/// metadata, and working out what a value would fall back to if it were cleared.
/// </summary>
/// <remarks>
/// <para>
/// Ported from src/Platform.UI.RuntimeTests/Tests/Platform_Helpers/Given_DependencyPropertyHelper.cs.
/// This exercises the dependency-property machinery directly - registration, metadata, precedence
/// and an explicit Style - which is all in-memory bookkeeping, so it needs no Window, no XamlRoot
/// and no layout pass, and answers the same on Linux, Windows and macOS.
/// </para>
/// <para>
/// The helper is internal to the framework, which is why this file needs the InternalsVisibleTo
/// grant in src/Platform.UI/AssemblyInfo.cs. The originals were already written with
/// SilverAssertions, so the port is close to verbatim: <c>[TestMethod]</c> becomes <c>[Fact]</c>,
/// and the <c>[TestClass]</c>/<c>[RunsOnUIThread]</c> pair goes away because every thread in this
/// process reports dispatcher access. The original's <c>#if HAS_CODEBRIX</c> guard goes too - this
/// project only ever builds the flavour where the helper exists.
/// </para>
/// </remarks>
public class DependencyPropertyHelperTests
{
	[Fact]
	public void When_GetDefaultValue()
	{
		// Arrange
		var property = TestClass.TestProperty;

		// Act
		var defaultValue = DependencyPropertyHelper.GetDefaultValue(property);

		// Assert
		defaultValue.Should().Be("TestValue");
	}

	[Fact]
	public void When_GetDependencyPropertyByName_OwnerType()
	{
		// Arrange
		var propertyName = "TestProperty";

		// Act
		var property1 = DependencyPropertyHelper.GetDependencyPropertyByName(typeof(TestClass), propertyName);
		var property2 = DependencyPropertyHelper.GetDependencyPropertyByName(typeof(DerivedTestClass), propertyName);

		// Assert
		property1.Should().Be(TestClass.TestProperty);
		property2.Should().Be(TestClass.TestProperty);
	}

	[Fact]
	public void When_GetDependencyPropertyByName_Property()
	{
		// Arrange
		var propertyName = "TestProperty";

		// Act
		var property1 = DependencyPropertyHelper.GetDependencyPropertyByName<TestClass>(propertyName);
		var property2 = DependencyPropertyHelper.GetDependencyPropertyByName<TestClass>(propertyName);

		// Assert
		property1.Should().Be(TestClass.TestProperty);
		property2.Should().Be(TestClass.TestProperty);
	}

	[Fact]
	public void When_GetDependencyPropertyByName_InvalidProperty()
	{
		// Arrange
		var propertyName = "InvalidProperty";

		// Act
		var property = DependencyPropertyHelper.GetDependencyPropertyByName(typeof(TestClass), propertyName);

		// Assert
		property.Should().BeNull();
	}

	[Fact]
	public void When_GetDependencyPropertyByName_InvalidPropertyCasing()
	{
		// Arrange
		var propertyName = "testProperty";

		// Act
		var property = DependencyPropertyHelper.GetDependencyPropertyByName(typeof(TestClass), propertyName);

		// Assert
		property.Should().BeNull();
	}

	[Fact]
	public void When_GetDependencyPropertiesForType()
	{
		// Act
		var properties = DependencyPropertyHelper.GetDependencyPropertiesForType<TestClass>();

		// Assert
		properties.Should().Contain(TestClass.TestProperty);
	}

	[Fact]
	public void When_TryGetDependencyPropertiesForType()
	{
		// Act
		var success = DependencyPropertyHelper.TryGetDependencyPropertiesForType(typeof(TestClass), out var properties);

		// Assert
		success.Should().BeTrue();
		properties.Should().Contain(TestClass.TestProperty);
	}

	[Fact]
	public void When_TryGetDependencyPropertiesForType_Invalid()
	{
		// Act
		var success = DependencyPropertyHelper.TryGetDependencyPropertiesForType(typeof(string), out var properties);

		// Assert
		success.Should().BeFalse();
		properties.Should().BeNull();
	}

	[Fact]
	public void When_GetPropertyType()
	{
		// Arrange
		var property = TestClass.TestProperty;

		// Act
		var propertyType = DependencyPropertyHelper.GetPropertyType(property);

		// Assert
		propertyType.Should().Be(typeof(string));
	}

	[Fact]
	public void When_GetPropertyDetails()
	{
		// Arrange
		var property = TestClass.TestProperty;

		// Act
		var (valueType, ownerType, name, isTypeNullable, isAttached, inInherited, defaultValue) =
			DependencyPropertyHelper.GetDetails(property);

		// Assert
		using var _ = new AssertionScope();
		valueType.Should().Be(typeof(string));
		ownerType.Should().Be(typeof(TestClass));
		name.Should().Be("TestProperty");
		isTypeNullable.Should().BeTrue();
		isAttached.Should().BeFalse();
		inInherited.Should().BeFalse();
		defaultValue.Should().Be("TestValue");
	}

	[Fact]
	public void When_GetPropertyDetails_DataContext()
	{
		// Arrange
		var property = UIElement.DataContextProperty;

		// Act
		var (valueType, _, name, isTypeNullable, isAttached, inInherited, defaultValue) =
			DependencyPropertyHelper.GetDetails(property);

		// Assert
		using var _ = new AssertionScope();
		valueType.Should().Be(typeof(object));
		// ownerType is not checked here because it's different following the platform
		name.Should().Be("DataContext");
		isTypeNullable.Should().BeTrue();
		isAttached.Should().BeFalse();
		inInherited.Should().BeTrue();
		defaultValue.Should().BeNull();
	}

	[Fact]
	public void When_GetPropertyDetails_Attached()
	{
		// Arrange
		var property = Grid.RowProperty;

		// Act
		var (valueType, ownerType, name, isTypeNullable, isAttached, inInherited, defaultValue) =
			DependencyPropertyHelper.GetDetails(property);

		// Assert
		using var _ = new AssertionScope();
		valueType.Should().Be(typeof(int));
		ownerType.Should().Be(typeof(Grid));
		name.Should().Be("Row");
		isTypeNullable.Should().BeFalse();
		isAttached.Should().BeTrue();
		inInherited.Should().BeFalse();
		defaultValue.Should().Be(0);
	}

	[Fact]
	public void When_GetProperties()
	{
		// Act
		var properties = DependencyPropertyHelper.GetDependencyPropertiesForType<DerivedTestClass>();

		// Assert
		properties.Should().Contain(DerivedTestClass.TestProperty);
	}

	[Fact]
	public void When_GetDefaultValue_Derived()
	{
		// Arrange
		var property = DerivedTestClass.TestProperty;

		// Act
		var defaultValue = DependencyPropertyHelper.GetDefaultValue(property);

		// Assert
		defaultValue.Should().Be("TestValue");
	}

	[Fact]
	public void When_GetDefaultUnsetValue_FromStyle()
	{
		// Arrange
		var sut = new DerivedTestClass();
		sut.SetValue(TestClass.TestProperty, "Something");

		// Act
		var (unsetValue, precedence) = DependencyPropertyHelper.GetDefaultUnsetValue(sut, TestClass.TestProperty);

		// Assert
		unsetValue.Should().Be("StyledTestValue");
		precedence.Should().Be(DependencyPropertyValuePrecedences.ExplicitStyle);
	}

	[Fact]
	public void When_GetDefaultUnsetValue()
	{
		// Arrange
		var sut = new TestClass();
		sut.SetValue(TestClass.TestProperty, "Something");

		// Act
		var (unsetValue, precedence) = DependencyPropertyHelper.GetDefaultUnsetValue(sut, TestClass.TestProperty);

		// Assert
		unsetValue.Should().Be("TestValue");
		precedence.Should().Be(DependencyPropertyValuePrecedences.DefaultValue);
	}

	// Not a plain DependencyObject because we don't want to deal with the generator here.
	private partial class TestClass : FrameworkElement
	{
		public static readonly DependencyProperty TestProperty = DependencyProperty.Register(
			"TestProperty", typeof(string), typeof(TestClass), new PropertyMetadata("TestValue"));
	}

	private partial class DerivedTestClass : TestClass
	{
		public DerivedTestClass()
		{
			Style = new Style(typeof(DerivedTestClass))
			{
				Setters = { new Setter(TestProperty, "StyledTestValue") },
			};
		}
	}
}
