using Windows.Storage;

namespace CodeBrix.Platform.Storage.Internal //Was previously: Uno.Storage.Internal
{
	internal static class StorageProviders
	{
		public static StorageProvider Local { get; } = new StorageProvider("computer", "StorageProviderLocalDisplayName");

#if false
		public static StorageProvider WasmDownloadPicker { get; } = new StorageProvider("wasmdownloadpicker", "StorageProviderWasmDownloadPickerName");

		public static StorageProvider WasmNative { get; } = new StorageProvider("jsfileaccessapi", "StorageProviderWasmNativeDisplayName");
#endif

#if false
		public static StorageProvider AndroidSaf { get; } = new StorageProvider("androidsaf", "StorageProviderAndroidSafDisplayName");
#endif

#if false
		public static StorageProvider IosSecurityScoped { get; } = new StorageProvider("iossecurityscoped", "StorageProviderIosSecurityScopedDisplayName");
#endif
	}
}
