#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.Contracts;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Helpers;
using SilverAssertions;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Streams;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-13 (a; FIXLIST_codebrix_android [AP7-B Lottie] "an ms-appx:/// Lottie document never loads on Android"): a platform
/// whose package files are not files on disk registers IApplicationPackageFilesPlatform, and every Core caller that turned
/// an ms-appx URI into a path under the installed folder reads through it instead; unregistered, the file-path behaviour
/// is exactly what it was.
/// </summary>
public class ApplicationPackageFilesTests : IDisposable
{
	private static IApplicationPackageFilesPlatform? _packageFiles;
	private static bool _registered;

	public ApplicationPackageFilesTests()
	{
		if (!_registered)
		{
			// A builder that returns null means "not registered" to the registry.
			ApiExtensibility.Register(typeof(IApplicationPackageFilesPlatform), _ => _packageFiles!);
			_registered = true;
		}
	}

	public void Dispose() => _packageFiles = null;

	[Fact]
	public async Task When_A_Platform_Serves_The_Package_Then_An_Ms_Appx_File_Reads_Through_It()
	{
		//Arrange
		var package = UsePackage(("Assets/pulse.json", "{\"v\":\"5.7\"}"));

		//Act
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/pulse.json")).AsTask(TestContext.Current.CancellationToken);
		var text = await FileIO.ReadTextAsync(file).AsTask(TestContext.Current.CancellationToken);
		using var randomAccess = await file.OpenAsync(FileAccessMode.Read).AsTask(TestContext.Current.CancellationToken);
		var properties = await file.GetBasicPropertiesAsync().AsTask(TestContext.Current.CancellationToken);

		//Assert
		text.Should().Be("{\"v\":\"5.7\"}");
		file.Path.Should().Be("ms-appx:///Assets/pulse.json");
		file.Name.Should().Be("pulse.json");
		file.FileType.Should().Be(".json");
		randomAccess.Size.Should().Be(11UL);
		properties.Size.Should().Be(11UL);
		package.Opened.Should().Contain("Assets/pulse.json");
	}

	[Fact]
	public async Task When_The_Uri_Has_A_Host_Or_Escapes_Then_The_Relative_Path_Joins_And_Decodes_Them()
	{
		//Arrange
		UsePackage(("MyLibrary/Fonts/My Font.ttf", "font"), ("Assets/a b.txt", "spaced"));

		//Act
		var hosted = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx://MyLibrary/Fonts/My%20Font.ttf")).AsTask(TestContext.Current.CancellationToken);
		var escaped = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/a%20b.txt")).AsTask(TestContext.Current.CancellationToken);

		//Assert
		(await FileIO.ReadTextAsync(hosted).AsTask(TestContext.Current.CancellationToken)).Should().Be("font");
		(await FileIO.ReadTextAsync(escaped).AsTask(TestContext.Current.CancellationToken)).Should().Be("spaced");
		ApplicationPackageFiles.GetRelativePath(new Uri("ms-appx:///Assets/a%20b.txt")).Should().Be("Assets/a b.txt");
	}

	[Fact]
	public async Task When_The_Package_Has_No_Such_File_Then_FileNotFound_Is_Thrown()
	{
		//Arrange
		UsePackage();

		//Act
		var act = () => StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/missing.json")).AsTask(TestContext.Current.CancellationToken);

		//Assert
		await Assert.ThrowsAsync<FileNotFoundException>(act);
	}

	[Fact]
	public async Task When_A_Package_File_Is_Opened_For_Writing_Then_It_Is_Read_Only()
	{
		//Arrange
		UsePackage(("Assets/data.txt", "data"));
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/data.txt")).AsTask(TestContext.Current.CancellationToken);

		//Act
		var write = () => file.OpenAsync(FileAccessMode.ReadWrite).AsTask(TestContext.Current.CancellationToken);
		var delete = () => file.DeleteAsync().AsTask(TestContext.Current.CancellationToken);

		//Assert
		await Assert.ThrowsAsync<UnauthorizedAccessException>(write);
		await Assert.ThrowsAsync<UnauthorizedAccessException>(delete);
	}

	[Fact]
	public async Task When_A_Platform_Serves_The_Package_Then_A_Stream_Reference_And_The_Existence_Check_Use_It()
	{
		//Arrange
		UsePackage(("Assets/logo.png", "png-bytes"));

		//Act
		using var stream = await RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/logo.png")).OpenReadAsync().AsTask(TestContext.Current.CancellationToken);
		var reader = new StreamReader(stream.AsStreamForRead());
		var text = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
		var exists = await Windows.Storage.Helpers.StorageFileHelper.ExistsInPackage("Assets/logo.png");
		var existsWithSlash = await Windows.Storage.Helpers.StorageFileHelper.ExistsInPackage("/Assets/logo.png");
		var missing = await Windows.Storage.Helpers.StorageFileHelper.ExistsInPackage("Assets/other.png");
		var viaEvaluator = await AppDataUriEvaluator.ToStream(new Uri("ms-appx:///Assets/logo.png"), TestContext.Current.CancellationToken);

		//Assert
		text.Should().Be("png-bytes");
		exists.Should().BeTrue();
		existsWithSlash.Should().BeTrue();
		missing.Should().BeFalse();
		new StreamReader(viaEvaluator).ReadToEnd().Should().Be("png-bytes");
	}

