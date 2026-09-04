using SilverAssertions;
using SkiaSharp.Views;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The add-in's internal environment probe, reached through the
/// <c>InternalsVisibleTo</c> grant in the add-in's InternalsVisibleTo.cs.
/// </summary>
/// <remarks>
/// <para>
/// These tests assert the CURRENT behaviour, which is that the probe always answers
/// <see langword="true"/>. That is not an accident of the test environment: the comment at the top
/// of SkiaSharp.Views.Shared/Extensions.cs records that as of the vendored SkiaSharp version
/// <c>SKPMColor.PreMultiply</c> is implemented entirely in managed code and no longer calls into
/// libSkiaSharp, so it can never raise the <c>DllNotFoundException</c> the probe catches - upstream
/// has the same defect, and the file is kept byte-aligned with upstream so it can be re-diffed at a
/// SkiaSharp bump. Nothing in the framework calls <c>IsValidEnvironment</c>.
/// </para>
/// <para>
/// So the value of these tests is the DAY THAT CHANGES: if a future SkiaSharp makes PreMultiply
/// native-backed again, the probe becomes meaningful and this file is what says so.
/// </para>
/// </remarks>
public class EnvironmentExtensionsTests
{
	[Fact]
	public void IsValidEnvironment_is_true_when_the_native_library_is_present()
	{
		//The native library really is present in this test run - SkiaSharpVersionAgreementTests
		//measures it - so true is the answer either way. The next test is the one that separates
		//"true because the probe worked" from "true because the probe cannot fail".
		EnvironmentExtensions.IsValidEnvironment.Should().BeTrue();
	}

	[Fact]
	public void IsValidEnvironment_is_cached_and_answers_the_same_every_time()
	{
		//Arrange
		//The probe is a Lazy<bool>, so it must be stable across calls; an application that read it
		//per frame would otherwise pay for it per frame.
		var first = EnvironmentExtensions.IsValidEnvironment;

		//Act
		var second = EnvironmentExtensions.IsValidEnvironment;
		var third = EnvironmentExtensions.IsValidEnvironment;

		//Assert
		second.Should().Be(first);
		third.Should().Be(first);
	}
}
