namespace Windows.ApplicationModel.Contacts
{
	public enum ContactStoreAccessType
	{
		AllContactsReadOnly = 1,

#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		[global::CodeBrix.Platform.NotImplemented]
		AppContactsReadWrite = 0,
#endif

#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		[global::CodeBrix.Platform.NotImplemented]
		AllContactsReadWrite = 2,
#endif
	}
}
