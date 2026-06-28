using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace CodeBrix.Platform.UI.Toolkit //Was previously: Uno.UI.Toolkit
{
	public static class UICommandExtensions
	{
		public static void SetDestructive(this UICommand command, bool isDestructive)
		{
#if false
			if (command == null)
			{
				return;
			}

			command.IsDestructive = isDestructive;
#endif
		}
	}
}
