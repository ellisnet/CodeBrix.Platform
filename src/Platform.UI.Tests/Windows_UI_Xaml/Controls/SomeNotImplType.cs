using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Tests.Windows_UI_Xaml.Controls //Was previously: Uno.UI.Tests.Windows_UI_Xaml.Controls
{
	[CodeBrix.Platform.NotImplemented]
	public class SomeNotImplType
	{
		public static int CreationAttempts { get; private set; }
		[CodeBrix.Platform.NotImplemented]
		public SomeNotImplType()
		{
			CreationAttempts++;
			throw new NotImplementedException();
		}
		public double SomeProperty { get; set; }
	}
}
