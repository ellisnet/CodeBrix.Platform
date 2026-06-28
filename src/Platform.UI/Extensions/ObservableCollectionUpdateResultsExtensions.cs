using System.Linq;

namespace CodeBrix.Platform.Extensions
{
	internal static class ObservableCollectionUpdateResultsExtensions
	{
		internal static bool HasChanged<T>(this ObservableCollectionUpdateResults<T> observableCollection)
		{
			return observableCollection.Moved.Any()
				|| observableCollection.Added.Any()
				|| observableCollection.Removed.Any();
		}
	}
}
