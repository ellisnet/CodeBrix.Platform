using System;
using CodeBrix.Platform.UI.DataBinding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.BinderTests;

/// <summary>
/// Fences the binding engine's behaviour when the generated bindable metadata knows a TYPE but
/// carries neither a dependency property nor a setter for the member being bound (a generator
/// MISS - an internal dependency property, a private setter, an attached property the generator
/// skipped). The engine must fall through to its reflection path, exactly as the value GETTER
/// already does, instead of handing back a setter that silently discards the write.
/// </summary>
/// <remarks>
/// The default host-free flavour installs no metadata provider at all, so these tests install one
/// for the duration of the test and clear the engine's memoization caches on both sides of it.
/// The assembly is marked <c>[DoNotParallelize]</c>, so the swap is safe.
/// </remarks>
[TestClass]
public partial class BindingPropertyHelperMetadataMissTests
{
	private IBindableMetadataProvider _previousProvider;

	[TestInitialize]
	public void Setup()
	{
		_previousProvider = BindableMetadata.Provider;
		BindingPropertyHelper.ClearCaches();
	}

	[TestCleanup]
	public void Cleanup()
	{
		BindableMetadata.Provider = _previousProvider;
		BindingPropertyHelper.ClearCaches();
	}

	[TestMethod]
	public void metadata_miss_falls_back_to_reflection_and_the_write_lands()
	{
		//Arrange
		var property = new StubBindableProperty(typeof(string), dependencyProperty: null, setter: null);
		BindableMetadata.Provider = new StubBindableMetadataProvider(
			new StubBindableType(typeof(MetadataMissProbe), nameof(MetadataMissProbe.Probe), property));

		var SUT = new MetadataMissProbe();
		SUT.SetBinding(nameof(MetadataMissProbe.Probe), new Binding { Path = "Value" });

		//Act
		SUT.DataContext = new { Value = "42" };

		//Assert
		SUT.Probe.Should().Be("42", "a metadata miss must fall through to the reflection path, which finds ProbeProperty");
	}

	[TestMethod]
	public void metadata_miss_falls_back_for_an_attached_property()
	{
		//Arrange - the second shape of the same miss: the metadata knows the target type AND the
		//attached property's OWNER type, but lists no member for the attached property itself, so
		//the descriptor comes back with an owner and a null property.
		BindableMetadata.Provider = new StubBindableMetadataProvider(
			new StubBindableType(typeof(MetadataMissProbe), name: null, property: null),
			new StubBindableType(typeof(MetadataMissAttached), name: null, property: null));

		var SUT = new MetadataMissProbe();
		var setter = BindingPropertyHelper.GetValueSetter(
			typeof(MetadataMissProbe),
			"(CodeBrix.Platform.UI.Tests.BinderTests:MetadataMissAttached.Tag)",
			convert: true);

		//Act
		setter(SUT, 42);

		//Assert
		MetadataMissAttached.GetTag(SUT).Should().Be(42, "the attached-property miss must fall through to the reflection path as well");
	}

	[TestMethod]
	public void metadata_miss_falls_back_when_the_setter_is_asked_for_directly()
	{
		//Arrange
		var property = new StubBindableProperty(typeof(string), dependencyProperty: null, setter: null);
		BindableMetadata.Provider = new StubBindableMetadataProvider(
			new StubBindableType(typeof(MetadataMissProbe), nameof(MetadataMissProbe.Probe), property));

		var SUT = new MetadataMissProbe();
		var setter = BindingPropertyHelper.GetValueSetter(typeof(MetadataMissProbe), nameof(MetadataMissProbe.Probe), convert: true);

		//Act
		setter(SUT, "42");

		//Assert
		SUT.Probe.Should().Be("42", "the engine must never hand back a setter that discards the value");
	}

	[TestMethod]
	public void metadata_hit_still_uses_the_generated_setter()
	{
		//Arrange
		object written = null;
		var property = new StubBindableProperty(
			typeof(string),
			dependencyProperty: null,
			setter: (instance, value, precedence) => written = value);
		BindableMetadata.Provider = new StubBindableMetadataProvider(
			new StubBindableType(typeof(MetadataMissProbe), nameof(MetadataMissProbe.Probe), property));

		var SUT = new MetadataMissProbe();
		SUT.SetBinding(nameof(MetadataMissProbe.Probe), new Binding { Path = "Value" });

		//Act
		SUT.DataContext = new { Value = "42" };

		//Assert
		written.Should().Be("42", "the metadata fast path must still win when the metadata does carry a setter");
		SUT.Probe.Should().BeNull("the metadata setter was used instead of the dependency property");
	}

