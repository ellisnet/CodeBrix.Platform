#nullable enable

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// The value the linker-hint passes, and the final trim, give the hints that gate the generated
	/// <c>BindableMetadataProvider</c>s: always <c>true</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// An assembly compiled with <c>CodeBrixXamlResourcesTrimming=true</c> gates its whole generated
	/// <c>BindableMetadataProvider</c> (its registrations, or its type switch) behind the hint
	/// <c>Is_&lt;RootNamespace&gt;_BindableMetadataProvider_Available</c>. The provider class is not a DependencyObject,
	/// so no pass ever found it "available": the hint was false in every pass AND in the final trim, and the
	/// application's bindable metadata was compiled away entirely. The generated metadata is what creates a navigated
	/// page (Frame.CreatePageInstance asks it first - the equivalent of WinUI's generated XAML type info activators), so
	/// the page's public parameterless constructor was trimmed and <c>Frame.Navigate(typeof(MainPage))</c> failed at
	/// startup with MissingMethodException (decision D10, WPE1-11); {Binding} lost its generated accessors as well.
	/// </para>
	/// <para>
	/// With the provider hints on in every pass, the passes model the program the final trim produces: the provider's
	/// registration of each DependencyObject type stays gated by that type's own hint, so a XAML type nothing uses is
	/// still dropped, while a page the application names with <c>typeof</c> survives pass 1, gets its metadata from pass 2
	/// on, and its constructor - and so its InitializeComponent and the XAML types it creates - is rooted in the passes
	/// that compute the other hints, and in the final trim.
	/// </para>
	/// </remarks>
	internal static class LinkerHintBindableMetadata
	{
		/// <summary>The end of the name of every generated BindableMetadataProvider's linker hint.</summary>
		internal const string ProviderHintSuffix = "_BindableMetadataProvider_Available";

		/// <summary>Whether a linker hint is the one that gates a generated BindableMetadataProvider.</summary>
		/// <param name="hint">The hint (feature) name.</param>
		/// <returns><c>true</c> for a provider hint.</returns>
		internal static bool IsProviderHint(string hint)
			=> hint.StartsWith("Is_", StringComparison.Ordinal)
				&& hint.EndsWith(ProviderHintSuffix, StringComparison.Ordinal);

		/// <summary>The value of a hint in the first pass: <c>true</c> for a provider hint, <c>false</c> for every other.</summary>
		/// <param name="hint">The hint (feature) name.</param>
		/// <returns>"true" or "false".</returns>
		internal static string InitialValue(string hint) => IsProviderHint(hint) ? "true" : "false";

		/// <summary>Sets every provider hint of a pass's resulting feature list to <c>true</c>.</summary>
		/// <param name="features">The feature list (hint name to "true"/"false"); changed in place.</param>
		internal static void EnableProviders(IDictionary<string, string> features)
		{
			foreach (var hint in new List<string>(features.Keys))
			{
				if (IsProviderHint(hint))
				{
					features[hint] = "true";
				}
			}
		}
	}
}
