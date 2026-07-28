#nullable enable

using System;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;

/// <summary>
/// Confines the emulated application to the fonts it actually ships, by reading
/// <see cref="FrameBufferEmulatorProtocol.FontIsolationVariable"/> from the
/// launch contract and turning on
/// <see cref="FeatureConfiguration.Font.RestrictToEmbeddedFonts"/>.
/// <para>
/// The emulator runs on a full desktop whose fonts a real frame-buffer device
/// would not have. Without this, the application's text quietly comes out right
/// for scripts it cannot actually display — the host's fonts fill the gaps —
/// which is the wrong answer to the question the emulator exists to ask. With
/// it on, a character none of the application's own fonts covers renders as
/// that font's missing-glyph, exactly as it would on the device.
/// </para>
/// <para>
/// Applied ONCE, before anything is built, because resolved fonts are cached:
/// text measured before the switch is set would keep the fonts it was given.
/// The environment is fixed at process launch, so this cannot change while the
/// application runs — a change in the IDE takes effect the next time the
/// emulator opens.
/// </para>
/// <para>
/// Only the framework's own symbols font stays exempt, which is not a hole:
/// CodeBrix.Platform depends on that package, so a real device carries it too.
/// </para>
/// </summary>
internal static class EmulatedFontIsolation
{
	/// <summary>
	/// Whether the emulated device confines the application to its own fonts.
	/// Absent means an IDE from before the setting existed — one that offers no
	/// way to turn isolation off — so it means off, which is how the emulator
	/// behaved before this existed. An unrecognized value is treated the same.
	/// </summary>
	internal static bool Read()
	{
		var value = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.FontIsolationVariable);
		return string.Equals(value?.Trim(), FrameBufferEmulatorProtocol.FontIsolationOn, StringComparison.Ordinal);
	}

	/// <summary>
	/// Applies <see cref="Read"/>, and returns whether isolation was turned on
	/// so the caller can log what the device is running as.
	/// </summary>
	internal static bool Apply()
	{
		var isolated = Read();
		if (isolated)
		{
			FeatureConfiguration.Font.RestrictToEmbeddedFonts = true;
		}
		return isolated;
	}
}
