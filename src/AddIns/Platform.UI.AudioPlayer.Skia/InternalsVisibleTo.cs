using System.Runtime.CompilerServices;

// CodeBrix.Platform.UI.AudioPlayer.Core (the platform-neutral assembly this file is compiled into since the Core/Skia
// split): its Skia twin, which implements the add-in's platform contracts (Contracts/IAudioPlayerPlatform,
// Contracts/IAudioOutputPlatform) and holds MidiPlayer, which shares the source resolver and the failure event args,
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AudioPlayer.Skia")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AudioPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AudioPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AudioPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AudioPlayer.Tests")]

// The host-free Core suite (src/Platform.UI.Core.Tests) registers test doubles for the platform contracts.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core.Tests")]

// The host-free engine proof (WPE1 C10): drives Engine/AudioTransport and the source resolver with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]
