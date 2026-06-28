using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CodeBrix.Platform.Extensions.Specialized;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Documents
{
	[ContentProperty(Name = nameof(Inlines))]
	public partial class Span : Inline
	{
#if true
		public Span()
		{
			Inlines = new InlineCollection(this);
		}
#endif

		public InlineCollection Inlines { get; set; }
	}
}
