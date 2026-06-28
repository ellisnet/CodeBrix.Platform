// CodeBrix.Platform fork: stubbed.
//
// Upstream uno's HotReloadInfoTask_v0 generated the [HotReloadInfoAttribute]
// + hot-reload manifest files that the Uno.UI.RemoteControl dev-time client
// uses to find the running app. The Uno.UI.RemoteControl folder (and the
// HotReloadInfoHelper class it contained) was dropped from this fork along
// with the rest of the Hot Reload feature.
//
// The class is kept as a no-op so:
//   * Uno.UI.Tasks.v0.dll still exports Uno.UI.Tasks.HotReloadInfo.HotReloadInfoTask_v0,
//     so the <UsingTask TaskName="...HotReloadInfoTask_v0" /> declarations in
//     Platform.UI.Tasks.targets still resolve cleanly at consumer parse time.
//   * The _GenerateHotReloadInfo target in Platform.UI.Tasks.targets is gated by
//     Condition="'$(CodeBrixGenerateHotReloadInfo)' == 'True'" and consumer apps
//     of CodeBrix.Platform should not set that property. If it does run, this
//     stub returns success without writing any files, leaving the downstream
//     <Compile Include="@(_HotReloadInfoTaskGeneratedFiles)" /> to include
//     an empty item set.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CodeBrix.Platform.UI.Tasks.HotReloadInfo; //Was previously: Uno.UI.Tasks.HotReloadInfo

public class HotReloadInfoTask_v0 : Microsoft.Build.Utilities.Task
{
	[Required]
	public string IntermediateOutputPath { get; set; }

	[Output]
	public ITaskItem[] GeneratedFiles { get; set; }

	public override bool Execute()
	{
		Log.LogMessage(MessageImportance.Low, "HotReloadInfoTask_v0 is a no-op in this CodeBrix.Platform fork; Hot Reload was dropped.");
		GeneratedFiles = Array.Empty<ITaskItem>();
		return true;
	}
}
