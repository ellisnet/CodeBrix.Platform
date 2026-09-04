using System.Runtime.CompilerServices;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

//An application head calls the add-in's generated resource entry points at startup; a host-free
//test process has no head, so this calls them itself. Without it Themes/Generic.xaml is never
//registered, DefaultStyleKey finds nothing, a ToolBar has no template and therefore no items
//panel, and every layout assertion in this suite would measure an empty control.
//
//MEASURED (wave 2, LAYOUT stream): with no call, `new ToolBar()` measured 0x0 and its Template was
//null after a measure pass; with the two calls below it measured its padding and border and its
//PART_ItemsHost panel was present.

/// <summary>
/// Registers the add-in's default styles with the framework so a host-free test can give a control
/// its template.
/// </summary>
/// <remarks>
/// Both calls are idempotent - the generated code guards itself - so it does not matter how many
/// module initializers in this suite ask for them.
/// </remarks>
internal static class DefaultStyleInitializer
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		//Module initializers run in an order nothing here controls, and registering a resource
		//dictionary creates one, which asks the dispatcher whether it has thread access. MEASURED:
		//without this line the first run threw NullReferenceException in
		//NativeDispatcher.GetHasThreadAccess, because the styles were registered before the
		//dispatcher bootstrap. DispatcherInitializer.Initialize only fills in what is still null,
		//so calling it here and letting the runtime call it again is harmless.
		DispatcherInitializer.Initialize();

		GlobalStaticResources.Initialize();
		GlobalStaticResources.RegisterDefaultStyles();
	}
}
