using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using IOPath = global::System.IO.Path;

#if false
using UIKit;
using Foundation;
#endif

namespace Windows.Storage
{
	public partial class StorageFolder : IStorageFolder, IStorageItem, IStorageItem2
	{
		private StorageFolder(ImplementationBase implementation)
		{
			Implementation = implementation;
			Implementation.InitOwner(this);
		}

		/// <summary>
		/// Creates a folder over a platform-provided implementation: the entry point a platform uses to hand out folders
		/// that are not paths on the local file system, such as a document tree a system folder picker returned as a
		/// content URI (the Skia heads' own pickers keep returning local folders).
		/// </summary>
		/// <param name="implementation">The platform's implementation of the folder: its name, listing and item
		/// operations (<see cref="ImplementationBase"/> is the provider contract; <see cref="ImplementationBase.Path"/> is
		/// whatever identifies the folder on that platform, such as the tree URI).</param>
		/// <returns>A new <see cref="StorageFolder"/> backed by <paramref name="implementation"/>.</returns>
		/// <remarks>Implementers: Android, Mobile. Platform (Skia): not used. Reached through InternalsVisibleTo.</remarks>
		internal static StorageFolder FromImplementation(ImplementationBase implementation)
			=> new StorageFolder(implementation ?? throw new ArgumentNullException(nameof(implementation)));

		public StorageProvider Provider => Implementation.Provider;

		public string Path => Implementation.Path;

		public string Name => Implementation.Name;

		public string DisplayName => Implementation.DisplayName;

		internal ImplementationBase Implementation { get; }

#if true
		private static Task TryInitializeStorage() => Task.CompletedTask;
#endif

		public bool IsOfType(StorageItemTypes type) =>
			type == StorageItemTypes.Folder;

		public static IAsyncOperation<StorageFolder> GetFolderFromPathAsync(string path) =>
			AsyncOperation.FromTask(async ct =>
			{
				await TryInitializeStorage();

				if (Directory.Exists(path))
				{
					return new StorageFolder(path);
				}
				else
				{
					throw new DirectoryNotFoundException($"The folder {path} does not exist");
				}
			}
		);

		public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName) =>
			CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);

		public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options) =>
			AsyncOperation.FromTask(token => Implementation.CreateFolderAsync(desiredName, options, token));

		public IAsyncOperation<StorageFile> GetFileAsync(string name) =>
			AsyncOperation.FromTask(ct => Implementation.GetFileAsync(name, ct));

		public IAsyncOperation<IStorageItem> GetItemAsync(string name) =>
			AsyncOperation.FromTask(ct => Implementation.GetItemAsync(name, ct));

		public IAsyncOperation<StorageFolder> GetFolderAsync(string name) =>
			AsyncOperation.FromTask(ct => Implementation.GetFolderAsync(name, ct));

		public IAsyncOperation<IStorageItem> TryGetItemAsync(string name) =>
			AsyncOperation.FromTask(ct => Implementation.TryGetItemAsync(name, ct));

		public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName) =>
			CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);

		public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options) =>
			AsyncOperation.FromTask(ct => Implementation.CreateFileAsync(desiredName, options, ct));

		public IAsyncAction DeleteAsync() => DeleteAsync(StorageDeleteOption.Default);

		public IAsyncAction DeleteAsync(StorageDeleteOption option) =>
			AsyncAction.FromTask(ct => Implementation.DeleteAsync(option, ct));

		public IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync() =>
			AsyncOperation.FromTask(ct => Implementation.GetItemsAsync(ct));

		public IAsyncOperation<IReadOnlyList<StorageFile>> GetFilesAsync() =>
			AsyncOperation.FromTask(ct => Implementation.GetFilesAsync(ct));

		public IAsyncOperation<IReadOnlyList<StorageFolder>> GetFoldersAsync() =>
			AsyncOperation.FromTask(ct => Implementation.GetFoldersAsync(ct));

		public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() =>
			AsyncOperation.FromTask(ct => Implementation.GetBasicPropertiesAsync(ct));

		public IAsyncOperation<StorageFolder> GetParentAsync() =>
			AsyncOperation.FromTask(ct => Implementation.GetParentAsync(ct));

		public bool IsEqual(IStorageItem item) => Implementation.IsEqual(item);
	}
}
