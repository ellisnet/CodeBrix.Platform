using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Support;
using CodeBrix.Platform.UI.Core.UIReqs.Hosting;
using CodeBrix.Platform.UI.Core.UIReqs.Support;
using CodeBrix.Platform.UI.WebGpu;
using CodeBrix.WebGpu.Skia;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Reqnroll;
using SilverAssertions;
using ElementFactory = CodeBrix.Platform.UI.Core.UIReqs.Support.ElementFactory;
using UIColors = CodeBrix.Platform.UI.Core.UIReqs.Support.Colors;

namespace CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Steps;

/// <summary>
/// The WebGpu group's vocabulary: the WebGpuRenderView itself, how many frames it has rendered, which driver runs it,
/// a frame rendered from a worker thread, and the view leaving and re-entering the tree.
/// <para>
/// Everything else a WebGpu scenario says - showing a sized cell, putting the view in it, what its region looks like,
/// which events it raised - is the core harness's own vocabulary, reached through this project's reqnroll.json
/// binding assemblies. Nothing from the core project is duplicated here.
/// </para>
/// <para>
/// Frames arrive on the wall clock (the Continuous driver runs at its target rate), so every claim about frames or
/// pictures is a BOUNDED POLL over fresh state rather than a single look at a computed moment - and never a sleep,
/// except where the claim IS a quiet period ("renders no new frame for ... milliseconds").
/// </para>
/// </summary>
[Binding]
public sealed class WebGpuSteps
{
	/// <summary>The name a feature file builds the view with.</summary>
	public const string WebGpuRenderViewKind = "WebGpuRenderView";

	/// <summary>
	/// The prerequisite name every feature file declares with a <c>@needs-webgpu</c> tag: a WebGPU adapter on this
	/// machine. A machine without one reports skipped scenarios with the engine's reason instead of blank regions.
	/// </summary>
	public const string WebGpuPrerequisite = "webgpu";

	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

	private static readonly object Gate = new();

	private static readonly Dictionary<WebGpuRenderView, int> Frames = new();

	private static readonly Dictionary<string, Panel> Parents = new(StringComparer.Ordinal);

	private static bool _registered;

	private readonly ScenarioContext _scenarioContext;

	/// <summary>Reqnroll builds one of these per scenario and injects the contexts it asks for.</summary>
	/// <param name="scenarioContext">The current scenario.</param>
	public WebGpuSteps(ScenarioContext scenarioContext)
	{
		_scenarioContext = scenarioContext;
	}

	// ------------------------------------------------------------ registration

	/// <summary>
	/// Teaches the element factory this add-in's nouns, before the first scenario, and asks the machine for a WebGPU
	/// adapter once. Every property is registered as a TYPED setter, so the words mean something on this view only.
	/// </summary>
	[BeforeTestRun(Order = 10)]
	public static void Register_the_WebGpu_vocabulary()
	{
		lock (Gate)
		{
			if (_registered)
			{
				return;
			}

			_registered = true;

			try
			{
				RegisterTheNouns();
				using var probe = WebGpuSkiaDevice.Create();
				if (!probe.IsAvailable)
				{
					Prerequisite.Missing(WebGpuPrerequisite, "this machine has no WebGPU adapter: " + probe.Reason);
				}
			}
			catch (Exception failure) when (failure is TypeLoadException or FileNotFoundException
				or FileLoadException or MissingMemberException or DllNotFoundException)
			{
				Prerequisite.Missing(WebGpuPrerequisite,
					$"the WebGpu add-in could not be used ({failure.GetType().Name}: {failure.Message})");
			}
		}
	}

	/// <summary>Forgets the frame counts and parents of the scenario that just ran.</summary>
	[AfterScenario(Order = 0)]
	public static void Forget_the_views_of_the_scenario()
	{
		lock (Gate)
		{
			Frames.Clear();
			Parents.Clear();
		}
	}

	// ------------------------------------------------------------ frames

	/// <summary>Waits until a view has rendered at least a number of frames through its built-in driver.</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="count">How many frames.</param>
	/// <param name="milliseconds">The budget.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Given("{string} has rendered {int} frames within {int} milliseconds")]
	[When("{string} has rendered {int} frames within {int} milliseconds")]
	[Then("{string} has rendered {int} frames within {int} milliseconds")]
	public async Task Then_has_rendered_frames_within(string name, int count, int milliseconds)
	{
		var view = ViewOf(name);
		var reached = await Poll.UntilAsync(() => Task.FromResult(FramesOf(view) >= count),
			TimeSpan.FromMilliseconds(milliseconds), PollInterval).ConfigureAwait(false);

		reached.Should().BeTrue("\"{0}\" had {1} milliseconds to render {2} frames and rendered {3} (render path {4})",
			name, milliseconds, count, FramesOf(view), await ReadAsync(view, v => v.RenderPath).ConfigureAwait(false));
	}

