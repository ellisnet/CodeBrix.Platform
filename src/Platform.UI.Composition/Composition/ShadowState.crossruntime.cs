#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using Microsoft.UI.Composition;
using Windows.UI;

namespace CodeBrix.Platform.UI.Composition.Composition; //Was previously: Uno.UI.Composition.Composition

/// <summary>
/// Captures the state of drop shadow for a visual.
/// </summary>
/// <remarks>
/// The platform builds what it draws the shadow with from these values (on Skia, the blurred shadow-only paint in
/// <c>CodeBrix.Platform.UI.Composition.Skia.VisualSkiaPlatform</c>).
/// </remarks>
internal record ShadowState(float Dx, float Dy, float SigmaX, float SigmaY, Color Color);
#endif
