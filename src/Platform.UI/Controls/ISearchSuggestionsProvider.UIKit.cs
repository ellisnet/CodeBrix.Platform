using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;
using System.Threading.Tasks;
using System;
using CT = System.Threading.CancellationToken;

namespace CodeBrix.Platform.UI.Controls //Was previously: Uno.UI.Controls
{
	public interface ISearchSuggestionsProvider
	{
		Task<SearchSuggestion[]> GetSuggestions(CT ct, string queryText);
	}
}
