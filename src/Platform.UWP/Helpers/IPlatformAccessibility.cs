namespace CodeBrix.Platform.Helpers;

internal interface ICodeBrixAccessibility
{
	bool IsAccessibilityEnabled { get; }

	// See live regions documentation for more information
	// https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/ARIA_Live_Regions#live_regions
	void AnnouncePolite(string text);
	void AnnounceAssertive(string text);
}