	[Fact]
	public async Task When_A_Platform_Serves_The_Package_Then_The_Image_Scale_Probe_Finds_The_Variant_In_It()
	{
		//Arrange
		UsePackage(("Assets/wpe113icon.png", "100"), ("Assets/wpe113icon.scale-200.png", "200"), ("Assets/wpe113icon.scale-400.png", "400"));

		//Act
		var at200 = await PlatformImageHelpers.GetScaledPath(new Uri("ms-appx:///Assets/wpe113icon.png"), ResolutionScale.Scale200Percent);
		var at250 = ApplicationPackageFiles.FindScaledPath(_packageFiles!, "Assets/wpe113icon.png", 250, new[] { 100, 200, 400 });
		var at100 = ApplicationPackageFiles.FindScaledPath(_packageFiles!, "Assets/wpe113icon.png", 100, new[] { 100, 200, 400 });
		var none = ApplicationPackageFiles.FindScaledPath(_packageFiles!, "Assets/plain.png", 150, new[] { 100, 200, 400 });

		//Assert
		at200.Should().Be("ms-appx:///Assets/wpe113icon.scale-200.png");
		at250.Should().Be("Assets/wpe113icon.scale-200.png");
		at100.Should().Be("Assets/wpe113icon.scale-400.png"); // no scale-100 file: the file probe's second pass takes the largest one above
		none.Should().Be("Assets/plain.png");
	}

	[Fact]
	public async Task When_No_Platform_Is_Registered_Then_Ms_Appx_Is_A_File_Under_The_Installed_Folder_As_Before()
	{
		//Arrange
		_packageFiles = null;

		//Act
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/pulse.json")).AsTask(TestContext.Current.CancellationToken);

		//Assert
		ApplicationPackageFiles.Platform.Should().BeNull();
		file.Path.Should().Be(Path.Combine(StorageFile.ResourcePathBase, "", "Assets/pulse.json"));
	}

	// WPE1-14 (FIXLIST_platform_core_split [WPE1-13] FOUND, desktop ms-appx): Uri.Host is lower-cased, so
	// ms-appx://MyLibrary/... looked under "mylibrary/" and missed a "MyLibrary" folder on a case-sensitive file system.
	// The host as written is used when that path exists; otherwise the lower-cased path of before.

	[Fact]
	public async Task When_The_Library_Folder_Has_The_Written_Case_Then_An_Ms_Appx_File_Under_It_Is_Found()
	{
		//Arrange
		_packageFiles = null;
		using var installed = new InstalledFolder();
		installed.Write("MyLibrary/Assets/a.txt", "written");

		//Act
		var text = await installed.ReadAsync("ms-appx://MyLibrary/Assets/a.txt");

		//Assert
		text.Should().Be("written");
	}

	[Fact]
	public async Task When_The_Library_Folder_Is_Lower_Case_Then_An_Ms_Appx_File_Under_It_Is_Still_Found()
	{
		//Arrange
		_packageFiles = null;
		using var installed = new InstalledFolder();
		installed.Write("mylibrary/Assets/a.txt", "lower");

		//Act
		var text = await installed.ReadAsync("ms-appx://MyLibrary/Assets/a.txt");

		//Assert
		text.Should().Be("lower");
	}

	[Fact]
	public async Task When_Both_Library_Folders_Exist_Then_The_Written_Case_Wins()
	{
		//Arrange
		_packageFiles = null;
		using var installed = new InstalledFolder();
		installed.Write("MyLibrary/Assets/a.txt", "written");
		installed.Write("mylibrary/Assets/a.txt", "lower");

		//Act
		var text = await installed.ReadAsync("ms-appx://MyLibrary/Assets/a.txt");

		//Assert
		text.Should().Be("written");
	}

	[Fact]
	public void When_Only_The_Written_Case_Folder_Exists_Then_A_Missing_File_Still_Maps_Into_It()
	{
		//Arrange
		using var installed = new InstalledFolder();
		installed.Write("MyLibrary/Assets/icon.scale-200.png", "variant");

		//Act
		var host = InstalledPackagePath.ResolveHost(new Uri("ms-appx://MyLibrary/Assets/icon.png"), installed.Path, "/Assets/icon.png");
		var noFolder = InstalledPackagePath.ChooseHost(installed.Path, "Other", "other", "/Assets/icon.png");

		//Assert
		host.Should().Be("MyLibrary");
		noFolder.Should().Be("other");
	}

	[Fact]
	public void When_The_Host_Is_Read_As_Written_Then_Its_Casing_Survives_Uri_Parsing()
	{
		//Act
		var written = InstalledPackagePath.GetWrittenHost(new Uri("ms-appx://MyLibrary/Assets/a%20b.png?x=1"));
		var noHost = InstalledPackagePath.GetWrittenHost(new Uri("ms-appx:///Assets/a.png"));

		//Assert
		written.Should().Be("MyLibrary");
		noHost.Should().BeEmpty();
	}

