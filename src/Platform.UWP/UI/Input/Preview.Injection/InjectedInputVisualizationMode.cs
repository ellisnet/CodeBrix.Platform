#nullable enable

using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;

namespace Windows.UI.Input.Preview.Injection;

public enum InjectedInputVisualizationMode
{
	None = 0,
	Default = 1,

	[NotImplemented] // Not supported yet
	Indirect = 2,
}
