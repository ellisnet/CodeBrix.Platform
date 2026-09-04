using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Wires an icon element to the icon source that created it.
/// </summary>
/// <remarks>
/// An icon source is a VALUE and an icon element is a VISUAL, and a source can be asked for several
/// elements - one per button showing that icon. Binding rather than copying is what keeps them in
/// step: change the tint on the source and every element it made follows. This is the same
/// arrangement the framework's own <c>IconSourceElement</c> uses for its built-in icon kinds.
/// </remarks>
internal static class IconBinding
{
	/// <summary>
	/// Binds one property of an element to the same-named property of its source.
	/// </summary>
	/// <param name="target">The icon element.</param>
	/// <param name="property">The element property to bind.</param>
	/// <param name="source">The icon source to bind from.</param>
	/// <param name="path">The source property's name.</param>
	internal static void Bind(DependencyObject target, DependencyProperty property, object source, string path)
	{
		var binding = new Binding
		{
			Source = source,
			Path = new PropertyPath(path),
			Mode = BindingMode.OneWay,
		};

		BindingOperations.SetBinding(target, property, binding);
	}
}
