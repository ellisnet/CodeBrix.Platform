using System;
using System.Reflection;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// Resolution order for one executing test: theory-row trait > method attribute > null (inherit the
// fixture's launch preference).
public sealed class PlayTestOrientationAttributeTests
{
    [Theory]
    [InlineData(ScreenOrientation.Landscape)]
    [InlineData(ScreenOrientation.Portrait)]
    public void Constructor_keeps_the_required_orientation(ScreenOrientation orientation)
        => new PlayTestOrientationAttribute(orientation).Orientation.Should().Be(orientation);

    [Fact]
    public void Constructor_rejects_an_undefined_orientation()
    {
        //Act
        Action create = () => _ = new PlayTestOrientationAttribute((ScreenOrientation)5);

        //Assert
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CaseTraitName_is_the_documented_trait_key()
        => PlayTestOrientationAttribute.CaseTraitName.Should().Be("PlayTestOrientation");

    [Fact]
    public void Resolve_returns_null_without_any_requirement()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Unannotated))).Should().BeNull();

    [Fact]
    public void Resolve_returns_the_method_requirement()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Portrait))).Should().Be(ScreenOrientation.Portrait);

    [Fact]
    public void Resolve_lets_a_row_override_the_method_requirement()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Portrait)), new[] { "landscape" })
            .Should().Be(ScreenOrientation.Landscape);

    [Fact]
    public void Resolve_uses_a_row_requirement_without_a_method_attribute()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Unannotated)), new[] { "PORTRAIT" })
            .Should().Be(ScreenOrientation.Portrait);

    [Fact]
    public void Resolve_treats_an_empty_row_list_as_no_row_requirement()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Portrait)), Array.Empty<string>())
            .Should().Be(ScreenOrientation.Portrait);

    [Fact]
    public void Resolve_accepts_repeated_identical_row_values()
        => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Unannotated)), new[] { "Portrait", "portrait" })
            .Should().Be(ScreenOrientation.Portrait);

    [Fact]
    public void Resolve_rejects_conflicting_row_requirements()
    {
        //Act
        Action resolve = () => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Portrait)), new[] { "Portrait", "Landscape" });

        //Assert
        resolve.Should().Throw<ArgumentException>().WithMessage("*both Landscape and Portrait*");
    }

    [Fact]
    public void Resolve_rejects_an_unknown_row_value_naming_the_trait()
    {
        //Act
        Action resolve = () => PlayTestOrientationAttribute.Resolve(Method(nameof(Samples.Unannotated)), new[] { "square" });

        //Assert
        resolve.Should().Throw<ArgumentException>().WithMessage("*PlayTestOrientation*square*");
    }

    private static MethodInfo Method(string name)
        => typeof(Samples).GetMethod(name) ?? throw new InvalidOperationException("Missing sample method " + name);

    private sealed class Samples
    {
        public void Unannotated() { }

        [PlayTestOrientation(ScreenOrientation.Portrait)]
        public void Portrait() { }
    }
}
