// Ported from the CodeBrix.Platform (Uno-based) Lottie package:
// src/AddIns/Platform.UI.Lottie/LottieVisualOptions.cs
// Declared for API parity; not implemented there or here.

namespace CodeBrix.Platform.WinUI.Lottie;

/// <summary>
/// Options for how a Lottie is translated. Declared for API parity with the
/// CodeBrix.Platform Lottie package, where it is also not implemented.
/// </summary>
public enum LottieVisualOptions
{
    /// <summary>No options set.</summary>
    None = 0,

    /// <summary>
    /// Optimizes the translation of the Lottie so as to reduce resource
    /// usage during rendering. Note that this may slow down loading.
    /// (Not implemented.)
    /// </summary>
    Optimize = 1,

    /// <summary>
    /// Sets the AnimatedVisualPlayer.Diagnostics property with information
    /// about the Lottie. (Not implemented.)
    /// </summary>
    IncludeDiagnostics = 2,

    /// <summary>Enables all options. (Not implemented.)</summary>
    All = IncludeDiagnostics | Optimize,
}
