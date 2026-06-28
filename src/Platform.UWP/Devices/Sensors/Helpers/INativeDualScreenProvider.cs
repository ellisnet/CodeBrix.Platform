using System;
using Windows.UI.ViewManagement;

namespace CodeBrix.Platform.Devices.Sensors //Was previously: Uno.Devices.Sensors
{
	public interface INativeDualScreenProvider
	{
		bool? IsSpanned { get; }

		bool SupportsSpanning { get; }
	}
}
