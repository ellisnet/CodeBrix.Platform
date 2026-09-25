#if !__NETSTD_REFERENCE__
#nullable enable

using CodeBrix.Platform.UI.Composition.Contracts;

namespace Microsoft.UI.Composition
{
	public partial class CompositionCapabilities
	{
		public bool AreEffectsSupported() => CompositionPlatformServices.Composition.AreEffectsSupported(_compositor);

		public bool AreEffectsFast() => CompositionPlatformServices.Composition.AreEffectsFast(_compositor);
	}
}
#endif
