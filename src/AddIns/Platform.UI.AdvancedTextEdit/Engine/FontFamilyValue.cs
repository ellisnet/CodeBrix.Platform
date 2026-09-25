#nullable enable

using System;
using System.Threading;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Engine;

/// <summary>
/// The neutral storage of a highlighting font family (WPE1 C8, the adapters form): the family NAME, plus the WinUI
/// FontFamily instance the public XAML-typed members hand out, created on demand. HighlightingColor and XshdColor keep one
/// of these instead of a FontFamily field, so the highlighting engine (xshd loading, DocumentHighlighter) never needs the
/// XAML object model; their public FontFamily properties are adapters over it. Immutable apart from the instance cache,
/// and shared by reference exactly where the FontFamily instance used to be shared.
/// </summary>
/// <remarks>
/// Equality and hashing reproduce the WinUI FontFamily's own: equal when the effective family (FontFamily.Source) is equal
/// (ordinal), the hash of the name the instance was created from. Source differs from the name only for the symbol-font
/// names the FontFamily constructor remaps (<see cref="IsRemappedName"/>); only then does equality need the instances.
/// </remarks>
internal sealed class FontFamilyValue
{
	private object? _instance;

	private FontFamilyValue(string name, int hashCode, bool nameIsSource, object? instance)
	{
		Name = name;
		HashCode = hashCode;
		NameIsSource = nameIsSource;
		_instance = instance;
	}

	/// <summary>The family name: the .xshd attribute value, or the Source of the FontFamily it was created from.</summary>
	internal string Name { get; }

	/// <summary>The hash code of the FontFamily (the hash of the name it was created from).</summary>
	internal int HashCode { get; }

	/// <summary>Whether <see cref="Name"/> is known to be the FontFamily's Source (the effective family).</summary>
	internal bool NameIsSource { get; }

	/// <summary>The FontFamily instance, if one exists yet (typed as object: no XAML type is named here).</summary>
	internal object? Instance => Volatile.Read(ref _instance);

	/// <summary>A family given by name (a .xshd file): the FontFamily instance is created on first request.</summary>
	/// <param name="name">The family name.</param>
	/// <returns>The value.</returns>
	internal static FontFamilyValue FromName(string name) => new(name, name.GetHashCode(), IsRemappedName(name) == false, null);

	/// <summary>A family given as an existing FontFamily instance (the public setters).</summary>
	/// <param name="instance">The FontFamily.</param>
	/// <param name="source">Its Source.</param>
	/// <param name="hashCode">Its hash code.</param>
	/// <returns>The value.</returns>
	internal static FontFamilyValue FromInstance(object instance, string source, int hashCode) => new(source, hashCode, true, instance);

	/// <summary>Returns the FontFamily instance, creating it once with <paramref name="create"/> when there is none.</summary>
	/// <param name="create">Creates the FontFamily from <see cref="Name"/> (the XAML-side adapter supplies it).</param>
	/// <returns>The instance.</returns>
	internal object GetOrCreateInstance(Func<string, object> create)
	{
		var instance = Volatile.Read(ref _instance);
		if (instance is null)
		{
			instance = create(Name);
			instance = Interlocked.CompareExchange(ref _instance, instance, null) ?? instance;
		}

		return instance;
	}

	/// <summary>
	/// Whether the WinUI FontFamily constructor would replace this name with the application's symbols font (the names
	/// it tests: containing "Segoe Fluent Icons" or "Segoe MDL2 Assets", or equal to "Symbols", ignoring case).
	/// </summary>
	/// <param name="name">A family name.</param>
	/// <returns>True when Source may differ from the name.</returns>
	internal static bool IsRemappedName(string name) =>
		name.Contains("Segoe Fluent Icons", StringComparison.InvariantCultureIgnoreCase) ||
		name.Contains("Segoe MDL2 Assets", StringComparison.InvariantCultureIgnoreCase) ||
		name.Equals("Symbols", StringComparison.InvariantCultureIgnoreCase);

	/// <summary>
	/// FontFamily equality over two optional values (null only equals null). When both names are the effective family,
	/// the names are compared; otherwise <paramref name="instancesEqual"/> compares the FontFamily instances (the XAML-side
	/// adapter), which happens only for a symbol-font name.
	/// </summary>
	/// <param name="a">One value.</param>
	/// <param name="b">The other.</param>
	/// <param name="instancesEqual">Compares the two values through their FontFamily instances.</param>
	/// <returns>Whether the families are equal.</returns>
	internal static bool AreEqual(FontFamilyValue? a, FontFamilyValue? b, Func<FontFamilyValue, FontFamilyValue, bool> instancesEqual)
	{
		if (a is null || b is null)
		{
			return a is null && b is null;
		}

		if (ReferenceEquals(a, b))
		{
			return true;
		}

		if (a.NameIsSource && b.NameIsSource)
		{
			return string.Equals(a.Name, b.Name, StringComparison.Ordinal);
		}

		return instancesEqual(a, b);
	}
}