	[TestMethod]
	public void an_unknown_type_is_unaffected_by_the_provider()
	{
		//Arrange
		BindableMetadata.Provider = new StubBindableMetadataProvider(
			new StubBindableType(typeof(MetadataMissAttached), name: null, property: null));

		var SUT = new MetadataMissProbe();
		SUT.SetBinding(nameof(MetadataMissProbe.Probe), new Binding { Path = "Value" });

		//Act
		SUT.DataContext = new { Value = "42" };

		//Assert
		SUT.Probe.Should().Be("42", "a type the metadata does not know has always taken the reflection path");
	}
}

/// <summary>
/// A control whose <see cref="Probe"/> dependency property stands in for a member the bindable
/// metadata generator skipped.
/// </summary>
public partial class MetadataMissProbe : DependencyObject
{
	/// <summary>Identifies the <see cref="Probe"/> dependency property.</summary>
	public static readonly DependencyProperty ProbeProperty = DependencyProperty.Register(
		nameof(Probe),
		typeof(string),
		typeof(MetadataMissProbe),
		new PropertyMetadata(default(string)));

	/// <summary>Gets or sets the probed value.</summary>
	public string Probe
	{
		get => (string)GetValue(ProbeProperty);
		set => SetValue(ProbeProperty, value);
	}
}

/// <summary>Declares an attached property whose owner type the stub metadata claims to know.</summary>
public static class MetadataMissAttached
{
	/// <summary>Identifies the MetadataMissAttached.Tag attached property.</summary>
	public static readonly DependencyProperty TagProperty = DependencyProperty.RegisterAttached(
		"Tag",
		typeof(int),
		typeof(MetadataMissAttached),
		new PropertyMetadata(0));

	/// <summary>Gets the value of the MetadataMissAttached.Tag attached property.</summary>
	/// <param name="instance">The object to read from.</param>
	/// <returns>The attached value.</returns>
	public static int GetTag(DependencyObject instance) => (int)instance.GetValue(TagProperty);

	/// <summary>Sets the value of the MetadataMissAttached.Tag attached property.</summary>
	/// <param name="instance">The object to write to.</param>
	/// <param name="value">The value to attach.</param>
	public static void SetTag(DependencyObject instance, int value) => instance.SetValue(TagProperty, value);
}

/// <summary>
/// A metadata provider that answers for a named handful of types and returns null for everything
/// else, the way a partial generated provider does.
/// </summary>
public class StubBindableMetadataProvider : IBindableMetadataProvider
{
	private readonly IBindableType[] _knownTypes;

	/// <summary>Creates a provider that answers for the given types only.</summary>
	/// <param name="knownTypes">The metadata the provider claims to hold.</param>
	public StubBindableMetadataProvider(params IBindableType[] knownTypes)
		=> _knownTypes = knownTypes;

	/// <inheritdoc />
	public IBindableType GetBindableTypeByFullName(string fullName)
		=> Array.Find(_knownTypes, known => known.Type.FullName == fullName);

	/// <inheritdoc />
	public IBindableType GetBindableTypeByType(Type type)
		=> Array.Find(_knownTypes, known => known.Type == type);
}

/// <summary>Metadata for one type, listing at most one member.</summary>
public class StubBindableType : IBindableType
{
	private readonly string _name;
	private readonly IBindableProperty _property;

	/// <summary>Creates metadata for <paramref name="type"/>.</summary>
	/// <param name="type">The type described.</param>
	/// <param name="name">The single member listed, or null for a type with no listed members.</param>
	/// <param name="property">The metadata for that member, or null.</param>
	public StubBindableType(Type type, string name, IBindableProperty property)
	{
		Type = type;
		_name = name;
		_property = property;
	}

	/// <inheritdoc />
	public Type Type { get; }

	/// <inheritdoc />
	public ActivatorDelegate CreateInstance() => null;

	/// <inheritdoc />
	public IBindableProperty GetProperty(string name)
		=> _name != null && name == _name ? _property : null;

	/// <inheritdoc />
	public StringIndexerGetterDelegate GetIndexerGetter() => null;

	/// <inheritdoc />
	public StringIndexerSetterDelegate GetIndexerSetter() => null;
}

/// <summary>Metadata for one member, with an optional dependency property and setter.</summary>
public class StubBindableProperty : IBindableProperty
{
	/// <summary>Creates member metadata.</summary>
	/// <param name="propertyType">The member's type.</param>
	/// <param name="dependencyProperty">The dependency property the generator found, or null for a miss.</param>
	/// <param name="setter">The setter the generator emitted, or null for a miss.</param>
	public StubBindableProperty(Type propertyType, DependencyProperty dependencyProperty, PropertySetterHandler setter)
	{
		PropertyType = propertyType;
		DependencyProperty = dependencyProperty;
		Setter = setter;
	}

	/// <inheritdoc />
	public Type PropertyType { get; }

	/// <inheritdoc />
	public DependencyProperty DependencyProperty { get; }

	/// <inheritdoc />
	public PropertyGetterHandler Getter => null;

	/// <inheritdoc />
	public PropertySetterHandler Setter { get; }
}
