#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using CodeBrix.Platform.Testing;
using CodeBrix.Platform.UI.RuntimeTests;
using CodeBrix.Platform.UI.RuntimeTests.Helpers;
using CodeBrix.Platform.UITest;

namespace SamplesApp.UITests;

// Note: All tests that are inheriting from this base class will run on UI thread.
public class SampleControlUITestBase : IInjectPointers
{
	protected RuntimeTestsApp App => RuntimeTestsApp.Current;

	/// <summary>
	/// Gets the default pointer type for the current platform
	/// </summary>
	public PointerDeviceType DefaultPointerType => App.DefaultPointerType;

	public PointerDeviceType CurrentPointerType => App.CurrentPointerType;

	protected async Task RunAsync(string metadataName, bool skipInitialScreenshot = true)
	{
		if (skipInitialScreenshot is not true)
		{
			throw new NotSupportedException("Initial screenshot is not supported by runtime tests.");
		}

		await App.RunAsync(metadataName);
	}

	void IInjectPointers.CleanupPointers()
		=> App.CleanupPointers();

	/// <inheritdoc />
	public IDisposable SetPointer(PointerDeviceType type)
		=> App.SetPointer(type);

	public ValueTask<RawBitmap> TakeScreenshotAsync(string name)
		=> App.TakeScreenshotAsync(name);
}
