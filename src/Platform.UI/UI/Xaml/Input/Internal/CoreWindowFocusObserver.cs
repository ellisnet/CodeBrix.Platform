using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Xaml.Core;

namespace CodeBrix.Platform.UI.Xaml.Input; //Was previously: Uno.UI.Xaml.Input

internal class CoreWindowFocusObserver : FocusObserver
{
	internal CoreWindowFocusObserver(ContentRoot contentRoot) : base(contentRoot)
	{
	}
}
