#nullable enable

// Ported from the CodeBrix.Platform (Uno-based) Lottie package:
// src/AddIns/Platform.UI.Lottie/LottieVisualSource.cs

using Microsoft.UI.Xaml.Data;

namespace CodeBrix.Platform.WinUI.Lottie;

/// <summary>
/// A Lottie animated visual source for use with <see cref="AnimatedVisualPlayer"/>.
/// The animation JSON is fed to the player unmodified.
/// </summary>
[Bindable]
public partial class LottieVisualSource : LottieVisualSourceBase
{
    /// <inheritdoc />
    protected override bool IsPayloadNeedsToBeUpdated => false;
}
