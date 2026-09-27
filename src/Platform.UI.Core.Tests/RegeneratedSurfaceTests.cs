#nullable enable

using System;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Notifications;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-12 (decision D9, the real regeneration of Generated/): EllipseGeometry and LineGeometry are implemented
/// (bounds, change notification, no NotImplemented marker), and every hand edit that used to live inside a Generated
/// file - now in hand-written files so a regeneration cannot revert it - still holds: the UIElement access-key members
/// (AccessKey wiring, ExitDisplayModeOnAccessKeyInvoked default true), the non-public constructors of AccessKeyManager
/// and MouseDevice, MouseDelta without a NotImplemented marker, the NotImplemented markers of VirtualizingStackPanel and
/// the system backdrops, and the Windows SDK 22621 toast members the generator's metadata does not have.
/// </summary>
public class RegeneratedSurfaceTests
{
	// ---------------------------------------------------------------- EllipseGeometry

	[Fact]
	public void When_An_EllipseGeometry_Is_Created_Then_It_Has_The_WinUI_Defaults_And_Is_Implemented()
	{
		//Arrange
		var ellipse = new EllipseGeometry();

		//Assert
		ellipse.Center.Should().Be(new Point(0, 0));
		ellipse.RadiusX.Should().Be(0);
		ellipse.RadiusY.Should().Be(0);
		ellipse.Bounds.Should().Be(new Rect(0, 0, 0, 0));
		IsMarkedNotImplemented(typeof(EllipseGeometry)).Should().BeFalse();
		typeof(EllipseGeometry).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(IsMarkedNotImplemented).Select(m => m.Name).Should().BeEmpty();
	}

	[Fact]
	public void When_An_EllipseGeometry_Has_A_Center_And_Radii_Then_Its_Bounds_Are_The_Box_It_Fills()
	{
		//Arrange
		var ellipse = new EllipseGeometry { Center = new Point(50, 40), RadiusX = 30, RadiusY = 20 };

		//Assert
		ellipse.Bounds.Should().Be(new Rect(20, 20, 60, 40));
	}

	[Fact]
	public void When_An_EllipseGeometry_Has_A_Transform_Then_Its_Bounds_Are_Transformed()
	{
		//Arrange
		var ellipse = new EllipseGeometry
		{
			Center = new Point(10, 10),
			RadiusX = 10,
			RadiusY = 5,
			Transform = new TranslateTransform { X = 100, Y = 200 },
		};

		//Assert
		ellipse.Bounds.Should().Be(new Rect(100, 205, 20, 10));
	}

	[Fact]
	public void When_A_Property_Of_An_EllipseGeometry_Changes_Then_The_Geometry_Reports_A_Change()
	{
		//Arrange
		var ellipse = new EllipseGeometry();
		var changes = 0;
		ellipse.GeometryChanged += () => changes++;

		//Act
		ellipse.Center = new Point(5, 5);
		ellipse.RadiusX = 3;
		ellipse.RadiusY = 4;

		//Assert
		changes.Should().Be(3);
	}

	// ---------------------------------------------------------------- LineGeometry

	[Fact]
	public void When_A_LineGeometry_Is_Created_Then_It_Has_The_WinUI_Defaults_And_Is_Implemented()
	{
		//Arrange
		var line = new LineGeometry();

		//Assert
		line.StartPoint.Should().Be(new Point(0, 0));
		line.EndPoint.Should().Be(new Point(0, 0));
		line.Bounds.Should().Be(new Rect(0, 0, 0, 0));
		IsMarkedNotImplemented(typeof(LineGeometry)).Should().BeFalse();
		typeof(LineGeometry).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(IsMarkedNotImplemented).Select(m => m.Name).Should().BeEmpty();
	}

	[Fact]
	public void When_A_LineGeometry_Goes_Up_And_Left_Then_Its_Bounds_Hold_Both_Ends()
	{
		//Arrange
		var line = new LineGeometry { StartPoint = new Point(80, 10), EndPoint = new Point(20, 50) };

		//Assert
		line.Bounds.Should().Be(new Rect(20, 10, 60, 40));
	}

