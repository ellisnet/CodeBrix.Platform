#nullable enable

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Localization;

public static partial class PlatformStrings
{
	// Georgian and Armenian render only where the application's font covers
	// those scripts; no font bundled with CodeBrix.Platform does yet, so their
	// text may show as missing-glyph boxes. The strings themselves are correct
	// and entirely within the Basic Multilingual Plane, so nothing here needs
	// to change when a suitable font is bundled.
	static IReadOnlyList<PlatformStringSet> Caucasus =>
	[
		new PlatformStringSet
		{
			Code = "ka",
			Cancel = "გაუქმება",
			Create = "შექმნა",
			FolderNamePlaceholder = "საქაღალდის სახელი",
			KeepEditing = "რედაქტირების გაგრძელება",
			NameLabel = "სახელი:",
			NavigateUp = "ზემოთ",
			NewFolder = "ახალი საქაღალდე",
			NoItems = "ერთეულები არ არის",
			Open = "გახსნა",
			OpenFileTitle = "ფაილის გახსნა",
			Replace = "ჩანაცვლება",
			ReplaceFileFormat = "ჩანაცვლდეს „{0}“?",
			Save = "შენახვა",
			SaveFileTitle = "ფაილის შენახვა",
			SelectFolderTitle = "საქაღალდის არჩევა",
			Ok = "კარგი",
			Yes = "დიახ",
			No = "არა",

			InformationTitle = "ინფორმაცია",
			ErrorTitle = "შეცდომა",
			ErrorOccurredLabel = "მოხდა შეცდომა:",
			DetailsLabel = "დეტალები:",
			ConfirmTitle = "დარწმუნებული ხართ?",
			KeyAbc = "აბგ",
			KeyTab = "Tab",
			KeyEnter = "Enter",
			KeyShift = "Shift",
			KeyShiftUpper = "SHIFT",
			KeyBackspace = "წაშლა",
		},

		new PlatformStringSet
		{
			Code = "hy",
			Cancel = "Չեղարկել",
			Create = "Ստեղծել",
			FolderNamePlaceholder = "Պանակի անունը",
			KeepEditing = "Շարունակել խմբագրումը",
			NameLabel = "Անուն՝",
			NavigateUp = "Վեր",
			NewFolder = "Նոր պանակ",
			NoItems = "Տարրեր չկան",
			Open = "Բացել",
			OpenFileTitle = "Բացել ֆայլը",
			Replace = "Փոխարինել",
			ReplaceFileFormat = "Փոխարինե՞լ «{0}»-ը",
			Save = "Պահպանել",
			SaveFileTitle = "Պահպանել ֆայլը",
			SelectFolderTitle = "Ընտրել պանակը",
			Ok = "Լավ",
			Yes = "Այո",
			No = "Ոչ",

			InformationTitle = "Տեղեկություն",
			ErrorTitle = "Սխալ",
			ErrorOccurredLabel = "Տեղի ունեցավ սխալ:",
			DetailsLabel = "Մանրամասներ:",
			ConfirmTitle = "Վստա՞հ եք",
			KeyAbc = "ԱԲԳ",
			KeyTab = "Tab",
			KeyEnter = "Enter",
			KeyShift = "Shift",
			KeyShiftUpper = "SHIFT",
			KeyBackspace = "Ջնջել",
		},
	];
}
