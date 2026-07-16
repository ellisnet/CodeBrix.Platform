#nullable enable

//CodeBrix warning-cleanup 2026-07-10: synchronous call retained deliberately (hosting/event-loop, disposal, or build-time tooling where sync execution is intended); CA1849 suppressed rather than changing async timing.
#pragma warning disable CA1849
using System;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public abstract class CodeBrixPlatformHost
{
	internal Action? AfterInitAction { get; set; }

	private async Task RunCore()
	{
		Initialize();
		await InitializeAsync();
		AfterInitAction?.Invoke();
		await RunLoop();
	}

	public void Run()
	{
		var task = RunCore();
		if (task.IsFaulted)
		{
			//Surface the real startup exception instead of the misleading RunAsync message below.
			task.GetAwaiter().GetResult();
		}
		if (!task.IsCompleted)
		{
			throw new InvalidOperationException($"Running host {this} requires calling 'await host.RunAsync()' instead of 'host.Run()'.");
		}
	}

	public async Task RunAsync() => await RunCore();

	protected abstract void Initialize();

	protected virtual Task InitializeAsync() => Task.CompletedTask;

	protected abstract Task RunLoop();
}
