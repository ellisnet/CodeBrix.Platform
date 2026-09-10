using System;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;

/// <summary>
/// The host builder behind
/// <see cref="CodeBrix.Platform.UI.Hosting.HostBuilder.UseLinuxTestTarget(ICodeBrixPlatformHostBuilder, TestTargetSession)"/>.
/// It carries nothing but the <see cref="TestTargetSession"/> the run belongs to:
/// the panel's shape is the session's, chosen at
/// <see cref="LinuxTestTarget.Setup(TestDisplayOrientation)"/>, so there is
/// nothing else to configure here.
/// </summary>
public sealed class TestTargetHostBuilder : IPlatformHostBuilder
{
	internal TestTargetHostBuilder(TestTargetSession session)
		=> Session = session;

	internal TestTargetSession Session { get; }

	bool IPlatformHostBuilder.IsSupported
		=> OperatingSystem.IsLinux();

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new TestTargetHost(appBuilder, this);
}
