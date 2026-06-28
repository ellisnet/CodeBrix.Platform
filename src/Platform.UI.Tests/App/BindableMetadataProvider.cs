using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.DataBinding;

namespace CodeBrix.Platform.UI.Tests //Was previously: Uno.UI.Tests
{
	// Added manually because for some reason it's not generated
	public class BindableMetadataProvider : IBindableMetadataProvider
	{
		public IBindableType GetBindableTypeByFullName(string fullName)
		{
			return null;
		}

		public IBindableType GetBindableTypeByType(Type type)
		{
			return null;
		}
	}
}
