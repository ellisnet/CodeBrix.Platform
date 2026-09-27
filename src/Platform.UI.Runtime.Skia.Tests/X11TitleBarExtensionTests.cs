using System.Reflection;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// WPE1-13 (g, option b; Jeremy's GO 2026-09-26): on X11, ExtendsContentIntoTitleBar keeps the window manager's
/// decorations (as on macOS): the X11 window wrapper no longer overrides ExtendContentIntoTitleBar (the base is a no-op)
/// and the X11 root host no longer has the Motif-hint removal it used to call.
/// </summary>
public class X11TitleBarExtensionTests
{
	private const string X11Assembly = "CodeBrix.Platform.UI.Runtime.Skia.X11";

	[Fact]
	public void the_x11_window_wrapper_leaves_extend_content_into_title_bar_to_the_no_op_base()
	{
		//Arrange
		var wrapper = Assembly.Load(X11Assembly).GetType("CodeBrix.Platform.WinUI.Runtime.Skia.X11.X11WindowWrapper", throwOnError: true)!;

		//Act
		var method = wrapper.GetMethod("ExtendContentIntoTitleBar", BindingFlags.Public | BindingFlags.Instance, [typeof(bool)])!;

		//Assert
		method.DeclaringType!.Name.Should().Be("NativeWindowWrapperBase");
	}

	[Fact]
	public void the_x11_root_host_has_no_decoration_removal_for_extend_content_into_title_bar()
	{
		//Arrange
		var host = Assembly.Load(X11Assembly).GetType("CodeBrix.Platform.WinUI.Runtime.Skia.X11.X11XamlRootHost", throwOnError: true)!;

		//Act
		var method = host.GetMethod("ExtendContentIntoTitleBar", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

		//Assert
		method.Should().BeNull();
	}
}
