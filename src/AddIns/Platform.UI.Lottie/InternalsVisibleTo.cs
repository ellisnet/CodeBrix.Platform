using System.Runtime.CompilerServices;

// The Skia twin (CodeBrix.Platform.UI.Lottie, the canvas supply) implements this assembly's contracts.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie.Tests")]

// The host-free engine proof (WPE1 C9): drives Engine/LottiePlayer and Engine/LottieColorTheme with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// Reserved for the add-in's Android and Mobile platform assemblies and their tests (decision P4).
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Lottie.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Lottie")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Lottie.Tests")]
