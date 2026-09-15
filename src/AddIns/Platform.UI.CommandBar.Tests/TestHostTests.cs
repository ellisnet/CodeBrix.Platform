using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Foundation;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The host-free bootstrap itself, which a consuming application is invited to copy.
/// </summary>
/// <remarks>
/// The readiness flag used to be set BEFORE the registration rather than after, so a second thread
/// returned from <c>EnsureReady</c> while the first was still half way through registering the
/// default styles - and a control built in that window failed with "ResourceDictionary was
/// registered as style provider for ToolBarOverflowButton but doesn't contain matching style". The
/// two tests here state the order, and that calling the bootstrap concurrently is safe.
/// <para>
/// None of this makes the framework's object model thread-safe: XAML-touching test classes still
/// have to be serialised. The add-in's AGENT-README says how.
/// </para>
/// </remarks>
public class TestHostTests
{
	[Fact]
	public void EnsureReady_has_finished_registering_the_default_styles_when_it_returns()
	{
		//Arrange
		//Act
		TestHost.EnsureReady();
		var button = new ToolButton { Text = "Open" };
		button.Measure(new Size(200, 100));

		//Assert
		//The invariant the old order broke: when the call returns, the styles are there to be
		//found, so anything built after it has a template.
		button.Template.Should().NotBeNull();
	}

	[Fact]
	public async Task EnsureReady_can_be_called_from_several_threads_at_once()
	{
		//Arrange
		var failures = new ConcurrentBag<Exception>();

		//Act
		//Only the bootstrap runs concurrently here - not the building of any XAML object, which is
		//the thing that is NOT thread-safe and that a suite serialises instead.
		var callers = new Task[8];
		for (var i = 0; i < callers.Length; i++)
		{
			callers[i] = Task.Run(() =>
			{
				try
				{
					TestHost.EnsureReady();
				}
				catch (Exception ex)
				{
					failures.Add(ex);
				}
			}, TestContext.Current.CancellationToken);
		}

		await Task.WhenAll(callers);

		//Assert
		failures.Should().BeEmpty();
	}
}
