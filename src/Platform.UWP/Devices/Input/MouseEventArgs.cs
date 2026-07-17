#nullable enable

namespace Windows.Devices.Input
{
	public partial class MouseEventArgs
	{
		internal MouseEventArgs(MouseDelta mouseDelta)
		{
			MouseDelta = mouseDelta;
		}

		public MouseDelta MouseDelta { get; }
	}
}
