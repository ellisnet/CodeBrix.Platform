using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using IAsyncOperation = System.Threading.Tasks.Task;

namespace Windows.UI.Popups
{
	public sealed partial class PopupMenu
	{
		[CodeBrix.Platform.NotImplemented]
		public PopupMenu()
		{
			throw new NotImplementedException();
		}

		[CodeBrix.Platform.NotImplemented]
		public Task<IUICommand> ShowForSelectionAsync()
		{
			throw new NotImplementedException();
		}

		[CodeBrix.Platform.NotImplemented]
		public IAsyncOperation<IUICommand> ShowForSelectionAsync(Rect selection)
		{
			throw new NotImplementedException();
		}

		[CodeBrix.Platform.NotImplemented]
		public IAsyncOperation<IUICommand> ShowForSelectionAsync(
		  Rect selection,
		  Placement preferredPlacement
		)
		{
			throw new NotImplementedException();
		}
	}
}
