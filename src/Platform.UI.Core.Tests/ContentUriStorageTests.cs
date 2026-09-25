#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SilverAssertions;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fence for WPE1-1 item C0b: a platform can hand out a <see cref="StorageFile"/> / <see cref="StorageFolder"/> that is
/// not a local path (a picked document behind a content URI) through the internal factories, and the public WinRT
/// API reads through the platform's implementation.
/// </summary>
public class ContentUriStorageTests
{
	[Fact]
	public async Task When_A_Platform_Creates_A_File_Over_A_Content_Uri_Then_The_Public_Api_Reads_Through_It()
	{
		//Arrange
		var file = StorageFile.FromImplementation(new ContentUriFile("content://documents/42", "notes.txt", "hello from a content URI"));

		//Act
		var text = await FileIO.ReadTextAsync(file).AsTask(TestContext.Current.CancellationToken);
		var properties = await file.GetBasicPropertiesAsync().AsTask(TestContext.Current.CancellationToken);

		//Assert
		text.Should().Be("hello from a content URI");
		file.Path.Should().Be("content://documents/42");
		file.Name.Should().Be("notes.txt");
		file.FileType.Should().Be(".txt");
		file.ContentType.Should().Be("text/plain");
		properties.Size.Should().Be(24UL);
	}

	[Fact]
	public async Task When_A_Platform_Creates_A_Folder_Over_A_Content_Uri_Then_Its_Files_Are_Listed_And_Readable()
	{
		//Arrange
		var folder = StorageFolder.FromImplementation(new ContentUriFolder("content://tree/7", "Picked", new[]
		{
			new ContentUriFile("content://tree/7/a", "a.txt", "first"),
			new ContentUriFile("content://tree/7/b", "b.txt", "second"),
		}));

		//Act
		var files = await folder.GetFilesAsync().AsTask(TestContext.Current.CancellationToken);
		var second = await FileIO.ReadTextAsync(files[1]).AsTask(TestContext.Current.CancellationToken);

		//Assert
		folder.Path.Should().Be("content://tree/7");
		folder.Name.Should().Be("Picked");
		files.Should().HaveCount(2);
		files[0].Name.Should().Be("a.txt");
		second.Should().Be("second");
	}

	[Fact]
	public void When_A_Platform_Passes_No_Implementation_Then_The_Factories_Refuse()
	{
		//Act
		var file = () => StorageFile.FromImplementation(null!);
		var folder = () => StorageFolder.FromImplementation(null!);

		//Assert
		file.Should().Throw<ArgumentNullException>();
		folder.Should().Throw<ArgumentNullException>();
	}

	/// <summary>A read-only document behind a URI, as a platform's document provider serves it.</summary>
	private sealed class ContentUriFile : StorageFile.ImplementationBase
	{
		private readonly string _name;
		private readonly byte[] _content;

		internal ContentUriFile(string uri, string name, string content)
			: base(uri)
		{
			_name = name;
			_content = Encoding.UTF8.GetBytes(content);
		}

		public override StorageProvider Provider { get; } = new("test-documents", "StorageProviderLocalDisplayName");

		public override string Name => _name;

		public override DateTimeOffset DateCreated => DateTimeOffset.UnixEpoch;

		protected override bool IsEqual(StorageFile.ImplementationBase implementation)
			=> implementation is ContentUriFile other && other.Path == Path;

		public override Task<StorageFolder?> GetParentAsync(CancellationToken ct) => Task.FromResult<StorageFolder?>(null);

		public override Task<BasicProperties> GetBasicPropertiesAsync(CancellationToken ct)
			=> Task.FromResult(new BasicProperties((ulong)_content.Length, DateTimeOffset.UnixEpoch));

		public override Task<IRandomAccessStreamWithContentType> OpenAsync(CancellationToken ct, FileAccessMode accessMode, StorageOpenOptions options)
			=> Task.FromResult<IRandomAccessStreamWithContentType>(new RandomAccessStreamWithContentType(new MemoryStream(_content, writable: false), ContentType));

		public override Task<StorageStreamTransaction> OpenTransactedWriteAsync(CancellationToken ct, StorageOpenOptions option)
			=> throw NotSupported();

		public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options) => throw NotSupported();
	}

	/// <summary>A folder behind a tree URI that lists the documents it was given.</summary>
	private sealed class ContentUriFolder : StorageFolder.ImplementationBase
	{
		private readonly string _name;
		private readonly IReadOnlyList<ContentUriFile> _files;

		internal ContentUriFolder(string uri, string name, IReadOnlyList<ContentUriFile> files)
			: base(uri)
		{
			_name = name;
			_files = files;
		}

		public override StorageProvider Provider { get; } = new("test-documents", "StorageProviderLocalDisplayName");

		public override string Name => _name;

		protected override bool IsEqual(StorageFolder.ImplementationBase implementation)
			=> implementation is ContentUriFolder other && other.Path == Path;

		public override Task<IReadOnlyList<StorageFile>> GetFilesAsync(CancellationToken ct)
		{
			var files = new List<StorageFile>();
			foreach (var file in _files)
			{
				files.Add(StorageFile.FromImplementation(file));
			}

			return Task.FromResult<IReadOnlyList<StorageFile>>(files);
		}

		public override Task<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options, CancellationToken cancellationToken) => throw NotSupported();

		public override Task<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option, CancellationToken token) => throw NotSupported();

		public override Task<StorageFolder> GetFolderAsync(string name, CancellationToken token) => throw NotSupported();

		public override Task<StorageFile> GetFileAsync(string name, CancellationToken token) => throw NotSupported();

		public override Task<IStorageItem> GetItemAsync(string name, CancellationToken token) => throw NotSupported();

		public override Task<StorageFolder?> GetParentAsync(CancellationToken token) => Task.FromResult<StorageFolder?>(null);

		public override Task<IStorageItem?> TryGetItemAsync(string name, CancellationToken token) => Task.FromResult<IStorageItem?>(null);

		public override Task<IReadOnlyList<IStorageItem>> GetItemsAsync(CancellationToken ct) => throw NotSupported();

		public override Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<StorageFolder>>(Array.Empty<StorageFolder>());

		public override Task DeleteAsync(StorageDeleteOption options, CancellationToken ct) => throw NotSupported();
	}
}
