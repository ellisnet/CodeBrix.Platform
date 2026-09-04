using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The SkiaSharp-bump guard. Nothing here constructs a XAML object, so these tests run even if
/// every other test in the suite is broken by a framework change - they are the first thing to
/// look at after $(SkiaSharpVersion) moves in src/Directory.Build.targets.
/// </summary>
public class SkiaSharpVersionAgreementTests
{
	[Fact]
	public void native_library_is_compatible_with_the_managed_assembly()
	{
		//Arrange
		//Nothing: the check reads the loaded libSkiaSharp, which the NativeAssets package for
		//this OS put beside the test binary. A partial bump (managed moved, native did not, or
		//the reverse) fails HERE rather than inside a running application.

		//Act
		var mismatch = Record.Exception(() =>
		{
			SkiaSharpVersion.CheckNativeLibraryCompatible(throwIfIncompatible: true);
			SkiaSharpVersion.Native.Should().BeGreaterThanOrEqualTo(SkiaSharpVersion.NativeMinimum);
		});

		//Assert
		//A partial bump surfaces as SkiaSharp's own message - measured by swapping a 4.150.1
		//libSkiaSharp under a 4.151.0 managed assembly: "The version of the native libSkiaSharp
		//library (150.0) is incompatible with this version of SkiaSharp. Supported versions of the
		//native libSkiaSharp library are in the range [151.0, 152.0)." It arrives wrapped in a
		//TypeInitializationException, because reading the native version is the first thing that
		//touches the library at all, so the call is wrapped here to keep the message readable.
		mismatch.Should().BeNull("the SkiaSharp.NativeAssets package for this OS must deliver a "
			+ "libSkiaSharp the managed SkiaSharp assembly accepts");
	}

	[Fact]
	public void the_native_library_is_the_one_this_test_project_asked_for()
	{
		//Arrange
		//Act
		//Assert
		//A NativeAssets package that failed to deliver anything would have made the compatibility
		//check throw a DllNotFoundException above; this states the positive case plainly, so the
		//failure message names the runtime that is missing rather than a P/Invoke stack.
		SkiaSharpVersion.Native.Should().NotBe(new Version(0, 0, 0),
			$"a libSkiaSharp for {RuntimeInformation.RuntimeIdentifier} must be next to the test binary");
	}

	[Fact]
	public void the_addin_assembly_version_tracks_the_skiasharp_it_was_built_against()
	{
		//Arrange
		var addIn = typeof(SKXamlCanvas).Assembly.GetName().Version!;
		var skiaSharp = typeof(SKObject).Assembly.GetName().Version!;

		//Act
		var addInTriple = new Version(addIn.Major, addIn.Minor, addIn.Build);
		var skiaSharpTriple = new Version(skiaSharp.Major, skiaSharp.Minor, skiaSharp.Build);

		//Assert
		//The add-in's <Version> is deliberately tied to the SkiaSharp it vendors (see the comment
		//in CodeBrix.Platform.SkiaSharp.Views.Skia.csproj); the fourth segment is reserved for
		//glue-only fixes between SkiaSharp bumps, so only Major.Minor.Build is compared. A bump of
		//$(SkiaSharpVersion) that forgets the two csproj <Version>/<PackageVersion> literals fails
		//here. (The nuspec's literal version is not visible to a test - check it by hand.)
		addInTriple.Should().Be(skiaSharpTriple,
			$"the add-in assembly version {addIn} must track SkiaSharp {skiaSharp}: bump the "
			+ "<Version>/<PackageVersion> literals in both add-in csprojs (and the nuspec) with "
			+ "$(SkiaSharpVersion) in src/Directory.Build.targets");
	}

	[Fact]
	public void the_addin_informational_version_tracks_the_skiasharp_it_was_built_against()
	{
		//Arrange
		var skiaSharp = typeof(SKObject).Assembly.GetName().Version!;
		var expectedPrefix = $"{skiaSharp.Major}.{skiaSharp.Minor}.{skiaSharp.Build}";

		//Act
		var informational = typeof(SKXamlCanvas).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		//Assert
		//AssemblyVersion and AssemblyInformationalVersion come from different csproj properties and
		//can drift apart independently, so both are covered.
		informational.Should().NotBeNull();
		informational!.StartsWith(expectedPrefix, StringComparison.Ordinal).Should().BeTrue(
			$"the add-in's informational version '{informational}' must start with the SkiaSharp "
			+ $"version it vendors ('{expectedPrefix}')");
	}

	[Fact]
	public void a_raster_surface_can_be_created_over_pinned_memory_and_flushed()
	{
		//Arrange
		//This is the exact call shape SKXamlCanvas.Skia.cs uses to paint: a BGRA premultiplied
		//SKImageInfo, an SKSurface over memory the caller owns, a draw, a Flush. A SkiaSharp
		//signature or behaviour change fails here with a readable message instead of inside the
		//control's present path.
		var info = new SKImageInfo(4, 4, SKColorType.Bgra8888, SKAlphaType.Premul);
		var pixels = new byte[info.BytesSize];
		var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

		try
		{
			//Act
			using var surface = SKSurface.Create(info, handle.AddrOfPinnedObject(), info.RowBytes);
			surface.Should().NotBeNull();
			surface.Canvas.Clear(SKColors.Red);
			surface.Flush();

			//Assert
			info.RowBytes.Should().Be(16);
			info.BytesSize.Should().Be(64);
			//Opaque red in BGRA premultiplied byte order is B=0, G=0, R=255, A=255.
			pixels[0].Should().Be(0);
			pixels[1].Should().Be(0);
			pixels[2].Should().Be(255);
			pixels[3].Should().Be(255);
			//Every one of the sixteen pixels, not just the first.
			for (var i = 0; i < pixels.Length; i += 4)
			{
				pixels[i + 2].Should().Be(255);
				pixels[i + 3].Should().Be(255);
			}
		}
		finally
		{
			handle.Free();
		}
	}

	[Fact]
	public void image_info_with_size_keeps_the_colour_and_alpha_types()
	{
		//Arrange
		//SKXamlCanvas builds the user-visible SKImageInfo with info.WithSize(userVisibleSize); if
		//WithSize ever stopped carrying the colour type across, the control would hand the handler
		//an info that does not describe the surface it is drawing on.
		var info = new SKImageInfo(400, 200, SKColorType.Bgra8888, SKAlphaType.Premul);

		//Act
		var resized = info.WithSize(new SKSizeI(200, 100));

		//Assert
		resized.Width.Should().Be(200);
		resized.Height.Should().Be(100);
		resized.ColorType.Should().Be(SKColorType.Bgra8888);
		resized.AlphaType.Should().Be(SKAlphaType.Premul);
	}

	[Fact]
	public void premultiply_still_answers_for_an_opaque_colour()
	{
		//Arrange
		//SKPMColor.PreMultiply is the one SkiaSharp call EnvironmentExtensions makes; it is
		//covered here as an API-shape guard as well as in EnvironmentExtensionsTests, which is
		//about the probe's behaviour rather than the call.

		//Act
		var premultiplied = SKPMColor.PreMultiply(SKColors.Black);

		//Assert
		premultiplied.Alpha.Should().Be(255);
		premultiplied.Red.Should().Be(0);
	}
}
