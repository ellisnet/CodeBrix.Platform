#nullable enable

using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Tests;

/// <summary>
/// The one font choice in the suite that has to come from the machine rather than from a literal.
/// </summary>
/// <remarks>
/// <para>
/// Everything else here lays text out with the generic family "sans-serif" and deliberately never
/// asserts what that resolves to. That is fine for geometry, but it is not enough for WEIGHT: a
/// platform that does not recognise a family name substitutes its default face, and Windows does so
/// without honouring the requested weight - so bold and regular come back as the same typeface, and
/// asserting that bold measures wider would compare a face against itself.
/// </para>
/// <para>
/// "sans-serif" is still tried first, so platforms that do resolve the alias properly (Linux, via
/// fontconfig) keep using exactly the font they always have. Only when the alias fails to produce
/// two distinct weights does this go looking through the installed families.
/// </para>
/// </remarks>
internal static class TestFonts
{
	private static readonly string? _boldCapableFamily = FindBoldCapableFamily();

	/// <summary>
	/// A family name this machine resolves to genuinely distinct regular and bold faces, or null
	/// when it has no such font installed.
	/// </summary>
	public static string? BoldCapableFamily => _boldCapableFamily;

	private static string? FindBoldCapableFamily()
	{
		const string preferred = "sans-serif";

		if (HasDistinctBold(preferred, requireExactFamilyName: false))
		{
			return preferred;
		}

		using var manager = SKFontManager.CreateDefault();

		foreach (var family in manager.GetFontFamilies())
		{
			if (HasDistinctBold(family, requireExactFamilyName: true))
			{
				return family;
			}
		}

		return null;
	}

	private static bool HasDistinctBold(string family, bool requireExactFamilyName)
	{
		using var regular = SKTypeface.FromFamilyName(
			family, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
		using var bold = SKTypeface.FromFamilyName(
			family, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

		if (regular is null || bold is null)
		{
			return false;
		}

		// Asking for a real family name and getting a different one back IS the substitution case,
		// so reject it. The generic alias is exempt: resolving to another name is what it is for.
		if (requireExactFamilyName
			&& (regular.FamilyName != family || bold.FamilyName != family))
		{
			return false;
		}

		// A monospaced face satisfies "two distinct weights" while advancing identically for both,
		// which would make the wider-when-bold assertion fail for an entirely legitimate reason.
		if (regular.IsFixedPitch || bold.IsFixedPitch)
		{
			return false;
		}

		return regular.FontWeight < (int)SKFontStyleWeight.SemiBold
			&& bold.FontWeight >= (int)SKFontStyleWeight.SemiBold;
	}
}
