using System.Runtime.CompilerServices;

// The add-in's unit suite drives internals directly: the inherited attached properties'
// storage, the icon rasterisation cache and the tool bar's overflow partition are all
// implementation detail that must not widen the public surface just to be measurable.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.CommandBar.Unit.Tests")]