	/// <summary>Remembers how many frames a view has rendered so far.</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	[Given("the frame count of {string} is remembered")]
	[When("the frame count of {string} is remembered")]
	public void Given_the_frame_count_of_is_remembered(string name) =>
		_scenarioContext[MarkKey(name)] = FramesOf(ViewOf(name));

	/// <summary>Asserts that a view renders nothing more for a quiet period (the claim IS the period).</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="milliseconds">The quiet period.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} renders no new frame for {int} milliseconds")]
	public async Task Then_renders_no_new_frame_for(string name, int milliseconds)
	{
		var view = ViewOf(name);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
		var before = FramesOf(view);
		await Task.Delay(milliseconds).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);

		FramesOf(view).Should().Be(before, "\"{0}\" must render nothing by itself for {1} milliseconds", name, milliseconds);
	}

	/// <summary>
	/// Asserts that a view renders EXACTLY a number of frames after the remembered count: it reaches the number within
	/// the budget, and a quiet period later it has not gone past it.
	/// </summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="count">How many more frames.</param>
	/// <param name="milliseconds">The budget.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} renders exactly {int} more frame(s) within {int} milliseconds")]
	public async Task Then_renders_exactly_more_frames_within(string name, int count, int milliseconds)
	{
		var view = ViewOf(name);
		var mark = _scenarioContext.TryGetValue(MarkKey(name), out var value) ? (int)value : 0;
		var reached = await Poll.UntilAsync(() => Task.FromResult(FramesOf(view) - mark >= count),
			TimeSpan.FromMilliseconds(milliseconds), PollInterval).ConfigureAwait(false);
		await Task.Delay(300).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);

		reached.Should().BeTrue("\"{0}\" had {1} milliseconds to render {2} more frames", name, milliseconds, count);
		(FramesOf(view) - mark).Should().Be(count, "\"{0}\" must render exactly {1} more frames", name, count);
	}

	// ------------------------------------------------------------ frames by hand
	// (The driver itself is set with the core's "the FrameDriver of ... is set to ..." through the typed setter
	// registered below.)

	/// <summary>Calls the view's Invalidate (from the UI thread, as an application would).</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <returns>A task that completes once the UI thread is idle again.</returns>
	[When("{string} is invalidated")]
	public async Task When_is_invalidated(string name)
	{
		var view = ViewOf(name);
		Given_the_frame_count_of_is_remembered(name);
		await TestTargetFixture.RunOnUIThreadAsync(view.Invalidate).ConfigureAwait(false);
	}

	/// <summary>
	/// Renders one frame by hand from a worker thread - the Manual driver's way - after setting the clear colour on
	/// the UI thread, so the frame's colour proves it reached the screen.
	/// </summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="color">The clear colour of that frame.</param>
	/// <returns>A task that completes when the frame has been ended.</returns>
	[When("{string} renders one frame cleared to {string} from a worker thread")]
	public async Task When_renders_one_frame_cleared_to_from_a_worker_thread(string name, string color)
	{
		var view = ViewOf(name);
		var clear = UIColors.Parse(GherkinValue.Unquote(color));
		await TestTargetFixture.RunOnUIThreadAsync(() => view.ClearColor = clear).ConfigureAwait(false);

		var uiThread = 0;
		await TestTargetFixture.RunOnUIThreadAsync(() => uiThread = Environment.CurrentManagedThreadId).ConfigureAwait(false);

		var (opened, workerThread) = await Task.Run(() =>
		{
			var frame = view.BeginFrame();
			if (frame is null)
			{
				return (false, Environment.CurrentManagedThreadId);
			}

			view.EndFrame();
			return (true, Environment.CurrentManagedThreadId);
		}).ConfigureAwait(false);

		opened.Should().BeTrue("\"{0}\" must open a frame from a worker thread under the Manual driver", name);
		workerThread.Should().NotBe(uiThread, "the frame must come from a thread that is not the UI thread");
	}

	// ------------------------------------------------------------ render path and device

	/// <summary>Asserts which render path the view reports.</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="path">The expected path.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("the RenderPath of {string} is {string}")]
	public async Task Then_the_RenderPath_of_is(string name, string path)
	{
		var expected = GherkinValue.ToEnum<WebGpuRenderPath>(path);
		var view = ViewOf(name);
		var reached = await Poll.UntilAsync(async () => await ReadAsync(view, v => v.RenderPath).ConfigureAwait(false) == expected,
			TimeSpan.FromSeconds(3), PollInterval).ConfigureAwait(false);

		reached.Should().BeTrue("the RenderPath of \"{0}\" must be {1}; it is {2}", name, expected,
			await ReadAsync(view, v => v.RenderPath).ConfigureAwait(false));
	}

	/// <summary>Asserts how many devices a view has been given (DeviceReady raised that many times).</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <param name="count">How many devices.</param>
	/// <param name="milliseconds">The budget.</param>
	/// <returns>A task that completes when the assertion has been made.</returns>
	[Then("{string} has had {int} devices within {int} milliseconds")]
	public async Task Then_has_had_devices_within(string name, int count, int milliseconds)
	{
		var reached = await Poll.UntilAsync(() => Task.FromResult(EventRecorder.Count(name, "DeviceReady") >= count),
			TimeSpan.FromMilliseconds(milliseconds), PollInterval).ConfigureAwait(false);

		reached.Should().BeTrue("\"{0}\" must have had {1} devices; the scenario recorded [{2}]",
			name, count, string.Join(", ", EventRecorder.Recorded));
		EventRecorder.Count(name, "DeviceReady").Should().Be(count);
	}

	// ------------------------------------------------------------ the visual tree

	/// <summary>Takes the view out of the panel that holds it (it is unloaded).</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <returns>A task that completes once the UI thread is idle again.</returns>
	[When("{string} is taken out of its layout")]
	public async Task When_is_taken_out_of_its_layout(string name)
	{
		var view = ViewOf(name);
		await TestTargetFixture.RunOnUIThreadAsync(() =>
		{
			var parent = (Panel)VisualTreeHelper.GetParent(view);
			lock (Gate)
			{
				Parents[name] = parent;
			}

			parent.Children.Remove(view);
		}).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	/// <summary>Puts the view back into the panel it was taken out of (it is loaded again).</summary>
	/// <param name="name">The Gherkin name of the view.</param>
	/// <returns>A task that completes once the UI thread is idle again.</returns>
	[When("{string} is put back into its layout")]
	public async Task When_is_put_back_into_its_layout(string name)
	{
		var view = ViewOf(name);
		Panel parent;
		lock (Gate)
		{
			parent = Parents[name];
		}

		await TestTargetFixture.RunOnUIThreadAsync(() => parent.Children.Add(view)).ConfigureAwait(false);
		await TestTargetFixture.WaitForIdleAsync().ConfigureAwait(false);
	}

	// --------------------------------------------------------------- inner

	private static void RegisterTheNouns()
	{
		ElementFactory.RegisterKind(WebGpuRenderViewKind, BuildView);

		ElementFactory.RegisterProperty<WebGpuRenderView>("FrameDriver",
			(view, value) => view.FrameDriver = GherkinValue.ToEnum<WebGpuFrameDriver>(value));
		ElementFactory.RegisterProperty<WebGpuRenderView>("ClearColor",
			(view, value) => view.ClearColor = UIColors.Parse(GherkinValue.Unquote(value)));
		ElementFactory.RegisterProperty<WebGpuRenderView>("TargetFrameRate",
			(view, value) => view.TargetFrameRate = GherkinValue.ToDouble(value));
		ElementFactory.RegisterProperty<WebGpuRenderView>("Stretch",
			(view, value) => view.Stretch = GherkinValue.ToEnum<Stretch>(value));
		ElementFactory.RegisterProperty<WebGpuRenderView>("Renderer",
			(view, value) => view.Renderer = GherkinValue.Unquote(value) switch
			{
				"Triangle" => new TriangleRenderer(drawOverlay: false),
				"TriangleWithOverlay" => new TriangleRenderer(drawOverlay: true),
				var other => throw new NotSupportedException(
					string.Create(CultureInfo.InvariantCulture, $"There is no renderer called \"{other}\"; use Triangle or TriangleWithOverlay.")),
			});
	}

	// Every view counts its frames (the Render event) and records its device and path events.
	private static FrameworkElement BuildView()
	{
		var view = new WebGpuRenderView();
		lock (Gate)
		{
			Frames[view] = 0;
		}

		view.Render += (sender, _) =>
		{
			lock (Gate)
			{
				Frames[(WebGpuRenderView)sender!] = Frames.TryGetValue((WebGpuRenderView)sender!, out var count) ? count + 1 : 1;
			}
		};
		view.DeviceReady += (sender, _) => RecordEvent(sender, "DeviceReady");
		view.DeviceLost += (sender, _) => RecordEvent(sender, "DeviceLost");
		view.RenderPathChanged += (sender, _) => RecordEvent(sender, "RenderPathChanged");
		return view;
	}

	private static void RecordEvent(object? sender, string eventName)
	{
		if (sender is FrameworkElement element && !string.IsNullOrEmpty(element.Name))
		{
			EventRecorder.Record(element.Name, eventName);
		}
	}

	private static int FramesOf(WebGpuRenderView view)
	{
		lock (Gate)
		{
			return Frames.TryGetValue(view, out var count) ? count : 0;
		}
	}

	private static string MarkKey(string name) => "uireqs.webgpu.frames." + name;

	private static WebGpuRenderView ViewOf(string name) =>
		ElementRegistry.Resolve(name) as WebGpuRenderView
		?? throw new NotSupportedException($"\"{name}\" is not a WebGpuRenderView, so it renders no frames.");

	private static async Task<TValue> ReadAsync<TValue>(WebGpuRenderView view, Func<WebGpuRenderView, TValue> read)
	{
		var value = default(TValue)!;
		await TestTargetFixture.RunOnUIThreadAsync(() => value = read(view)).ConfigureAwait(false);
		return value;
	}
}
