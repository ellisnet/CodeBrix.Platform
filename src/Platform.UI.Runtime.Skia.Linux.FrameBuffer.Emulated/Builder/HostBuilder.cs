using System;
using CodeBrix.Platform.UI.Runtime.Skia;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public static class HostBuilder
{
	public static ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer(this ICodeBrixPlatformHostBuilder builder)
	{
		builder.AddHostBuilder(() => new FramebufferHostBuilder());
		return builder;
	}

	public static ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer(this ICodeBrixPlatformHostBuilder builder, Action<FramebufferHostBuilder> action)
	{
		builder.AddHostBuilder(() =>
		{
			var fbBuilder = new FramebufferHostBuilder();
			if (((IPlatformHostBuilder)fbBuilder).IsSupported)
			{
				action.Invoke(fbBuilder);
			}
			return fbBuilder;
		});

		return builder;
	}

	/// <summary>
	/// Runs the application on the in-process TEST TARGET rather than under the
	/// CodeBrix.Develop frame-buffer emulator: a fixed panel that renders and takes
	/// input inside this process, with no shared memory, no socket and nothing that
	/// can end the process.
	/// </summary>
	/// <param name="builder">The host builder.</param>
	/// <param name="session">The session the run belongs to, from <see cref="LinuxTestTarget.Setup(TestDisplayOrientation)"/>.</param>
	/// <returns>The same host builder, for chaining.</returns>
	public static ICodeBrixPlatformHostBuilder UseLinuxTestTarget(this ICodeBrixPlatformHostBuilder builder, TestTargetSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		builder.AddHostBuilder(() => new TestTargetHostBuilder(session));
		return builder;
	}
}
