using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeBrix.Platform.UI.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Tests.Localization
{
	[TestClass]
	public class Given_PlatformStrings
	{
		// The software keyboard's layout ids, which are also the language codes
		// CodeBrix.Develop offers for the frame-buffer emulator. Every one of
		// them must reach a set of its own rather than falling back.
		private static readonly string[] LayoutLanguages =
		[
			"en", "en-GB", "de", "de-CH", "fr", "fr-BE", "fr-CH", "nl",
			"es", "pt", "it", "mt", "sq", "tr", "el",
			"da", "no", "sv", "fi", "is", "lt", "lv", "et",
			"pl", "cs", "sk", "hu", "ro", "hr", "sr-Latn",
			"ru", "uk", "be", "bg", "sr", "mk",
			"ka", "hy",
		];

		private CultureInfo _originalUICulture;

		[TestInitialize]
		public void Setup() => _originalUICulture = CultureInfo.CurrentUICulture;

		[TestCleanup]
		public void Cleanup() => CultureInfo.CurrentUICulture = _originalUICulture;

		[TestMethod]
		public void When_Enumerating_Then_Every_Keyboard_Layout_Language_Is_Present()
		{
			var supported = PlatformStrings.SupportedLanguages.ToList();

			foreach (var language in LayoutLanguages)
			{
				Assert.IsTrue(supported.Contains(language, StringComparer.OrdinalIgnoreCase),
					$"No string set for the '{language}' keyboard layout.");
			}
		}

		[TestMethod]
		public void When_Culture_Is_A_Layout_Language_Then_No_String_Is_Blank()
		{
			foreach (var language in LayoutLanguages)
			{
				CultureInfo.CurrentUICulture = new CultureInfo(language);

				foreach (var (name, value) in ReadAll())
				{
					Assert.IsFalse(string.IsNullOrWhiteSpace(value),
						$"{language}: {name} is blank.");
				}
			}
		}

		[TestMethod]
		public void When_Culture_Is_German_Then_Strings_Are_German()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("de");

			Assert.AreEqual("Abbrechen", PlatformStrings.Cancel);
			Assert.AreEqual("Speichern", PlatformStrings.Save);
			Assert.AreEqual("Ja", PlatformStrings.Yes);
			Assert.AreEqual("Umschalt", PlatformStrings.KeyShift);
		}

		[TestMethod]
		public void When_Culture_Is_A_Regional_Variant_Without_A_Set_Then_It_Falls_Back_To_The_Language()
		{
			// Austrian German has no set; German does.
			CultureInfo.CurrentUICulture = new CultureInfo("de-AT");

			Assert.AreEqual("Abbrechen", PlatformStrings.Cancel);
		}

		[TestMethod]
		public void When_Culture_Has_No_Set_At_All_Then_It_Falls_Back_To_English()
		{
			// Japanese is a keyboard layout CodeBrix.Platform does not carry;
			// English words beat an exception in a file picker.
			CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");

			Assert.AreEqual("Cancel", PlatformStrings.Cancel);
			Assert.AreEqual("Save", PlatformStrings.Save);
		}

		[TestMethod]
		public void When_Culture_Is_A_Regional_Variant_With_Its_Own_Set_Then_That_Set_Wins()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("de-CH");
			var swiss = PlatformStrings.ReplaceFile("a.txt");

			CultureInfo.CurrentUICulture = new CultureInfo("de");
			var german = PlatformStrings.ReplaceFile("a.txt");

			// Swiss German quotes with guillemets; both name the same file.
			Assert.AreNotEqual(german, swiss);
			StringAssert.Contains(swiss, "«a.txt»");
			StringAssert.Contains(german, "„a.txt“");
		}

		[TestMethod]
		public void When_Culture_Is_Serbian_Then_The_Script_Decides_The_Words()
		{
			// This pair is why the sets are keyed on the full tag: a resource
			// loader matching on base culture alone cannot tell them apart.
			CultureInfo.CurrentUICulture = new CultureInfo("sr");
			Assert.AreEqual("Откажи", PlatformStrings.Cancel);

			CultureInfo.CurrentUICulture = new CultureInfo("sr-Latn");
			Assert.AreEqual("Otkaži", PlatformStrings.Cancel);
		}

		[TestMethod]
		public void When_Culture_Changes_Then_The_Next_Read_Follows_It()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("fr");
			Assert.AreEqual("Annuler", PlatformStrings.Cancel);

			CultureInfo.CurrentUICulture = new CultureInfo("pl");
			Assert.AreEqual("Anuluj", PlatformStrings.Cancel);
		}

		[TestMethod]
		public void When_Formatting_A_Replace_Prompt_Then_The_File_Name_Is_Carried()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("en");

			StringAssert.Contains(PlatformStrings.ReplaceFile("report.pdf"), "report.pdf");
		}

		[TestMethod]
		public void When_Reading_Letters_Key_Then_It_Uses_The_Languages_Own_Alphabet()
		{
			CultureInfo.CurrentUICulture = new CultureInfo("ru");
			Assert.AreEqual("АБВ", PlatformStrings.KeyAbc);

			CultureInfo.CurrentUICulture = new CultureInfo("el");
			Assert.AreEqual("ΑΒΓ", PlatformStrings.KeyAbc);

			CultureInfo.CurrentUICulture = new CultureInfo("en");
			Assert.AreEqual("ABC", PlatformStrings.KeyAbc);
		}

		private static IEnumerable<(string Name, string Value)> ReadAll()
		{
			yield return (nameof(PlatformStrings.Cancel), PlatformStrings.Cancel);
			yield return (nameof(PlatformStrings.Create), PlatformStrings.Create);
			yield return (nameof(PlatformStrings.FolderNamePlaceholder), PlatformStrings.FolderNamePlaceholder);
			yield return (nameof(PlatformStrings.KeepEditing), PlatformStrings.KeepEditing);
			yield return (nameof(PlatformStrings.NameLabel), PlatformStrings.NameLabel);
			yield return (nameof(PlatformStrings.NewFolder), PlatformStrings.NewFolder);
			yield return (nameof(PlatformStrings.NoItems), PlatformStrings.NoItems);
			yield return (nameof(PlatformStrings.Open), PlatformStrings.Open);
			yield return (nameof(PlatformStrings.OpenFileTitle), PlatformStrings.OpenFileTitle);
			yield return (nameof(PlatformStrings.Replace), PlatformStrings.Replace);
			yield return (nameof(PlatformStrings.Save), PlatformStrings.Save);
			yield return (nameof(PlatformStrings.SaveFileTitle), PlatformStrings.SaveFileTitle);
			yield return (nameof(PlatformStrings.SelectFolderTitle), PlatformStrings.SelectFolderTitle);
			yield return (nameof(PlatformStrings.Ok), PlatformStrings.Ok);
			yield return (nameof(PlatformStrings.Yes), PlatformStrings.Yes);
			yield return (nameof(PlatformStrings.No), PlatformStrings.No);
			yield return (nameof(PlatformStrings.KeyAbc), PlatformStrings.KeyAbc);
			yield return (nameof(PlatformStrings.KeyTab), PlatformStrings.KeyTab);
			yield return (nameof(PlatformStrings.KeyEnter), PlatformStrings.KeyEnter);
			yield return (nameof(PlatformStrings.KeyShift), PlatformStrings.KeyShift);
			yield return (nameof(PlatformStrings.KeyShiftUpper), PlatformStrings.KeyShiftUpper);
			yield return (nameof(PlatformStrings.KeyBackspace), PlatformStrings.KeyBackspace);
			yield return ("ReplaceFile", PlatformStrings.ReplaceFile("x"));
		}
	}
}
