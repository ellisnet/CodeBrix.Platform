#nullable enable

extern alias __codebrix;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using CodeBrix.Platform.Extensions.Equality;
using CodeBrix.Platform.Extensions;

namespace CodeBrix.Platform.UI.SourceGenerators.XamlGenerator; //Was previously: Uno.UI.SourceGenerators.XamlGenerator

internal partial class XamlCodeGeneration
{
	private static readonly ConcurrentDictionary<ResourceCacheKey, CachedResource> _cachedResources = new();
	private static readonly TimeSpan _cacheEntryLifetime = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

	private static void ScavengeCache()
	{
		_cachedResources.Remove(kvp => DateTimeOffset.Now - kvp.Value.LastTimeUsed > _cacheEntryLifetime);
	}

	/// <summary>
	/// Identifies one parsed resource file in <see cref="_cachedResources"/>.
	/// </summary>
	/// <remarks>
	/// The cache is static, so it is shared by every compilation the compiler server runs, and the cached
	/// <see cref="ResourceDetails"/> carry the name of the assembly that parsed the file
	/// (<see cref="ResourceDetails.Assembly"/>, which decides <c>ResourceDetailsCollection.HasLocalResources</c>,
	/// i.e. the generated <c>CodeBrixHasLocalizationResources</c> attribute, and the x:Uid resource paths).
	/// Two projects that compile the SAME resw file under different assembly names (CodeBrix.Platform.UI.Core
	/// and the unit-test flavour CodeBrix.Platform.UI) must therefore never share an entry: the assembly name is
	/// part of the key. Without it, whichever project compiled first decided the attribute for the other.
	/// WPE1-13: the name the details carry, and so the key, is the RESOURCE MAP name
	/// (<c>_resourceMapLibraryName</c>: $(CodeBrixResourceMapLibraryName), else the assembly name).
	/// </remarks>
	private struct ResourceCacheKey : IEquatable<ResourceCacheKey>
	{
		public ResourceCacheKey(string assemblyName, string file, ImmutableArray<byte> checksum)
		{
			AssemblyName = assemblyName;
			File = file;
			Checksum = checksum;
		}

		public string AssemblyName { get; }
		public string File { get; }
		public ImmutableArray<byte> Checksum { get; }

		public override bool Equals(object? obj)
			=> obj is ResourceCacheKey key && Equals(key);

		public bool Equals(ResourceCacheKey other)
			=> AssemblyName == other.AssemblyName && File == other.File && ByteSequenceComparer.Equals(Checksum, other.Checksum);

		public override int GetHashCode()
		{
			var hashCode = 682997901;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssemblyName);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(File);
			hashCode = hashCode * -1521134295 + ByteSequenceComparer.GetHashCode(Checksum);
			return hashCode;
		}

		public static bool operator ==(ResourceCacheKey left, ResourceCacheKey right) => left.Equals(right);
		public static bool operator !=(ResourceCacheKey left, ResourceCacheKey right) => !(left == right);
	}

	private struct CachedResource : IEquatable<CachedResource>
	{
		public DateTimeOffset LastTimeUsed { get; }
		public ResourceDetails[] ResourceKeys { get; }

		public CachedResource(DateTimeOffset lastTimeUsed, ResourceDetails[] resourceKeys)
		{
			LastTimeUsed = lastTimeUsed;
			ResourceKeys = resourceKeys;
		}

		public CachedResource WithUpdatedLastTimeUsed()
		{
			return new CachedResource(DateTimeOffset.Now, ResourceKeys);
		}

		public override bool Equals(object? obj) => obj is CachedResource resource && Equals(resource);
		public bool Equals(CachedResource other) => LastTimeUsed.Equals(other.LastTimeUsed) && EqualityComparer<ResourceDetails[]>.Default.Equals(ResourceKeys, other.ResourceKeys);

		public override int GetHashCode()
		{
			var hashCode = 1975215354;
			hashCode = hashCode * -1521134295 + LastTimeUsed.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<ResourceDetails[]>.Default.GetHashCode(ResourceKeys);
			return hashCode;
		}

		public static bool operator ==(CachedResource left, CachedResource right) => left.Equals(right);
		public static bool operator !=(CachedResource left, CachedResource right) => !(left == right);
	}
}
