using CodeBrix.Platform.UI.Dispatching;

namespace Windows.UI.Core
{
	public sealed partial class CoreDispatcher
	{
		public void ProcessEvents(CoreProcessEventsOption options)
			=> _inner.ProcessEvents((CodeBrix.Platform.UI.Dispatching.CoreProcessEventsOption)options);
	}
}
