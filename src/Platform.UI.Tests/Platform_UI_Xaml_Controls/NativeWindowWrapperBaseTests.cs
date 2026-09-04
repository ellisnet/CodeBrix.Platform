using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Tests.Platform_UI_Xaml_Controls;

/// <summary>
/// Fences the framed-versus-client size contract every desktop head relies on: AppWindow.Size is the
/// framed size, AppWindow.ClientSize is the client area, and Resize takes a framed size, so a head has
/// to subtract the frame extents before it asks the windowing system for a size. Collapsing the two
/// sizes onto one value is what made AppWindow.Size and AppWindow.Resize disagree by the window
/// manager's frame on the X11 head, so an application that saved the one and restored with the other
/// grew its window at every launch.
/// </summary>
[TestClass]
public class NativeWindowWrapperBaseTests
{
	[TestMethod]
	public void SetSizes_reports_the_framed_and_the_client_size_independently()
	{
		//Arrange
		var wrapper = new TestNativeWindowWrapper();

		//Act
		wrapper.SetSizesForTest(new SizeInt32 { Width = 1220, Height = 850 }, new SizeInt32 { Width = 1200, Height = 800 });

		//Assert
		Assert.AreEqual(1220, wrapper.Size.Width);
		Assert.AreEqual(850, wrapper.Size.Height);
		Assert.AreEqual(1200, wrapper.ClientSize.Width);
		Assert.AreEqual(800, wrapper.ClientSize.Height);
	}

	[TestMethod]
	public void HasNonClientFrame_is_false_while_the_two_sizes_are_equal()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1024, Height = 640 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var result = NativeWindowWrapperBase.HasNonClientFrame(framed, client);

		//Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void HasNonClientFrame_is_true_once_the_framed_size_is_larger()
	{
		//Arrange
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var result = NativeWindowWrapperBase.HasNonClientFrame(framed, client);

		//Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void ToClientSize_subtracts_the_frame_extents()
	{
		//Arrange
		var requested = new SizeInt32 { Width = 1220, Height = 850 };
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var result = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		Assert.AreEqual(1200, result.Width);
		Assert.AreEqual(800, result.Height);
	}

	[TestMethod]
	public void ToClientSize_returns_the_requested_size_when_no_frame_is_known()
	{
		//Arrange
		var requested = new SizeInt32 { Width = 1200, Height = 760 };
		var framed = new SizeInt32 { Width = 1024, Height = 640 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var result = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		Assert.AreEqual(1200, result.Width);
		Assert.AreEqual(760, result.Height);
	}

	[TestMethod]
	public void ToClientSize_clamps_each_dimension_to_at_least_one_pixel()
	{
		//Arrange
		var requested = new SizeInt32 { Width = 10, Height = 10 };
		var framed = new SizeInt32 { Width = 1044, Height = 690 };
		var client = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var result = NativeWindowWrapperBase.ToClientSize(requested, framed, client);

		//Assert
		Assert.AreEqual(1, result.Width);
		Assert.AreEqual(1, result.Height);
	}

	[TestMethod]
	public void ToClientSize_round_trips_the_size_the_wrapper_reports()
	{
		//Arrange
		var wrapper = new TestNativeWindowWrapper();
		wrapper.SetSizesForTest(new SizeInt32 { Width = 1044, Height = 690 }, new SizeInt32 { Width = 1024, Height = 640 });

		//Act
		var result = wrapper.ToClientSize(wrapper.Size);

		//Assert
		Assert.AreEqual(wrapper.ClientSize.Width, result.Width);
		Assert.AreEqual(wrapper.ClientSize.Height, result.Height);
	}

	private sealed class TestNativeWindowWrapper : NativeWindowWrapperBase
	{
		public override object NativeWindow => null;

		public void SetSizesForTest(SizeInt32 size, SizeInt32 clientSize) => SetSizes(size, clientSize);
	}
}
