using System;

namespace CodeBrix.Platform.Devices.Sensors.Helpers //Was previously: Uno.Devices.Sensors.Helpers
{
	public class NativeHingeAngleReading
	{
		public NativeHingeAngleReading(double angleInDegrees, DateTimeOffset timestamp)
		{
			AngleInDegrees = angleInDegrees;
			Timestamp = timestamp;
		}

		public double AngleInDegrees { get; }

		public DateTimeOffset Timestamp { get; }
	}
}