	/// <summary>A temporary installed folder that <see cref="StorageFile.ResourcePathBase"/> points at while it lives.</summary>
	private sealed class InstalledFolder : IDisposable
	{
		private readonly string _previousBase = StorageFile.ResourcePathBase;

		internal InstalledFolder()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wpe1-14-installed-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
			StorageFile.ResourcePathBase = Path;
		}

		internal string Path { get; }

		internal void Write(string relativePath, string content)
		{
			var full = System.IO.Path.Combine(Path, relativePath);
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
			File.WriteAllText(full, content);
		}

		internal async Task<string> ReadAsync(string uri)
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri)).AsTask(TestContext.Current.CancellationToken);
			return await FileIO.ReadTextAsync(file).AsTask(TestContext.Current.CancellationToken);
		}

		public void Dispose()
		{
			StorageFile.ResourcePathBase = _previousBase;
			Directory.Delete(Path, recursive: true);
		}
	}

	// WPE1-21 (FIXLIST_codebrix_android_buildout [AP7-C] SoundEffect ms-appx): SoundEffect.Preload/Play(string) read an
	// ms-appx source with File.ReadAllBytes under the installed folder before any platform code ran, so on a platform
	// whose package files are not files an ms-appx sound effect was never found. It reads through the contract now.

	[Fact]
	public void When_A_Platform_Serves_The_Package_Then_A_Sound_Effect_Reads_Its_Ms_Appx_Source_Through_It()
	{
		//Arrange
		var package = UsePackage(("Assets/Sounds/click one.wav", "RIFF-click"));

		//Act
		var bytes = CodeBrix.Platform.UI.AudioPlayer.Skia.Internal.AudioSourceResolver.ReadAllBytes("ms-appx:///Assets/Sounds/click%20one.wav");

		//Assert
		Encoding.UTF8.GetString(bytes).Should().Be("RIFF-click");
		package.Opened.Should().Equal("Assets/Sounds/click one.wav");
	}

	[Fact]
	public void When_A_Platform_Serves_The_Package_Then_Preloading_A_Sound_Effect_Needs_No_File_On_Disk()
	{
		//Arrange
		var package = UsePackage(("Assets/Sounds/preload.wav", "RIFF-preload"));

		try
		{
			//Act
			var preload = () => CodeBrix.Platform.UI.AudioPlayer.Skia.SoundEffect.Preload("ms-appx:///Assets/Sounds/preload.wav");

			//Assert
			preload.Should().NotThrow();
			package.Opened.Should().Equal("Assets/Sounds/preload.wav");
		}
		finally
		{
			CodeBrix.Platform.UI.AudioPlayer.Skia.SoundEffect.ClearCache();
		}
	}

	[Fact]
	public void When_The_Package_Has_No_Such_Sound_Then_FileNotFound_Is_Thrown()
	{
		//Arrange
		UsePackage();

		//Act
		var read = () => CodeBrix.Platform.UI.AudioPlayer.Skia.Internal.AudioSourceResolver.ReadAllBytes("ms-appx:///Assets/Sounds/missing.wav");

		//Assert
		read.Should().Throw<FileNotFoundException>();
	}

	[Fact]
	public void When_A_Platform_Serves_The_Package_Then_A_Sound_Effect_File_Path_Still_Reads_The_File()
	{
		//Arrange
		var package = UsePackage(("Assets/Sounds/click.wav", "from the package"));
		var file = Path.Combine(Path.GetTempPath(), "wpe121-" + Guid.NewGuid().ToString("N") + ".wav");
		File.WriteAllText(file, "from the disk");

		try
		{
			//Act
			var bytes = CodeBrix.Platform.UI.AudioPlayer.Skia.Internal.AudioSourceResolver.ReadAllBytes(file);

			//Assert
			Encoding.UTF8.GetString(bytes).Should().Be("from the disk");
			package.Opened.Should().BeEmpty();
		}
		finally
		{
			File.Delete(file);
		}
	}

	private static FakePackage UsePackage(params (string Path, string Content)[] files)
	{
		var package = new FakePackage(files);
		_packageFiles = package;
		return package;
	}

	/// <summary>An APK-like package: files by relative path; streams are NOT seekable, as an Android asset stream.</summary>
	private sealed class FakePackage : IApplicationPackageFilesPlatform
	{
		private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

		internal FakePackage((string Path, string Content)[] files)
		{
			foreach (var (path, content) in files)
			{
				_files[path] = Encoding.UTF8.GetBytes(content);
			}
		}

		internal List<string> Opened { get; } = new();

		public bool FileExists(string relativePath) => _files.ContainsKey(relativePath);

		public Stream? OpenRead(string relativePath)
		{
			if (!_files.TryGetValue(relativePath, out var bytes))
			{
				return null;
			}

			Opened.Add(relativePath);
			return new ForwardOnlyStream(new MemoryStream(bytes, writable: false));
		}
	}

	private sealed class ForwardOnlyStream(Stream inner) : Stream
	{
		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				inner.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
