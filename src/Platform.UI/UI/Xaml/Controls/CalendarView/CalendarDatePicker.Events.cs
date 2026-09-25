using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarDatePicker
	{
		public event EventHandler<object> Opened;

		public event EventHandler<object> Closed;

		public event TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs> DateChanged;

		/// <summary>
		/// Raise entry point for a platform handler: its native calendar dialog opened (the path the flyout's Opened
		/// takes on the template path: IsCalendarOpen becomes true, then Opened is raised).
		/// </summary>
		internal void RaiseOpenedFromPlatform()
		{
			IsCalendarOpen = true;
			Opened?.Invoke(this, new object());
		}

		/// <summary>
		/// Raise entry point for a platform handler: its native calendar dialog closed (the path the flyout's Closed
		/// takes on the template path: IsCalendarOpen becomes false, then Closed is raised).
		/// </summary>
		internal void RaiseClosedFromPlatform()
		{
			IsCalendarOpen = false;
			Closed?.Invoke(this, new object());
		}

		private protected override void OnUnloaded()
		{
			// Ensure flyout is closed when the control is unloaded
			IsCalendarOpen = false;

			base.OnUnloaded();
		}
	}
}
