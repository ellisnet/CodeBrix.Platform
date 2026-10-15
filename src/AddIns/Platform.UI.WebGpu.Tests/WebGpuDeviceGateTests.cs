using System;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// What the view does with the head's owned-GPU-device gate: proceed without one, report its reason, and swallow the
/// failure a launch without a provider records (the PlayTest head's three cases).
/// </summary>
public class WebGpuDeviceGateTests
{
	private sealed class Gate : IOwnedGpuDeviceGate
	{
		internal string? Reason { get; init; }

		internal Exception? Failure { get; init; }

		internal object? State { get; init; }

		internal string? AskedAbout { get; private set; }

		public string? GetUnavailableReason(string api)
		{
			AskedAbout = api;
			if (Failure is not null)
			{
				throw Failure;
			}

			return Reason;
		}

		public object? GetProviderState(string api) => State;
	}

	[Fact]
	public void no_gate_means_the_view_creates_its_device()
	{
		//Act
		var decision = WebGpuDeviceGate.Evaluate(null);

		//Assert
		decision.MayCreate.Should().BeTrue();
		decision.Reason.Should().BeNull();
		decision.Failed.Should().BeFalse();
	}

	[Fact]
	public void the_gate_is_asked_about_webgpu()
	{
		//Arrange
		var gate = new Gate();

		//Act
		WebGpuDeviceGate.Evaluate(gate);

		//Assert
		gate.AskedAbout.Should().Be(OwnedGpuDeviceApis.WebGpu);
	}

	[Fact]
	public void a_reason_from_the_gate_means_no_device_with_that_reason()
	{
		//Arrange
		var gate = new Gate { Reason = "PlayTestOptions.WebGpu is Unavailable for this launch." };

		//Act
		var decision = WebGpuDeviceGate.Evaluate(gate);

		//Assert
		decision.MayCreate.Should().BeFalse();
		decision.Reason.Should().Be("PlayTestOptions.WebGpu is Unavailable for this launch.");
		decision.Failed.Should().BeFalse();
	}

	[Fact]
	public void a_gate_that_throws_is_an_initialization_failure_that_is_swallowed()
	{
		//Arrange
		var gate = new Gate { Failure = new InvalidOperationException("No WebGPU provider is registered.") };

		//Act
		var decision = WebGpuDeviceGate.Evaluate(gate);

		//Assert
		decision.MayCreate.Should().BeFalse();
		decision.Failed.Should().BeTrue();
		decision.Reason.Should().Be("No WebGPU provider is registered.");
	}

	[Fact]
	public void the_providers_state_is_accepted()
	{
		//Arrange
		var state = new object();
		var gate = new Gate { State = state };

		//Act
		var decision = WebGpuDeviceGate.Evaluate(gate);

		//Assert
		decision.MayCreate.Should().BeTrue();
		decision.ProviderState.Should().BeSameAs(state);
	}

	[Fact]
	public void a_xaml_root_with_no_registered_gate_proceeds()
	{
		//Act
		var decision = WebGpuDeviceGate.Resolve(new object());

		//Assert
		decision.MayCreate.Should().BeTrue();
	}
}
