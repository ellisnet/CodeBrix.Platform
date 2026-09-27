namespace Windows.UI.Notifications
{
	// NotificationMode and NotificationModeChanged (Windows SDK 10.0.22621 and later) are not in the metadata the API sync
	// generator compares against (Windows SDK 10.0.22000 contracts), so a regeneration would delete them from Generated/.
	// They are kept here, unchanged (not implemented). Because this hand-written part exists, the generator no longer
	// emits the type-level NotImplemented marker and the internal constructor, so both are declared here as well.

	/// <summary>
	/// Creates toast notifiers for a specific user (not implemented).
	/// </summary>
	[global::CodeBrix.Platform.NotImplemented]
	public partial class ToastNotificationManagerForUser
	{
		internal ToastNotificationManagerForUser()
		{
		}

		/// <summary>
		/// Gets the toast notification mode of the user (not implemented).
		/// </summary>
		[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
		public global::Windows.UI.Notifications.ToastNotificationMode NotificationMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToastNotificationMode ToastNotificationManagerForUser.NotificationMode is not implemented. For more information, visit https://github.com/ellisnet/CodeBrix.Platform/blob/main/NOT-IMPLEMENTED.md");
			}
		}

		/// <summary>
		/// Occurs when the toast notification mode of the user changes (not implemented).
		/// </summary>
		[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Notifications.ToastNotificationManagerForUser, object> NotificationModeChanged
		{
			[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotificationManagerForUser", "event TypedEventHandler<ToastNotificationManagerForUser, object> ToastNotificationManagerForUser.NotificationModeChanged");
			}
			[global::CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__", "__CODEBRIX_CORE__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotificationManagerForUser", "event TypedEventHandler<ToastNotificationManagerForUser, object> ToastNotificationManagerForUser.NotificationModeChanged");
			}
		}
	}
}
