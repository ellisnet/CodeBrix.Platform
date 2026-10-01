namespace Microsoft.UI.Xaml.Controls;

// Optional host service. A host must explicitly register it before creating the app.
internal interface INativeMenuBarExtension
{
	void Load();
	void Unload();
}
