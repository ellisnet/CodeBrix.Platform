#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
	[global::CodeBrix.Platform.NotImplemented]
#endif
	public partial interface IContentTypeProvider
	{
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
		string ContentType
		{
			get;
		}
#endif
		// Forced skipping of method Windows.Storage.Streams.IContentTypeProvider.ContentType.get
	}
}
