#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;

namespace Microsoft.UI.Xaml.Documents;

internal readonly partial struct UnicodeText
{
	private static class ICU
	{
		private static Assembly? _dataAssembly;

		// The version number of ICU is important because the exported symbols have their names appended by
		// the version number. For example, there's a ubrk_open_74 in ICU v74, but not a ubrk_open.
		private static int _icuVersion;
		private static IntPtr _libicuuc;
		// Concurrent because the layout engine is reachable off the UI thread through the
		// CodeBrix.Platform.UI.TextLayout add-in - an image pipeline or a background renderer can lay
		// text out on any thread. A plain Dictionary here corrupts under that load. Two threads racing
		// to resolve the same symbol is harmless: they compute the same delegate.
		private static readonly ConcurrentDictionary<Type, object> _lookupCache = new();

		// Guards the one-time native load. An application head initialises ICU from a module
		// initializer emitted by IcuDataInitializerGenerator, which runs before any user code; the
		// flag lets a host-free caller (the TextLayout add-in, or a test) initialise on demand
		// instead, without ever double-loading.
		private static readonly object _initLock = new();
		private static bool _initialized;

		private const DllImportSearchPath NativeLibrarySearchDirectories =
			  DllImportSearchPath.ApplicationDirectory
			| DllImportSearchPath.AssemblyDirectory
			| DllImportSearchPath.UserDirectories
			;

		public static void SetDataAssembly(Assembly assembly)
		{
			lock (_initLock)
			{
				_dataAssembly = assembly;
				Init();
				_initialized = true;
			}
		}

		/// <summary>
		/// Loads ICU if no application head has already done so.
		/// </summary>
		/// <remarks>
		/// Heads always win this race - their module initializer runs first - so in an application
		/// this is a no-op. It exists for callers with no head at all, where nothing would otherwise
		/// call <see cref="SetDataAssembly"/> and the first bidi call would fail on a null library
		/// handle. On Windows and macOS, ICU's data file is an embedded resource of the head
		/// assembly, so the entry assembly is the best available guess when there is no head.
		/// </remarks>
		public static void EnsureInitialized()
		{
			if (Volatile.Read(ref _initialized))
			{
				return;
			}

			lock (_initLock)
			{
				if (_initialized)
				{
					return;
				}

				_dataAssembly ??= Assembly.GetEntryAssembly();
				Init();
				_initialized = true;
			}
		}

		const int MinSupportedIcuucVersion = 50;
		const int MaxSupportedIcuucVersion = 100;

		private static unsafe void Init()
		{
			IntPtr libicuuc;
			if (OperatingSystem.IsWindows())
			{
				// On Windows, we get the ICU binaries from the CodeBrix.Platform.Unicode package.
				_icuVersion = 77;
				if (!NativeLibrary.TryLoad("icuuc77", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out libicuuc))
				{
					// Say where the binary comes from: Windows has no system ICU to fall back on, so
					// this always means the package that carries it never reached the output folder.
					// An application head gets it through its runtime package; a project that uses the
					// text engine WITHOUT a head has to reference the Unicode package itself.
					throw new Exception(
						"Failed to load ICU on Windows. Attempted: [icuuc77]. The ICU binaries ship in "
						+ "the CodeBrix.Platform.Unicode.ApacheLicenseForever package - reference it "
						+ "directly from any project that uses the text engine without an application "
						+ "head.");
				}
			}
			else if (OperatingSystem.IsIOS())
			{
				_icuVersion = 77;
				libicuuc = IntPtr.Zero;
			}
			else if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() || OperatingSystem.IsMacOS())
			{
				// On Linux, we get the ICU binaries from the dynamic linker search path.
				// On MacOS, we get the ICU binaries from the CodeBrix.Platform.UnicodeMacOs package.
				// On Android, ICU is a system library accessible only through the default
				// dlopen search path (not through assembly-relative paths).
				if (OperatingSystem.IsMacOS() && !NativeLibrary.TryLoad("icudata", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out _))
				{
					// MacOS doesn't automatically load icudata from icuuc for some reason even though the icuuc binary
					// lists icudata in the `otool -L` output, so we have to load it by hand
					throw new Exception("Failed to load libicudata.");
				}
				// Track every candidate we attempt so the final exception can report exactly
				// what was tried. On Android the real fallback is "libicu.so", not "libicuuc".
				var attempts = new List<string> { "icuuc" };
				Exception? lastError = null;

				if (!NativeLibrary.TryLoad("icuuc", typeof(ICU).Assembly, NativeLibrarySearchDirectories, out libicuuc))
				{
					if (OperatingSystem.IsLinux())
					{
						for (int j = MaxSupportedIcuucVersion; j >= MinSupportedIcuucVersion; j--)
						{
							// some environments only have a versioned library and don't symlink it to libicuuc.so
							var name = $"libicuuc.so.{j}";
							attempts.Add(name);
							if (NativeLibrary.TryLoad(name, typeof(ICU).Assembly, DllImportSearchPath.UserDirectories, out libicuuc))
							{
								break;
							}
						}
					}
					else if (OperatingSystem.IsAndroid())
					{
						// Three tiers on Android:
						//   - API 31+: the NDK-stable wrapper "libicu.so" is available.
						//   - API 21-23: the private "libicuuc.so" can still be dlopen'd.
						//   - API 24-30: the linker namespace blocks "libicuuc.so" and
						//     "libicu.so" does not exist yet, so skip loading and fail
						//     fast with a clear error below.
						// ICU is a system library on Android, so use default dlopen search
						// paths (not assembly-relative). Use Load (not TryLoad) so the
						// underlying dlopen error is preserved for diagnostics.
						string? androidFallback = null;
						if (OperatingSystem.IsAndroidVersionAtLeast(31))
						{
							androidFallback = "libicu.so";
						}
						else if (!OperatingSystem.IsAndroidVersionAtLeast(24))
						{
							androidFallback = "libicuuc.so";
						}

						if (androidFallback is not null)
						{
							attempts.Add(androidFallback);
							try
							{
								libicuuc = NativeLibrary.Load(androidFallback);
							}
							catch (Exception e)
							{
								lastError = e;
								libicuuc = IntPtr.Zero;
							}
						}
					}
				}
				if (libicuuc == IntPtr.Zero)
				{
					var platform = OperatingSystem.IsAndroid() ? "Android"
						: OperatingSystem.IsLinux() ? "Linux"
						: OperatingSystem.IsMacOS() ? "MacOS"
						: "unknown";
					string hint;
					if (OperatingSystem.IsAndroid()
						&& OperatingSystem.IsAndroidVersionAtLeast(24)
						&& !OperatingSystem.IsAndroidVersionAtLeast(31))
					{
						hint = " Android API 24-30 has no loadable ICU: the NDK-stable libicu.so requires API 31+, and the private libicuuc.so is blocked by the linker namespace.";
					}
					else if (OperatingSystem.IsAndroid() && !OperatingSystem.IsAndroidVersionAtLeast(31))
					{
						hint = " Android's NDK-stable ICU (libicu.so) requires API 31+; libicuuc.so was attempted as a best-effort fallback for API 21-23.";
					}
					else
					{
						hint = string.Empty;
					}
					throw new Exception(
						$"Failed to load ICU on {platform}. Attempted: [{string.Join(", ", attempts)}].{hint}"
						+ (lastError is null ? string.Empty : $" Last loader error: {lastError.Message}"),
						lastError);
				}

				// Since libicuuc not installed by us, we have no control over the specific version number, so
				// we try a wide range of versions.
				for (int i = MaxSupportedIcuucVersion; i >= MinSupportedIcuucVersion; i--)
				{
					if (NativeLibrary.TryGetExport(libicuuc, $"u_getVersion_{i}", out _))
					{
						_icuVersion = i;
						break;
					}
				}

				if (_icuVersion == 0)
				{
					throw new Exception($"Loaded icuuc, but could not find symbol `u_getVersion_N`, where N is in range [{MinSupportedIcuucVersion}-{MaxSupportedIcuucVersion}].");
				}
			}
			else
			{
				throw new DllNotFoundException("Failed to load libicuuc: unsupported platform.");
			}

			_libicuuc = libicuuc;

			GetMethod<u_getVersion>()(out var versionInfo);
			var ptr = Marshal.AllocHGlobal(1000);
			GetMethod<u_versionToString>()((IntPtr)(&versionInfo), ptr);
			typeof(ICU).LogDebug()?.Debug($"Found ICU version {Marshal.PtrToStringAnsi(ptr)}.");
			Marshal.FreeHGlobal(ptr);

			if (OperatingSystem.IsMacOS() || OperatingSystem.IsWindows())
			{
				LoadCommonData();
			}
		}

		/// <summary>
		/// Hands ICU its common data archive, on the platforms that have no system copy of it.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Linux never gets here: ICU comes off the dynamic linker search path already carrying its
		/// data. Windows and macOS have no system ICU at all, so the 5.5 MB icudt.dat archive has to
		/// be handed in by hand - and until that happens, everything that needs a Unicode property
		/// table fails. Bidi resolution keeps working without it, which makes the failure look
		/// intermittent: it is line breaking (ubrk_*) that dies first, with U_MISSING_RESOURCE_ERROR.
		/// </para>
		/// <para>
		/// An application head embeds the archive into its own assembly and names that assembly
		/// through <see cref="SetDataAssembly"/>, so the first candidate below settles it. Everything
		/// after that exists for a caller with NO head - the TextLayout add-in, an image pipeline, a
		/// test - where the entry assembly is something like a test host that carries no archive.
		/// Those callers get the archive as a file next to the application instead, which is what the
		/// Unicode package's build targets put there.
		/// </para>
		/// </remarks>
		private static unsafe void LoadCommonData()
		{
			// What was looked for, recorded as the search goes, so a failure can report it rather
			// than naming one assembly and leaving the rest a mystery.
			var attempts = new List<string>();

			if (TryFindCommonData(attempts, out var bytes))
			{
				// udata_setCommonData does not copy the buffer, so it needs to be pinned.
				// For alignment, the ICU docs require 16-byte alignment. https://unicode-org.github.io/icu/userguide/icu_data/#alignment
				var data = NativeMemory.AlignedAlloc((UIntPtr)bytes.Length, 16);
				bytes.AsSpan().CopyTo(new Span<byte>(data, bytes.Length));
				GetMethod<udata_setCommonData>()((IntPtr)data, out var errorCode);
				CheckErrorCode<udata_setCommonData>(errorCode);
				return;
			}

			// No archive anywhere - but a build that links its data straight into the binary needs
			// none, so ask ICU itself before giving up. u_init is the cheap probe for this: it
			// reports U_FILE_ACCESS_ERROR when, and only when, the common data cannot be reached.
			if (IsCommonDataAvailable())
			{
				typeof(ICU).LogDebug()?.Debug(
					"icudt.dat was not found, but ICU reports its common data is already available.");
				return;
			}

			throw new InvalidOperationException(
				$"Failed to find the ICU data archive (icudt.dat). Attempted: [{string.Join(", ", attempts)}]. "
				+ "The archive ships in the CodeBrix.Platform.Unicode.ApacheLicenseForever package "
				+ "(CodeBrix.Platform.UnicodeMacOs.ApacheLicenseForever on macOS), which embeds it into "
				+ "an application head and copies it beside the application otherwise.");
		}

		/// <summary>
		/// Looks for icudt.dat, as an embedded resource first and then as a file on disk.
		/// </summary>
		private static bool TryFindCommonData(List<string> attempts, out byte[] data)
		{
			// The named guesses are worth reporting one by one; the sweep behind them is not, so it
			// gets counted and reported as a single line. An error listing sixty loaded assemblies
			// buries the two that were actually expected to carry the archive.
			var sweptAssemblies = 0;

			foreach (var (assembly, isNamedGuess) in CandidateDataAssemblies())
			{
				if (isNamedGuess)
				{
					attempts.Add($"resource in {assembly.GetName().Name}");
				}
				else
				{
					sweptAssemblies++;
				}

				string? resourceName;
				try
				{
					resourceName = assembly
						.GetManifestResourceNames()
						.FirstOrDefault(name => name.EndsWith("icudt.dat", StringComparison.InvariantCulture));
				}
				catch (Exception e)
				{
					// A dynamic or otherwise unreadable assembly must not sink the whole search - it
					// was only ever a guess that it might be the one carrying the archive.
					typeof(ICU).LogTrace()?.Trace(
						$"Could not read the manifest resources of {assembly.GetName().Name}: {e.Message}");
					continue;
				}

				if (resourceName is null || assembly.GetManifestResourceStream(resourceName) is not { } stream)
				{
					continue;
				}

				using (stream)
				{
					data = new byte[stream.Length];
					stream.ReadExactly(data);
				}

				return true;
			}

			if (sweptAssemblies > 0)
			{
				attempts.Add($"resources in {sweptAssemblies} other loaded assemblies");
			}

			foreach (var path in CandidateDataFiles())
			{
				attempts.Add(path);

				if (File.Exists(path))
				{
					data = File.ReadAllBytes(path);
					return true;
				}
			}

			data = [];
			return false;
		}

		/// <summary>
		/// The assemblies that might carry icudt.dat, best guess first.
		/// </summary>
		/// <returns>
		/// Each candidate, flagged true when it is a deliberate guess rather than one more assembly
		/// off the sweep - which is all the difference between a useful error message and a wall of
		/// assembly names.
		/// </returns>
		private static IEnumerable<(Assembly Assembly, bool IsNamedGuess)> CandidateDataAssemblies()
		{
			var seen = new HashSet<Assembly>();

			// The head's own assembly, named through SetDataAssembly - right in an application, and
			// the only candidate that used to be tried.
			if (_dataAssembly is not null && seen.Add(_dataAssembly))
			{
				yield return (_dataAssembly, true);
			}

			// EnsureInitialized already defaults _dataAssembly to the entry assembly, so this only
			// adds anything when a head named a different one.
			if (Assembly.GetEntryAssembly() is { } entryAssembly && seen.Add(entryAssembly))
			{
				yield return (entryAssembly, true);
			}

			// Under a test host the entry assembly is the RUNNER, and the archive - if it was embedded
			// at all - sits in the test assembly instead. Nothing identifies that assembly ahead of
			// time, so sweep what is loaded. This runs once, behind the init lock.
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!assembly.IsDynamic && seen.Add(assembly))
				{
					yield return (assembly, false);
				}
			}
		}

		/// <summary>
		/// The places icudt.dat might sit on disk, for a caller with no head to embed it.
		/// </summary>
		private static IEnumerable<string> CandidateDataFiles()
		{
			// BaseDirectory rather than the assembly's own Location: it is the one that still answers
			// correctly inside a single-file application, and it is where a CopyToOutputDirectory item
			// lands in every layout the framework ships.
			var baseDirectory = AppContext.BaseDirectory;
			if (!string.IsNullOrEmpty(baseDirectory))
			{
				yield return Path.Combine(baseDirectory, "icudt.dat");
			}
		}

		/// <summary>
		/// Asks ICU whether it can already reach its common data.
		/// </summary>
		private static bool IsCommonDataAvailable()
		{
			if (!NativeLibrary.TryGetExport(_libicuuc, nameof(u_init), out var export)
				&& !NativeLibrary.TryGetExport(_libicuuc, $"{nameof(u_init)}_{_icuVersion}", out export))
			{
				return false;
			}

			// ICU reads the status on the way IN as well as writing it on the way out, so it has to
			// start at U_ZERO_ERROR - an `out` parameter would hand it whatever was on the stack.
			var status = 0;
			Marshal.GetDelegateForFunctionPointer<u_init>(export)(ref status);

			// Negative codes are ICU "warnings", which it hands out freely; only a positive code means
			// the data could not be reached.
			return status <= 0;
		}

		public static T GetMethod<T>()
		{
			if (!_lookupCache.TryGetValue(typeof(T), out var value))
			{
				if (OperatingSystem.IsIOS())
				{
					// iOS doesn't support NativeLibrary.TryGetExport so we have to make DllImport declarations to
					// the exact symbol names at compile times (even DllImport.EntryPoint doesn't work) and do the
					// method mapping by reflection.
					var (methodName, type) = ($"{typeof(T).Name}_{_icuVersion}", typeof(IOSICUSymbols));
					var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
					if (method is null)
					{
						throw new InvalidOperationException($"Failed to find {typeof(T).Name} in {type.Name}.");
					}
					value = Delegate.CreateDelegate(typeof(T), method);
				}
				else if (NativeLibrary.TryGetExport(_libicuuc, typeof(T).Name, out var originalNameFunc))
				{
					value = Marshal.GetDelegateForFunctionPointer<T>(originalNameFunc)!;
				}
				else if (NativeLibrary.TryGetExport(_libicuuc, $"{typeof(T).Name}_{_icuVersion}", out var versionPostfixedFunc))
				{
					value = Marshal.GetDelegateForFunctionPointer<T>(versionPostfixedFunc)!;
				}
				else
				{
					throw new Exception($"Failed to obtain the {typeof(T).Name} method from the ICU libraries.");
				}
				_lookupCache[typeof(T)] = value;
			}
			return (T)value;
		}

		public static unsafe DisposableStruct<IntPtr> CreateBiDiAndSetPara(string text, int start, int end, byte paraLevel, out IntPtr bidi)
		{
			bidi = GetMethod<ubidi_open>()();
			fixed (char* textPtr = &text.GetPinnableReference())
			{
				GetMethod<ubidi_setPara>()(bidi, (IntPtr)(textPtr + start), end - start, paraLevel, IntPtr.Zero, out var setParaErrorCode);
				if (setParaErrorCode > 0)
				{
					throw new InvalidOperationException($"{nameof(ubidi_setPara)} failed with error code {setParaErrorCode}");
				}
			}
			return new DisposableStruct<IntPtr>(static bidi => GetMethod<ubidi_close>()(bidi), bidi);
		}

		public static void CheckErrorCode<T>(int status)
		{
			if (status > 0)
			{
				throw new InvalidOperationException($"{typeof(T).Name} failed with error code {status.ToString("X", CultureInfo.InvariantCulture)}");
			}
			else if (status < 0)
			{
				// ICU has a very low bar for what it considers a "warning", so this can be very spammy.
				var errorString = Marshal.PtrToStringUTF8(GetMethod<u_errorName>()(status));
				typeof(ICU).LogTrace()?.Trace($"{typeof(T).Name} raised a warning code: {errorString}");
			}
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ubidi_open();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_close(IntPtr pBiDi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_setPara(IntPtr pBiDi, IntPtr text, int length, byte paraLevel, IntPtr embeddingLevels, out int errorCode);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubidi_getLogicalRun(IntPtr pBiDi, int logicalPosition, out int logicalLimit, out byte level);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubidi_countRuns(IntPtr pBiDI, out int errorCode);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubidi_getVisualRun(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ubrk_open(int type, IntPtr locale, IntPtr text, int textLength, out int status);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ubrk_close(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubrk_first(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int ubrk_next(IntPtr bi);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void u_getVersion(out UVersionInfo versionInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void u_versionToString(IntPtr versionArray, IntPtr versionString);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void udata_setCommonData(IntPtr bytes, out int errorCode);

		// Resolved by hand rather than through GetMethod, because "not exported" is a legitimate
		// answer here (see IsCommonDataAvailable) and GetMethod treats it as fatal.
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void u_init(ref int status);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr u_errorName(int code);

		[StructLayout(LayoutKind.Sequential)]
		private struct UVersionInfo
		{
			public byte byte1;
			public byte byte2;
			public byte byte3;
			public byte byte4;
		}

		private static class IOSICUSymbols
		{
			[DllImport("__Internal")]
			static extern void udata_setCommonData_77(IntPtr bytes, out int errorCode);

			[DllImport("__Internal")]
			static extern IntPtr ubidi_open_77();

			[DllImport("__Internal")]
			static extern void ubidi_close_77(IntPtr pBiDi);

			[DllImport("__Internal")]
			static extern void ubidi_setPara_77(IntPtr pBiDi, IntPtr text, int length, byte paraLevel, IntPtr embeddingLevels, out int errorCode);

			[DllImport("__Internal")]
			static extern void ubidi_getLogicalRun_77(IntPtr pBiDi, int logicalPosition, out int logicalLimit, out byte level);

			[DllImport("__Internal")]
			static extern int ubidi_countRuns_77(IntPtr pBiDI, out int errorCode);

			[DllImport("__Internal")]
			static extern int ubidi_getVisualRun_77(IntPtr pBiDi, int runIndex, out int logicalStart, out int length);

			[DllImport("__Internal")]
			static extern IntPtr ubrk_open_77(int type, IntPtr locale, IntPtr text, int textLength, out int status);

			[DllImport("__Internal")]
			static extern void ubrk_close_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern int ubrk_first_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern int ubrk_next_77(IntPtr bi);

			[DllImport("__Internal")]
			static extern void u_getVersion_77(out UVersionInfo versionInfo);

			[DllImport("__Internal")]
			static extern void u_versionToString_77(IntPtr versionArray, IntPtr versionString);

			[DllImport("__Internal")]
			static extern IntPtr u_errorName_77(int code);
		}
	}
}
