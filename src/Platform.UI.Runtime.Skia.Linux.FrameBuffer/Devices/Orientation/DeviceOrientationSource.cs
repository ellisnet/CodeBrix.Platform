// Real-head only (NOT Shared): the Emulated head's rotation is driven by the
// Emulator View over its transport, so it has no use for this listener.

#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.LinuxDBus;
using CodeBrix.Platform.UI.Runtime.Skia;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer;

/// <summary>
/// The head's runtime orientation sources, both riding the D-Bus SYSTEM bus
/// (present on any Debian device — systemd-logind depends on it) and both
/// funneling into the window wrapper's SetDeviceOrientation, so
/// <see cref="FramebufferHostBuilder.AutoRotationEnabled(bool)"/> gates them
/// identically:
/// <list type="bullet">
/// <item>SENSOR — iio-sensor-proxy's accelerometer (net.hadess.SensorProxy):
/// claim it, read the initial orientation, and follow PropertiesChanged. The
/// production source, selected by the application's
/// <see cref="FramebufferHostBuilder.UseOrientationSensor"/>.</item>
/// <item>DEVELOP — a broadcast signal CodeBrix.Develop emits with dbus-send
/// while testing. Broadcast signals need no bus policy and no name ownership,
/// so nothing is provisioned; the trade is that any local user could emit one,
/// which is acceptable on a single-user development device and never enabled
/// in production.</item>
/// </list>
/// The launcher chooses via CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE:
/// "develop" (CodeBrix.Develop always sets this, so a physically-turned test
/// device NEVER fights the IDE's instructions), "sensor" to force the sensor,
/// "none" to disable both, unset to honor the application's declaration.
/// </summary>
internal static class DeviceOrientationSource
{
	private const string EnvironmentCodeBrixOrientationSource = "CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE";

	private const string DevelopSignalPath = "/com/codebrix/platform/FrameBuffer";
	private const string DevelopSignalInterface = "com.codebrix.platform.FrameBuffer";
	private const string DevelopSignalMember = "DeviceOrientation";

	private const string SensorProxyService = "net.hadess.SensorProxy";
	private const string SensorProxyPath = "/net/hadess/SensorProxy";
	private const string SensorProxyInterface = "net.hadess.SensorProxy";

	private enum Source
	{
		None,
		Develop,
		Sensor,
	}

	// The bus connection lives for the application's lifetime; holding it here
	// keeps it (and its match subscriptions) from being collected.
	private static Connection? _connection;

	/// <summary>
	/// Starts the configured orientation source, if any. <paramref name="apply"/>
	/// receives each instructed device orientation on a background thread; the
	/// host marshals it to the UI loop and into SetDeviceOrientation.
	/// </summary>
	internal static void Start(FramebufferHostBuilder hostBuilder, Action<DisplayOrientations> apply)
	{
		var value = Environment.GetEnvironmentVariable(EnvironmentCodeBrixOrientationSource)?.Trim().ToLowerInvariant();
		var source = value switch
		{
			"develop" => Source.Develop,
			"sensor" => Source.Sensor,
			null or "" => hostBuilder.OrientationSensorEnabled ? Source.Sensor : Source.None,
			// "none", "off" and anything unrecognized: no orientation source.
			_ => Source.None,
		};
		if (source == Source.None)
		{
			return;
		}
		_ = Task.Run(() => ListenAsync(source, apply));
	}

	private static async Task ListenAsync(Source source, Action<DisplayOrientations> apply)
	{
		try
		{
			var address = Address.System;
			if (string.IsNullOrEmpty(address))
			{
				LogError("No D-Bus system bus address could be determined; no orientation source is active.");
				return;
			}
			var connection = new Connection(address);
			await connection.ConnectAsync();
			_connection = connection;

			if (source == Source.Develop)
			{
				await ListenForDevelopAsync(connection, apply);
			}
			else
			{
				await ListenForSensorAsync(connection, apply);
			}
		}
		catch (Exception e)
		{
			LogError($"The {source} orientation source could not be started: {e.Message}");
		}
	}

	private static async Task ListenForDevelopAsync(Connection connection, Action<DisplayOrientations> apply)
	{
		var rule = new MatchRule
		{
			Type = MessageType.Signal,
			Path = DevelopSignalPath,
			Interface = DevelopSignalInterface,
			Member = DevelopSignalMember,
		};
		_ = await connection.AddMatchAsync(rule,
			(Message message, object? _) => message.GetBodyReader().ReadString(),
			(Exception? exception, string value, object? _, object? _) =>
			{
				if (exception is not null)
				{
					LogError($"Reading a Develop orientation instruction failed: {exception.Message}");
					return;
				}
				if (ParseInstruction(value) is { } orientation)
				{
					apply(orientation);
				}
				else
				{
					LogError($"Ignoring unknown orientation instruction '{value}'.");
				}
			},
			null, null, emitOnCapturedContext: false, ObserverFlags.None);
	}

