using System.Runtime.CompilerServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Svg;
using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// Registers the Svg add-in as the framework's SVG provider, the way an application head does.
/// </summary>
/// <remarks>
/// <para>
/// In an application the registration is generated into App.xaml's code-behind: the XAML generator
/// walks the referenced assemblies for their <c>ApiExtension</c> attributes and emits one
/// <c>ApiExtensibility.Register</c> call each. A test process has no App.xaml, so nothing registers
/// the provider and every <c>SvgImageSource</c> would quietly render nothing.
/// </para>
/// <para>
/// This runs from a module initializer, before any test and therefore before the first image source
/// is constructed and caches whether it found a provider.
/// </para>
/// </remarks>
internal static class SvgProviderRegistration
{
	[ModuleInitializer]
	internal static void Initialize()
	{
		if (!ApiExtensibility.IsRegistered<ISvgProvider>())
		{
			ApiExtensibility.Register(typeof(ISvgProvider), owner => new SvgProvider(owner));
		}
	}
}
