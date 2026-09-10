using System;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Core.UIReqs.Canvas;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using Reqnroll;

namespace CodeBrix.Platform.UI.Core.UIReqs.Hooks;

/// <summary>
/// The once-per-process hooks. One virtual application serves every scenario of an assembly:
/// the window wrapper, the pointer source and the dispatcher overrides are process-wide, so
/// the application is launched here and only its content changes between scenarios.
/// </summary>
[Binding]
public sealed class TestRunHooks
{
	/// <summary>
	/// Reads the panel this assembly declared, launches the virtual application and settles its
	/// first frame, so no scenario is ever handed the empty frame the host publishes before the
	/// application has laid itself out. The opt-in review archive is set up afterwards, because
	/// it files its frames by panel orientation and the panel is only known once the
	/// application is up - which also keeps the launch handshake's own frame out of it.
	/// </summary>
	/// <returns>A task that completes when the application is showing.</returns>
	[BeforeTestRun(Order = 0)]
	public static async Task Launch_the_virtual_application()
	{
		await TestTargetFixture.LaunchAsync().ConfigureAwait(false);
		FrameReview.Initialize();
		AnnounceThePanel();
	}

	private static void AnnounceThePanel() =>
		Console.Out.WriteLine($"UIReqs: {TestTargetFixture.DescribePanel()}; {FrameArchive.Describe()}.");

	/// <summary>Shuts the virtual application down after the last scenario.</summary>
	/// <returns>A task that completes when the host thread has ended.</returns>
	[AfterTestRun(Order = 0)]
	public static async Task Shut_the_virtual_application_down() =>
		await TestTargetFixture.ShutdownAsync().ConfigureAwait(false);
}
