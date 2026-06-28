#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Composition; //Was previously: Uno.UI.Composition

internal interface ICompositionPathCommandsProvider
{
	List<CompositionPathCommand> Commands { get; }
}
