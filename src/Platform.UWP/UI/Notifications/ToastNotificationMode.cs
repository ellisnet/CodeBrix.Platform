namespace Windows.UI.Notifications
{
	// This enum (Windows SDK 10.0.22621 and later) is not in the metadata the API sync generator compares against
	// (Windows SDK 10.0.22000 contracts), so a regeneration would delete it from Generated/. It is kept here, unchanged,
	// so the public API does not lose it.

	/// <summary>
	/// Specifies the toast notification mode (the Focus Assist state) of a user.
	/// </summary>
	public enum ToastNotificationMode
	{
		/// <summary>All toast notifications are shown.</summary>
		Unrestricted = 0,

		/// <summary>Only priority toast notifications are shown.</summary>
		PriorityOnly = 1,

		/// <summary>Only alarm toast notifications are shown.</summary>
		AlarmsOnly = 2,
	}
}
