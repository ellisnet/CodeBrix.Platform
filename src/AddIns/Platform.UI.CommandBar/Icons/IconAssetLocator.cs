using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Turns the URI an icon was given into the URI the platform's image loading is handed.
/// </summary>
/// <remarks>
/// <para>
/// Two jobs. The first is the standard scale qualifier: an application that ships
/// <c>open.png</c> beside <c>open.scale-125.png</c> and <c>open.scale-200.png</c> wants the variant
/// that matches the display, and the choice has to be made where the file is, not where the icon
/// is declared. The second is the embedded-resource scheme, whose bytes live inside an assembly and
/// therefore have no URI the platform's raster decoder can open - those are written once to a
/// per-user temporary file, whose name is derived from the resource URI, so the decoder can read
/// them like any other file.
/// </para>
/// <para>
/// Everything here is filesystem-only and side-effect-free apart from that one cached write.
/// </para>
/// </remarks>
internal static class IconAssetLocator
{
	/// <summary>The scale qualifiers the platform's asset convention defines.</summary>
	private static readonly int[] ScaleQualifiers = [100, 125, 150, 200, 400];

	private static readonly ConcurrentDictionary<string, Uri?> _scaleVariants = new(StringComparer.Ordinal);
	private static readonly ConcurrentDictionary<string, Uri?> _materialized = new(StringComparer.Ordinal);

	/// <summary>
	/// The URI to hand the platform for one icon at one display scale.
	/// </summary>
	/// <param name="source">The URI the icon was given.</param>
	/// <param name="scale">The display scale, where 1.0 is 100%.</param>
	/// <returns>
	/// The scale variant when one exists on disk, a temporary file when the URI names an embedded
	/// resource, otherwise <paramref name="source"/> unchanged. Null when
	/// <paramref name="source"/> is null or names an embedded resource that cannot be found.
	/// </returns>
	internal static Uri? Resolve(Uri? source, double scale)
	{
		if (source is null)
		{
			return null;
		}

		if (IconResourceScheme.IsResourceUri(source))
		{
			return Materialize(source);
		}

		return ResolveScaleVariant(source, scale);
	}

	/// <summary>
	/// The best scale variant of <paramref name="source"/> that exists beside it.
	/// </summary>
	/// <param name="source">The URI the icon was given.</param>
	/// <param name="scale">The display scale, where 1.0 is 100%.</param>
	/// <returns>A <c>file:</c> URI naming the variant, or <paramref name="source"/> unchanged.</returns>
	internal static Uri ResolveScaleVariant(Uri source, double scale)
	{
		var key = string.Create(CultureInfo.InvariantCulture, $"{source}|{scale:0.####}");
		return _scaleVariants.GetOrAdd(key, _ => FindScaleVariant(source, scale)) ?? source;
	}

	private static Uri? FindScaleVariant(Uri source, double scale)
	{
		if (TryGetLocalPath(source) is not { } path)
		{
			return null;
		}

		var directory = Path.GetDirectoryName(path);
		var name = Path.GetFileNameWithoutExtension(path);
		var extension = Path.GetExtension(path);

		if (string.IsNullOrEmpty(directory)
			|| string.IsNullOrEmpty(name)
			|| name.Contains(".scale-", StringComparison.OrdinalIgnoreCase))
		{
			//A URI that already names a qualifier is an explicit choice; honour it.
			return null;
		}

		var wanted = (int)Math.Round(scale * 100d, MidpointRounding.AwayFromZero);

		Uri? best = null;
		var bestQualifier = int.MaxValue;
		Uri? largest = null;
		var largestQualifier = -1;
		var sawHundred = false;

		//The smallest variant that is big enough wins - upscaling a bitmap icon is what looks
		//wrong. Failing that, the biggest one there is.
		foreach (var qualifier in ScaleQualifiers)
		{
			var candidate = Path.Combine(directory, $"{name}.scale-{qualifier}{extension}");
			if (!File.Exists(candidate))
			{
				continue;
			}

			sawHundred |= qualifier == 100;
			Consider(qualifier, new Uri(candidate));
		}

		//A file named without a qualifier is the 100% artwork, unless an explicit .scale-100 file
		//says otherwise. Returning null for it means "use the URI exactly as it was given", which
		//keeps an ms-appx URI an ms-appx URI.
		if (!sawHundred && File.Exists(path))
		{
			Consider(100, null);
		}

		if (best is null && bestQualifier == int.MaxValue && largestQualifier < 0)
		{
			return null;
		}

		return bestQualifier != int.MaxValue ? best : largest;

		void Consider(int qualifier, Uri? candidate)
		{
			if (qualifier >= wanted && qualifier < bestQualifier)
			{
				bestQualifier = qualifier;
				best = candidate;
			}

			if (qualifier > largestQualifier)
			{
				largestQualifier = qualifier;
				largest = candidate;
			}
		}
	}

	/// <summary>
	/// The filesystem path a URI names, when it names one at all.
	/// </summary>
	/// <param name="uri">The URI to map.</param>
	/// <returns>An absolute path, or null for a URI that is not on this machine's disk.</returns>
	internal static string? TryGetLocalPath(Uri uri)
	{
		if (!uri.IsAbsoluteUri)
		{
			return null;
		}

		if (uri.IsFile)
		{
			return uri.LocalPath;
		}

		if (string.Equals(uri.Scheme, "ms-appx", StringComparison.OrdinalIgnoreCase))
		{
			//The same mapping the framework's own ms-appx reader uses: the package's installed
			//directory, the URI's host, then its path.
			var relative = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
			try
			{
				return Path.Combine(Package.Current.InstalledPath, uri.Host, relative);
			}
			catch (Exception)
			{
				//No package identity (a host-free process, for one): there is no local path.
				return null;
			}
		}

		return null;
	}

	/// <summary>
	/// Writes an embedded resource to a cached temporary file so a file-based decoder can read it.
	/// </summary>
	/// <param name="resourceUri">A <c>cb-res://</c> URI.</param>
	/// <returns>A <c>file:</c> URI, or null when the resource could not be found.</returns>
	internal static Uri? Materialize(Uri resourceUri)
		=> _materialized.GetOrAdd(resourceUri.ToString(), _ => MaterializeCore(resourceUri));

	private static Uri? MaterializeCore(Uri resourceUri)
	{
		if (!IconResourceScheme.TryOpen(resourceUri, out var stream))
		{
			return null;
		}

		using (stream)
		{
			try
			{
				var directory = Path.Combine(Path.GetTempPath(), "CodeBrix.Platform.CommandBar.Icons");
				Directory.CreateDirectory(directory);

				var extension = Path.GetExtension(Uri.UnescapeDataString(resourceUri.AbsolutePath));
				var file = Path.Combine(directory, StableName(resourceUri) + extension);

				if (!File.Exists(file))
				{
					//Written through a unique temporary name and moved into place, so two processes
					//racing on the same icon cannot read a half-written file.
					var scratch = file + "." + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".tmp";
					using (var output = File.Create(scratch))
					{
						stream.CopyTo(output);
					}

					File.Move(scratch, file, overwrite: true);
				}

				return new Uri(file);
			}
			catch (Exception)
			{
				//A read-only or full temporary directory means no icon, not a crash.
				return null;
			}
		}
	}

	private static string StableName(Uri resourceUri)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(resourceUri.ToString()));
		var builder = new StringBuilder(32);
		for (var i = 0; i < 16; i++)
		{
			builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
		}

		return builder.ToString();
	}
}
