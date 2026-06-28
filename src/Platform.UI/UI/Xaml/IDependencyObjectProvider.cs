using CodeBrix.Platform.Extensions.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Diagnostics.Eventing;

namespace Microsoft.UI.Xaml
{
	public interface IDependencyObjectStoreProvider
	{
		DependencyObjectStore Store { get; }
	}
}
