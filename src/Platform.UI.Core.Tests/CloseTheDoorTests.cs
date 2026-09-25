#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeBrix.Platform.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Xaml;
using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SilverAssertions;
using Windows.Foundation;
using Windows.System.Profile;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for the Platform Core behaviour items WPE1-5 closed from the FIXLIST trackers (the Android track's findings
/// and the split's own): each test fails on the code before the fix.
/// </summary>
public class CloseTheDoorTests
{
	// ---------------------------------------------------------------- Application.PreloadFonts (AP1-B)

	[Theory]
	[InlineData("Segoe UI")]
	[InlineData("Open Sans")]
	[InlineData("Segoe UI#Segoe UI")]
	public void When_The_Default_Font_Family_Is_A_Plain_Name_Then_It_Is_Not_Treated_As_A_Font_Manifest(string family)
	{
		//Act
		var isManifest = FontFamilyHelper.TryGetFontManifestUri(family, out _);

		//Assert
		isManifest.Should().BeFalse();
	}

	[Fact]
	public void When_The_Default_Font_Family_Is_A_Package_Uri_Then_Its_Manifest_Is_Looked_Up()
	{
		//Act
		var isManifest = FontFamilyHelper.TryGetFontManifestUri("ms-appx:///Assets/Fonts/OpenSans.ttf", out var uri);

		//Assert
		isManifest.Should().BeTrue();
		uri.OriginalString.Should().Be("ms-appx:///Assets/Fonts/OpenSans.ttf");
	}

	// ---------------------------------------------------------------- SvgImageSource with no provider (AP1-C)

	[Fact]
	public void When_No_Svg_Provider_Is_Registered_Then_The_Missing_Package_Is_Reported_Once_And_Not_Per_Image()
	{
		//Arrange
		ApiExtensibility.IsRegistered<ISvgProvider>().Should().BeFalse("this suite registers no SVG provider");

		//Act
		var images = Enumerable.Range(0, 5).Select(_ => new SvgImageSource()).ToList();

		//Assert
		images.Count.Should().Be(5);
		SvgImageSource.SvgPackageMissingReports.Should().Be(1);
	}

	// ---------------------------------------------------------------- AnalyticsVersionInfo.DeviceFamily (AP1-B)

	private static FakeDeviceFamily? _deviceFamily;
	private static bool _deviceFamilyRegistered;

	[Fact]
	public void When_The_Platform_Names_Its_Operating_System_Family_Then_DeviceFamily_Starts_With_It()
	{
		//Arrange
		UseDeviceFamily(new FakeDeviceFamily("Android"));

		try
		{
			//Act
			var family = new AnalyticsVersionInfo().DeviceFamily;

			//Assert
			family.Should().Be($"Android.{AnalyticsInfo.DeviceForm}");
		}
		finally
		{
			UseDeviceFamily(null);
		}
	}

	[Fact]
	public void When_No_Device_Family_Platform_Is_Registered_Then_DeviceFamily_Keeps_The_Operating_System_Platform()
	{
		//Arrange
		UseDeviceFamily(null);

		//Act
		var family = new AnalyticsVersionInfo().DeviceFamily;

		//Assert
		family.Should().Be($"{Environment.OSVersion.Platform}.{AnalyticsInfo.DeviceForm}");
	}

	private static void UseDeviceFamily(FakeDeviceFamily? platform)
	{
		if (!_deviceFamilyRegistered)
		{
			// A builder that returns null means "not registered" to the registry.
			ApiExtensibility.Register(typeof(IDeviceFamilyPlatform), _ => _deviceFamily!);
			_deviceFamilyRegistered = true;
		}

		_deviceFamily = platform;
	}

	private sealed class FakeDeviceFamily(string family) : IDeviceFamilyPlatform
	{
		public string OperatingSystemFamily { get; } = family;
	}

	// ---------------------------------------------------------------- TextBlock.ActualWidth / ActualHeight (AP2)

	[Fact]
	public void When_A_TextBlock_With_A_Margin_Is_Laid_Out_Then_Its_Actual_Size_Is_Its_Render_Size_Without_The_Margin()
	{
		//Arrange
		var host = new Grid();
		host.Enter(new EnterParams(isLive: true), 0);
		var text = new TextBlock { Text = "Margins", Margin = new Thickness(20, 5, 20, 5), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		host.Children.Add(text);

		//Act
		host.Measure(new Size(400, 200));
		host.Arrange(new Rect(0, 0, 400, 200));

		//Assert
		text.ActualWidth.Should().Be(text.RenderSize.Width);
		text.ActualHeight.Should().Be(text.RenderSize.Height);
		text.ActualWidth.Should().Be(text.DesiredSize.Width - 40);
		text.ActualHeight.Should().Be(text.DesiredSize.Height - 10);
	}

	// ---------------------------------------------------------------- DependencyProperty name cache (WPC3)

	[Fact]
	public void When_Two_Threads_Look_Up_Dependency_Properties_By_Name_At_Once_Then_The_Name_Cache_Stays_Consistent()
	{
		//Arrange
		var previousCheck = FeatureConfiguration.DependencyProperty.DisableThreadingCheck;
		FeatureConfiguration.DependencyProperty.DisableThreadingCheck = true;
		var run = Guid.NewGuid().ToString("N");
		var types = new[] { typeof(Border), typeof(Grid), typeof(TextBlock), typeof(Button), typeof(StackPanel), typeof(ContentControl) };
		var lookups = Enumerable.Range(0, 4000).Select(i => (Type: types[i % types.Length], Name: $"Missing{run}_{i / types.Length}")).ToArray();
		var real = types.Select(t => (Type: t, Name: "Width")).ToArray();
		var failures = new List<Exception>();
		using var start = new Barrier(2);

		void LookUpAll()
		{
			try
			{
				start.SignalAndWait();
				foreach (var (type, name) in lookups)
				{
					DependencyProperty.GetProperty(type, name);
				}

				foreach (var (type, name) in real)
				{
					if (DependencyProperty.GetProperty(type, name) != FrameworkElement.WidthProperty)
					{
						throw new InvalidOperationException($"{type.Name}.{name} resolved to the wrong property.");
					}
				}
			}
			catch (Exception e)
			{
				lock (failures)
				{
					failures.Add(e);
				}
			}
		}

		try
		{
			//Act
			var threads = new[] { new Thread(LookUpAll), new Thread(LookUpAll) };
			foreach (var thread in threads)
			{
				thread.Start();
			}

			foreach (var thread in threads)
			{
				thread.Join();
			}
		}
		finally
		{
			FeatureConfiguration.DependencyProperty.DisableThreadingCheck = previousCheck;
		}

		//Assert
		failures.Should().BeEmpty();
	}

	// ---------------------------------------------------------------- Shape.StrokeMiterLimit default (AP2)

	[Fact]
	public void When_A_Shape_Is_Created_Then_Its_StrokeMiterLimit_Is_The_WinUI_Default_Of_10()
	{
		//Act
		var rectangle = new Rectangle();

		//Assert
		rectangle.StrokeMiterLimit.Should().Be(10.0);
		Shape.StrokeMiterLimitProperty.GetMetadata(typeof(Rectangle)).DefaultValue.Should().Be(10.0);
	}

	// ---------------------------------------------------------------- VisualState Setter through a shared brush (WPE1-1 FIXLIST)

	/// <summary>The ProgressBar template's UpdatingError setter target, as the XAML generator writes it.</summary>
	private const string SetterTargetPath = "(Microsoft.UI.Xaml.Shapes:Shape.Fill).(Microsoft.UI.Xaml.Media:SolidColorBrush.Color)";

	[Fact]
	public void When_A_Setter_Targets_The_Color_Of_A_Shared_Fill_Then_The_Shared_Brush_Is_Not_Written_And_Clearing_Restores_It()
	{
		//Arrange
		var shared = new SolidColorBrush(Colors.Blue);
		var indicator = new Rectangle { Fill = shared };
		var other = new Rectangle { Fill = shared };
		var caution = Color.FromArgb(0xFF, 0x9D, 0x5D, 0x00);
		var setter = new Setter { Target = new TargetPropertyPath(indicator, new PropertyPath(SetterTargetPath)), Value = caution };

		//Act
		setter.ApplyValue(indicator);
		var applied = (Fill: indicator.Fill, FillColor: ((SolidColorBrush)indicator.Fill).Color, SharedColor: shared.Color);
		setter.ClearValue();

		//Assert
		applied.SharedColor.Should().Be(Colors.Blue);
		applied.Fill.Should().NotBeSameAs(shared);
		applied.FillColor.Should().Be(caution);
		other.Fill.Should().BeSameAs(shared);
		indicator.Fill.Should().BeSameAs(shared);
		shared.Color.Should().Be(Colors.Blue);
	}

	[Fact]
	public void When_A_Setter_Replaces_A_Whole_Brush_Then_No_Copy_Is_Made()
	{
		//Arrange
		var shared = new SolidColorBrush(Colors.Blue);
		var replacement = new SolidColorBrush(Colors.Red);
		var indicator = new Rectangle { Fill = shared };
		var setter = new Setter { Target = new TargetPropertyPath(indicator, new PropertyPath("(Microsoft.UI.Xaml.Shapes:Shape.Fill)")), Value = replacement };

		//Act
		setter.ApplyValue(indicator);
		var applied = indicator.Fill;
		setter.ClearValue();

		//Assert
		applied.Should().BeSameAs(replacement);
		indicator.Fill.Should().BeSameAs(shared);
	}

	// ---------------------------------------------------------------- ApiExtensibility builders run outside the registry lock

	private interface IProbeOuterContract { }

	private interface IProbeInnerContract { }

	private sealed class ProbeContract : IProbeOuterContract, IProbeInnerContract { }

	[Fact]
	public void When_A_Contract_Builder_Waits_For_Another_Thread_That_Resolves_A_Contract_Then_Neither_Blocks()
	{
		//Arrange: the shape of the TextLayout suite deadlock - a builder (loading a platform assembly) waits for work on
		// another thread (that assembly's module initializer) which itself resolves a contract.
		var innerResolvedOnOtherThread = false;
		var otherThreadFinishedInTime = false;
		ApiExtensibility.Register(typeof(IProbeInnerContract), _ => new ProbeContract());
		ApiExtensibility.Register(typeof(IProbeOuterContract), _ =>
		{
			var other = new Thread(() => innerResolvedOnOtherThread = ApiExtensibility.CreateInstance<IProbeInnerContract>(typeof(IProbeInnerContract), out IProbeInnerContract? _));
			other.Start();
			otherThreadFinishedInTime = other.Join(TimeSpan.FromSeconds(5));
			return new ProbeContract();
		});

		//Act
		var outer = ApiExtensibility.CreateInstance<IProbeOuterContract>(typeof(IProbeOuterContract), out IProbeOuterContract? _);

		//Assert
		outer.Should().BeTrue();
		otherThreadFinishedInTime.Should().BeTrue("the other thread must not wait for the registry lock the builder's caller holds");
		innerResolvedOnOtherThread.Should().BeTrue();
	}

	// ---------------------------------------------------------------- editable ComboBox custom value (WPH1)

	[Fact]
	public void When_An_Editable_ComboBox_Without_A_Template_Commits_Text_That_Is_Not_An_Item_Then_It_Keeps_It_As_The_Custom_Value()
	{
		//Arrange
		var comboBox = new ComboBox { IsEditable = true };
		comboBox.Items.Add("alpha");
		comboBox.Items.Add("beta");
		comboBox.SelectedIndex = 0;

		//Act
		var handled = comboBox.RaiseTextSubmittedFromPlatform("gamma");

		//Assert
		handled.Should().BeFalse();
		comboBox.SelectedItem.Should().Be("gamma");
		comboBox.SelectedIndex.Should().Be(-1);
	}

	[Fact]
	public void When_A_ComboBox_Is_Not_Editable_Then_A_Value_That_Is_Not_An_Item_Is_Still_Refused()
	{
		//Arrange
		var comboBox = new ComboBox();
		comboBox.Items.Add("alpha");
		comboBox.SelectedIndex = 0;

		//Act
		comboBox.SelectedItem = "gamma";

		//Assert
		comboBox.SelectedItem.Should().Be("alpha");
	}
}