	[Fact]
	public void When_A_Point_Of_A_LineGeometry_Changes_Then_The_Geometry_Reports_A_Change()
	{
		//Arrange
		var line = new LineGeometry();
		var changes = 0;
		line.GeometryChanged += () => changes++;

		//Act
		line.StartPoint = new Point(1, 2);
		line.EndPoint = new Point(3, 4);

		//Assert
		changes.Should().Be(2);
	}

	// ---------------------------------------------------------------- hand edits moved out of Generated/

	[Fact]
	public void When_A_UIElement_Is_Created_Then_Its_Access_Key_Members_Have_The_WinUI_Defaults_And_Are_Implemented()
	{
		//Arrange
		var element = new Border();

		//Assert
		element.ExitDisplayModeOnAccessKeyInvoked.Should().BeTrue();
		element.AccessKey.Should().BeNull();
		element.IsAccessKeyScope.Should().BeFalse();
		element.AccessKeyScopeOwner.Should().BeNull();
		foreach (var name in new[] { nameof(UIElement.AccessKey), nameof(UIElement.ExitDisplayModeOnAccessKeyInvoked), nameof(UIElement.IsAccessKeyScope), nameof(UIElement.AccessKeyScopeOwner) })
		{
			IsMarkedNotImplemented(typeof(UIElement).GetProperty(name)!).Should().BeFalse(name);
			IsMarkedNotImplemented(typeof(UIElement).GetProperty(name + "Property")!).Should().BeFalse(name + "Property");
		}
	}

	[Fact]
	public void When_An_Access_Key_Is_Set_Then_The_Element_Registers_With_The_AccessKeyManager()
	{
		//Arrange
		var element = new Border();
		var before = RegisteredCount();

		//Act
		element.AccessKey = "Q";
		var whileSet = RegisteredCount();
		element.AccessKey = "";

		//Assert
		whileSet.Should().Be(before + 1);
		RegisteredCount().Should().Be(before);
		GC.KeepAlive(element);
	}

	[Fact]
	public void When_The_Surface_Is_Regenerated_Then_Types_Without_A_Public_Constructor_Still_Have_None()
	{
		//Assert
		typeof(AccessKeyManager).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
		typeof(MouseDevice).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
		typeof(ToastNotificationManagerForUser).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Should().BeEmpty();
	}

	[Fact]
	public void When_The_Surface_Is_Regenerated_Then_The_NotImplemented_Markers_Are_Unchanged()
	{
		//Assert
		IsMarkedNotImplemented(typeof(MouseDelta)).Should().BeFalse();
		IsMarkedNotImplemented(typeof(VirtualizingStackPanel)).Should().BeTrue();
		IsMarkedNotImplemented(typeof(SystemBackdrop)).Should().BeTrue();
		IsMarkedNotImplemented(typeof(MicaBackdrop)).Should().BeTrue();
		IsMarkedNotImplemented(typeof(DesktopAcrylicBackdrop)).Should().BeTrue();
		IsMarkedNotImplemented(typeof(ToastNotificationManagerForUser)).Should().BeTrue();
		IsMarkedNotImplemented(typeof(ToastNotificationManagerForUser).GetProperty(nameof(ToastNotificationManagerForUser.NotificationMode))!).Should().BeTrue();
		Enum.GetNames<ToastNotificationMode>().Should().Equal("Unrestricted", "PriorityOnly", "AlarmsOnly");
	}

