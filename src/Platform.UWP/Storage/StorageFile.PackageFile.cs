#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Contracts;
using CodeBrix.Platform.Helpers;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	partial class StorageFile
	{
		/// <summary>
		/// A read-only application package file served by a registered <see cref="IApplicationPackageFilesPlatform"/>
		/// (WPE1-13): what StorageFile.GetFileFromApplicationUriAsync returns on a platform whose package files are not
		/// files on disk. Its <see cref="ImplementationBase.Path"/> is the ms-appx URI; writing, deleting, renaming or
		/// moving it fails with <see cref="UnauthorizedAccessException"/>, as for a file of an installed package.
		/// </summary>
		private sealed class PackageFile : ImplementationBase
		{
			private static readonly StorageProvider _provider = new StorageProvider("appx", "StorageProviderLocalDisplayName");

			private readonly IApplicationPackageFilesPlatform _platform;
			private readonly string _relativePath;

			internal PackageFile(IApplicationPackageFilesPlatform platform, string relativePath)
				: base(ApplicationPackageFiles.ToUriString(relativePath))
			{
				_platform = platform;
				_relativePath = relativePath;
			}

			public override StorageProvider Provider => _provider;

			public override string Name => global::System.IO.Path.GetFileName(_relativePath);

			public override DateTimeOffset DateCreated => DateTimeOffset.MinValue;

			protected override bool IsEqual(ImplementationBase implementation)
				=> implementation is PackageFile other && string.Equals(other._relativePath, _relativePath, StringComparison.Ordinal);

			public override Task<StorageFolder?> GetParentAsync(CancellationToken ct)
				=> Task.FromResult<StorageFolder?>(null);

			public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			{
				using var stream = ApplicationPackageFiles.OpenRead(_platform, _relativePath);
				var size = stream.CanSeek ? stream.Length : CountBytes(stream);
				return Task.FromResult(new BasicProperties((ulong)size, DateTimeOffset.MinValue));
			}

			public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			{
				ThrowIfWrite(accessMode);
				return Task.FromResult<IRandomAccessStreamWithContentType>(
					new RandomAccessStreamWithContentType(ApplicationPackageFiles.OpenSeekable(_platform, _relativePath), ContentType));
			}

			public override Task<Stream> OpenStreamAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			{
				ThrowIfWrite(accessMode);
				return Task.FromResult(ApplicationPackageFiles.OpenRead(_platform, _relativePath));
			}

			public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
				=> throw ReadOnly();

			public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
				=> throw ReadOnly();

			public override Task RenameAsync(CancellationToken ct, string desiredName, NameCollisionOption option)
				=> throw ReadOnly();

			public override Task MoveAndReplaceAsync(CancellationToken ct, IStorageFile target)
				=> throw ReadOnly();

			private void ThrowIfWrite(FileAccessMode accessMode)
			{
				if (accessMode != FileAccessMode.Read)
				{
					throw ReadOnly();
				}
			}

			private UnauthorizedAccessException ReadOnly()
				=> new UnauthorizedAccessException($"The application package file [{Path}] is read-only.");

			private static long CountBytes(Stream stream)
			{
				var buffer = new byte[81920];
				long total = 0;
				int read;
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					total += read;
				}

				return total;
			}
		}
	}
}
