#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Engine;

/// <summary>
/// The completion list's matching and ranking (WPE1 C8), extracted from CodeCompletion/CompletionList so that a view with
/// no XAML (a CodeBrix.Mobile completion popup) filters and ranks exactly as the editor's own list does. Generic over the
/// item type: the caller supplies each item's text and priority.
/// </summary>
internal static class CompletionFilter
{
	/// <summary>
	/// The quality of a match of <paramref name="query"/> against an item's text: 8 = full match case sensitive,
	/// 7 = full match, 6 = match start case sensitive, 5 = match start, 4 = CamelCase match when the query is 1 or 2
	/// characters, 3 = substring case sensitive, 2 = substring (3 and 2 only when filtering), 1 = CamelCase match,
	/// -1 = no match.
	/// </summary>
	/// <param name="itemText">The item's text (never null).</param>
	/// <param name="query">What was typed.</param>
	/// <param name="isFiltering">Whether the list filters (substring matches count only then).</param>
	/// <returns>The quality.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="itemText"/> is null.</exception>
	internal static int GetMatchQuality(string itemText, string query, bool isFiltering)
	{
		if (itemText == null)
			throw new ArgumentNullException(nameof(itemText), "ICompletionData.Text returned null");

		// Qualities:
		//  	8 = full match case sensitive
		// 		7 = full match
		// 		6 = match start case sensitive
		//		5 = match start
		//		4 = match CamelCase when length of query is 1 or 2 characters
		// 		3 = match substring case sensitive
		//		2 = match substring
		//		1 = match CamelCase
		//		-1 = no match
		if (query == itemText)
			return 8;
		if (string.Equals(itemText, query, StringComparison.InvariantCultureIgnoreCase))
			return 7;

		if (itemText.StartsWith(query, StringComparison.InvariantCulture))
			return 6;
		if (itemText.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
			return 5;

		bool? camelCaseMatch = null;
		if (query.Length <= 2)
		{
			camelCaseMatch = CamelCaseMatch(itemText, query);
			if (camelCaseMatch == true)
				return 4;
		}

		// search by substring, if filtering (i.e. new behavior) turned on
		if (isFiltering)
		{
			if (itemText.IndexOf(query, StringComparison.InvariantCulture) >= 0)
				return 3;
			if (itemText.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
				return 2;
		}

		if (!camelCaseMatch.HasValue)
			camelCaseMatch = CamelCaseMatch(itemText, query);
		if (camelCaseMatch == true)
			return 1;

		return -1;
	}

	/// <summary>Whether the query matches the first letters of the text's words (camelCase or PascalCase).</summary>
	/// <param name="text">The item text.</param>
	/// <param name="query">What was typed.</param>
	/// <returns>Whether it matches.</returns>
	internal static bool CamelCaseMatch(string text, string query)
	{
		// We take the first letter of the text regardless of whether or not it's upper case so we match
		// against camelCase text as well as PascalCase text ("cct" matches "camelCaseText")
		IEnumerable<char> theFirstLetterOfEachWord = text.Take(1).Concat(text.Skip(1).Where(char.IsUpper));

		int i = 0;
		foreach (char letter in theFirstLetterOfEachWord)
		{
			if (i > query.Length - 1)
				return true;    // return true here for CamelCase partial match ("CQ" matches "CodeQualityAnalysis")
			if (char.ToUpperInvariant(query[i]) != char.ToUpperInvariant(letter))
				return false;
			i++;
		}
		if (i >= query.Length)
			return true;
		return false;
	}

	/// <summary>
	/// Filters the items to those matching <paramref name="query"/> (quality above 0, in their original order) and finds
	/// the best one: the highest quality, then the highest priority, where the currently suggested item counts as the
	/// highest priority.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="items">The items to filter.</param>
	/// <param name="getText">An item's text.</param>
	/// <param name="getPriority">An item's priority.</param>
	/// <param name="query">What was typed.</param>
	/// <param name="suggestedItem">The item selected before this query, if any (compared by reference).</param>
	/// <param name="bestIndex">The index of the best match in the result, or -1 when nothing matches.</param>
	/// <returns>The matching items.</returns>
	internal static List<T> Filter<T>(IEnumerable<T> items, Func<T, string> getText, Func<T, double> getPriority,
		string query, T? suggestedItem, out int bestIndex)
		where T : class
	{
		var matchingItems =
			from item in items
			let quality = GetMatchQuality(getText(item), query, isFiltering: true)
			where quality > 0
			select new { Item = item, Quality = quality };

		List<T> result = new List<T>();
		bestIndex = -1;
		int bestQuality = -1;
		double bestPriority = 0;
		int i = 0;
		foreach (var matchingItem in matchingItems)
		{
			double priority = ReferenceEquals(matchingItem.Item, suggestedItem) ? double.PositiveInfinity : getPriority(matchingItem.Item);
			int quality = matchingItem.Quality;
			if (quality > bestQuality || (quality == bestQuality && (priority > bestPriority)))
			{
				bestIndex = i;
				bestPriority = priority;
				bestQuality = quality;
			}
			result.Add(matchingItem.Item);
			i++;
		}
		return result;
	}

	/// <summary>
	/// Finds the item that best matches <paramref name="query"/> without filtering: the highest quality, then the
	/// currently suggested item, then the highest priority.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="items">The items.</param>
	/// <param name="getText">An item's text.</param>
	/// <param name="getPriority">An item's priority.</param>
	/// <param name="query">What was typed.</param>
	/// <param name="suggestedIndex">The index of the item selected before this query, or -1.</param>
	/// <param name="isFiltering">Whether the list filters (see <see cref="GetMatchQuality"/>).</param>
	/// <returns>The index of the best match, or -1 when nothing matches.</returns>
	internal static int FindBestMatch<T>(IList<T> items, Func<T, string> getText, Func<T, double> getPriority,
		string query, int suggestedIndex, bool isFiltering)
	{
		int bestIndex = -1;
		int bestQuality = -1;
		double bestPriority = 0;
		for (int i = 0; i < items.Count; ++i)
		{
			int quality = GetMatchQuality(getText(items[i]), query, isFiltering);
			if (quality < 0)
				continue;

			double priority = getPriority(items[i]);
			bool useThisItem;
			if (bestQuality < quality)
			{
				useThisItem = true;
			}
			else
			{
				if (bestIndex == suggestedIndex)
				{
					useThisItem = false;
				}
				else if (i == suggestedIndex)
				{
					// prefer recommendedItem, regardless of its priority
					useThisItem = bestQuality == quality;
				}
				else
				{
					useThisItem = bestQuality == quality && bestPriority < priority;
				}
			}
			if (useThisItem)
			{
				bestIndex = i;
				bestPriority = priority;
				bestQuality = quality;
			}
		}
		return bestIndex;
	}
}