	/// <summary>
	/// WPE1-13 (h): the 34 types whose type-level NotImplemented marker used to name the flavours explicitly carry the
	/// generator's BARE marker (WPE1-12's NotImplementedTypeMarkers.cs preservation files are gone), so the Uno0001
	/// analyzer warns a consuming app that uses one of them. Their generated internal constructors, public
	/// constructor stubs and HostName.ToString stub are the generator's own again.
	/// </summary>
	[Fact]
	public void When_The_Surface_Is_Regenerated_Then_The_34_Type_Markers_Are_Bare_So_Apps_Are_Warned()
	{
		//Arrange
		string[] typeNames =
		{
			"Windows.ApplicationModel.Appointments.AppointmentManager, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Appointments.AppointmentStore, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Background.BackgroundTaskDeferral, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Calls.PhoneCallHistoryEntryReader, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Calls.PhoneCallHistoryManager, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Calls.PhoneCallHistoryStore, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Calls.PhoneCallManager, CodeBrix.Platform.Core",
			"Windows.ApplicationModel.Chat.ChatMessageManager, CodeBrix.Platform.Core",
			"Windows.Data.Pdf.PdfDocument, CodeBrix.Platform.Core",
			"Windows.Data.Pdf.PdfPage, CodeBrix.Platform.Core",
			"Windows.Data.Pdf.PdfPageDimensions, CodeBrix.Platform.Core",
			"Windows.Data.Pdf.PdfPageRenderOptions, CodeBrix.Platform.Core",
			"Windows.Devices.Geolocation.Geolocator, CodeBrix.Platform.Core",
			"Windows.Devices.Midi.IMidiOutPort, CodeBrix.Platform.Core",
			"Windows.Devices.Power.Battery, CodeBrix.Platform.Core",
			"Windows.Devices.Power.BatteryReport, CodeBrix.Platform.Core",
			"Windows.Devices.Radios.Radio, CodeBrix.Platform.Core",
			"Windows.Devices.Sensors.HingeAngleReading, CodeBrix.Platform.Core",
			"Windows.Devices.Sensors.HingeAngleSensorReadingChangedEventArgs, CodeBrix.Platform.Core",
			"Windows.Gaming.Input.IGameController, CodeBrix.Platform.Core",
			"Windows.Media.SpeechRecognition.SpeechRecognitionResult, CodeBrix.Platform.Core",
			"Windows.Media.SpeechRecognition.SpeechRecognizer, CodeBrix.Platform.Core",
			"Windows.Networking.Connectivity.ConnectionCost, CodeBrix.Platform.Core",
			"Windows.Networking.Connectivity.IPInformation, CodeBrix.Platform.Core",
			"Windows.Networking.HostName, CodeBrix.Platform.Core",
			"Windows.Phone.Devices.Notification.VibrationDevice, CodeBrix.Platform.Core",
			"Windows.Services.Maps.MapLocationFinder, CodeBrix.Platform.Core",
			"Windows.Storage.KnownFolders, CodeBrix.Platform.Core",
			"Windows.System.Display.DisplayRequest, CodeBrix.Platform.Core",
			"Windows.UI.StartScreen.JumpListItem, CodeBrix.Platform.Core",
			"Windows.UI.ViewManagement.StatusBar, CodeBrix.Platform.Core",
			"Microsoft.UI.Xaml.Controls.ItemsWrapGrid, CodeBrix.Platform.UI.Core",
			"Microsoft.UI.Xaml.Controls.Primitives.GridViewItemPresenter, CodeBrix.Platform.UI.Core",
			"Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter, CodeBrix.Platform.UI.Core",
		};

		//Act
		var notBare = typeNames
			.Select(n => Type.GetType(n, throwOnError: true)!)
			.Where(t => t.GetCustomAttribute<NotImplementedAttribute>() is not { } marker || marker.Platforms is { Length: > 0 })
			.Select(t => t.FullName)
			.ToArray();

		//Assert
		typeNames.Length.Should().Be(34);
		notBare.Should().BeEmpty();
		typeof(Windows.Networking.HostName).GetMethod(nameof(ToString), Type.EmptyTypes)!.DeclaringType.Should().Be(typeof(Windows.Networking.HostName));
		typeof(Windows.Devices.Geolocation.Geolocator).GetConstructor(Type.EmptyTypes)!.GetCustomAttribute<NotImplementedAttribute>().Should().NotBeNull();
	}

	private static bool IsMarkedNotImplemented(MemberInfo member)
		=> member.GetCustomAttributes(typeof(NotImplementedAttribute), false).Length > 0;

	private static int RegisteredCount()
		=> (int)typeof(AccessKeyManager).GetField("_registeredCount", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
}
