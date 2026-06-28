using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Windows.ApplicationModel
{
	public partial class Package
	{
		private StorageFolder _installedLocation;

		internal Package() => InitializePlatform();

		partial void InitializePlatform();

		public bool IsDevelopmentMode => GetInnerIsDevelopmentMode();

		public PackageId Id { get; } = new();

		public DateTimeOffset InstallDate => GetInstallDate();

		public DateTimeOffset InstalledDate => GetInstallDate();

		public static Package Current { get; } = new Package();

		[CodeBrix.Platform.NotImplemented]
		public IReadOnlyList<Package> Dependencies => new List<Package>();

		/// <summary>
		/// Gets the current package's location in the original install folder for the current package.
		/// </summary>
#if false
#endif
		public StorageFolder InstalledLocation => _installedLocation ??= new StorageFolder(GetInstalledPath());

		/// <summary>
		/// Gets the current package's path in the original install folder for the current package.
		/// </summary>
#if false
#endif
		public string InstalledPath => GetInstalledPath();

		[CodeBrix.Platform.NotImplemented]
		public bool IsFramework => false;

#if !__SKIA__
		[CodeBrix.Platform.NotImplemented]
		public string Description => "";
#endif

		[CodeBrix.Platform.NotImplemented]
		public bool IsBundle => false;

		[CodeBrix.Platform.NotImplemented]
		public bool IsResourcePackage => false;

#if false
		[global::CodeBrix.Platform.NotImplemented]
		public global::System.Uri Logo => default;
#endif

#if !__SKIA__
		[CodeBrix.Platform.NotImplemented]
		public string PublisherDisplayName => "";
#endif

		[CodeBrix.Platform.NotImplemented]
		public PackageStatus Status => new PackageStatus();

		[CodeBrix.Platform.NotImplemented]
		public bool IsOptional => false;

		[CodeBrix.Platform.NotImplemented]
		public PackageSignatureKind SignatureKind => PackageSignatureKind.None;

		[CodeBrix.Platform.NotImplemented]
		public Foundation.IAsyncOperation<IReadOnlyList<Core.AppListEntry>> GetAppListEntriesAsync()
		{
			throw new NotImplementedException("The member IAsyncOperation<IReadOnlyList<AppListEntry>> Package.GetAppListEntriesAsync() is not implemented in CodeBrix.");
		}

		[CodeBrix.Platform.NotImplemented]
		public string GetThumbnailToken()
		{
			throw new NotImplementedException("The member string Package.GetThumbnailToken() is not implemented in CodeBrix.");
		}

		[CodeBrix.Platform.NotImplemented]
		public void Launch(string parameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Package", "void Package.Launch(string parameters)");
		}

		[CodeBrix.Platform.NotImplemented]
		public Foundation.IAsyncOperation<bool> VerifyContentIntegrityAsync()
		{
			throw new NotImplementedException("The member IAsyncOperation<bool> Package.VerifyContentIntegrityAsync() is not implemented in CodeBrix.");
		}
	}
}
