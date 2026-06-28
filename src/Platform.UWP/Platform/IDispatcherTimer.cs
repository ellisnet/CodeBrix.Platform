using System;

namespace CodeBrix.Platform; //Was previously: Uno

internal interface IDispatcherTimer
{
	TimeSpan Interval { get; set; }

	bool IsEnabled { get; }

	event EventHandler<object> Tick;

	void Start();

	void Stop();
}