	private static async Task ListenForSensorAsync(Connection connection, Action<DisplayOrientations> apply)
	{
		var hasAccelerometer = await GetSensorPropertyAsync(connection, "HasAccelerometer");
		if (!hasAccelerometer.GetBool())
		{
			LogError("iio-sensor-proxy reports no accelerometer; no orientation source is active.");
			return;
		}

		// The claim is per-connection and auto-released when the process dies.
		// (A restart of the daemon drops the claim; re-claiming on daemon
		// restart is production hardening for later.)
		await connection.CallMethodAsync(CreateSensorCallMessage(connection, "ClaimAccelerometer"));

		var rule = new MatchRule
		{
			Type = MessageType.Signal,
			Sender = SensorProxyService,
			Path = SensorProxyPath,
			Interface = "org.freedesktop.DBus.Properties",
			Member = "PropertiesChanged",
			Arg0 = SensorProxyInterface,
		};
		_ = await connection.AddMatchAsync(rule,
			(Message message, object? _) =>
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // the interface name, already matched by Arg0
				return reader.ReadDictionaryOfStringToVariantValue();
			},
			(Exception? exception, System.Collections.Generic.Dictionary<string, VariantValue> changed, object? _, object? _) =>
			{
				if (exception is not null)
				{
					LogError($"Reading a sensor orientation change failed: {exception.Message}");
					return;
				}
				if (changed.TryGetValue("AccelerometerOrientation", out var value)
					&& MapSensorOrientation(value.GetString()) is { } orientation)
				{
					apply(orientation);
				}
			},
			null, null, emitOnCapturedContext: false, ObserverFlags.None);

		var initial = await GetSensorPropertyAsync(connection, "AccelerometerOrientation");
		if (MapSensorOrientation(initial.GetString()) is { } first)
		{
			apply(first);
		}
	}

	private static Task<VariantValue> GetSensorPropertyAsync(Connection connection, string property)
	{
		return connection.CallMethodAsync(CreateMessage(),
			(Message message, object? _) => message.GetBodyReader().ReadVariantValue(), null);

		MessageBuffer CreateMessage()
		{
			var writer = connection.GetMessageWriter();
			writer.WriteMethodCallHeader(
				destination: SensorProxyService,
				path: SensorProxyPath,
				@interface: "org.freedesktop.DBus.Properties",
				signature: "ss",
				member: "Get");
			writer.WriteString(SensorProxyInterface);
			writer.WriteString(property);
			return writer.CreateMessage();
		}
	}

	private static MessageBuffer CreateSensorCallMessage(Connection connection, string member)
	{
		var writer = connection.GetMessageWriter();
		writer.WriteMethodCallHeader(
			destination: SensorProxyService,
			path: SensorProxyPath,
			@interface: SensorProxyInterface,
			member: member);
		return writer.CreateMessage();
	}

	private static DisplayOrientations? ParseInstruction(string value)
		=> value?.Trim().ToLowerInvariant() switch
		{
			"landscape" => DisplayOrientations.Landscape,
			"portrait" => DisplayOrientations.Portrait,
			"landscapeflipped" => DisplayOrientations.LandscapeFlipped,
			"portraitflipped" => DisplayOrientations.PortraitFlipped,
			_ => null,
		};

	// iio-sensor-proxy reports which way the device is HELD relative to its
	// panel's natural scanout ("normal", "bottom-up", "left-up", "right-up"),
	// so the device orientation is that many quarter-turns from the panel's
	// native orientation. Quarter-turn positions follow the window wrapper's
	// convention: Landscape 0, Portrait 1, LandscapeFlipped 2, PortraitFlipped 3.
	// The left-up/right-up handedness below is the freedesktop convention ON
	// PAPER — verify on the first live sensor test and swap those two mappings
	// if the application rotates opposite to the physical turn.
	private static DisplayOrientations? MapSensorOrientation(string sensorOrientation)
	{
		var nativeTurns = FrameBufferWindowWrapper.Instance.NativeOrientation switch
		{
			DisplayOrientations.Portrait => 1,
			DisplayOrientations.LandscapeFlipped => 2,
			DisplayOrientations.PortraitFlipped => 3,
			_ => 0,
		};
		int? turnsFromNative = sensorOrientation?.Trim().ToLowerInvariant() switch
		{
			"normal" => 0,
			"right-up" => 1,
			"bottom-up" => 2,
			"left-up" => 3,
			// "undefined" (sensor cannot tell, e.g. device flat on a table)
			// and anything future-unknown: leave the orientation as it is.
			_ => null,
		};
		if (turnsFromNative is null)
		{
			return null;
		}
		return ((nativeTurns + turnsFromNative.Value) & 3) switch
		{
			1 => DisplayOrientations.Portrait,
			2 => DisplayOrientations.LandscapeFlipped,
			3 => DisplayOrientations.PortraitFlipped,
			_ => DisplayOrientations.Landscape,
		};
	}

	private static void LogError(string message)
	{
		if (typeof(DeviceOrientationSource).Log().IsEnabled(LogLevel.Error))
		{
			typeof(DeviceOrientationSource).Log().Error(message);
		}
	}
}
