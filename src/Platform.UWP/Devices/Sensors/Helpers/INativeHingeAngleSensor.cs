using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.Devices.Sensors.Helpers;
using Windows.Devices.Sensors;
using Windows.Foundation;

namespace CodeBrix.Platform.Devices.Sensors //Was previously: Uno.Devices.Sensors
{
	public interface INativeHingeAngleSensor
	{
		bool DeviceHasHinge { get; }

		event EventHandler<NativeHingeAngleReading> ReadingChanged;
	}
}
