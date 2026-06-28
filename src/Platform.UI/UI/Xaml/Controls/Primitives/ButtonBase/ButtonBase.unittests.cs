using CodeBrix.Platform;
using CodeBrix.Platform.Client;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using System.Threading;

using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase : ContentControl
	{
		private readonly SerialDisposable _touchSubscription = new SerialDisposable();
		private readonly SerialDisposable _isEnabledSubscription = new SerialDisposable();

		partial void OnUnloadedPartial()
		{
			_isEnabledSubscription.Disposable = null;
			_touchSubscription.Disposable = null;
		}
	}
}
