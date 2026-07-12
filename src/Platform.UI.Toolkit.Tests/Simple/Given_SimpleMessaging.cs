using System;
using System.Threading.Tasks;
using CodeBrix.Platform.Simple;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

[TestClass]
public class Given_SimpleMessaging
{
	//Each test uses its own SimpleMessaging instance (not the shared Instance singleton)
	//so subscriptions can never leak between tests.
	private ISimpleMessaging _messaging;

	private sealed class Publisher;

	[TestInitialize]
	public void Init() => _messaging = new SimpleMessaging();

	[TestMethod]
	public void When_Send_With_Args_Invokes_Subscriber()
	{
		//Arrange
		var publisher = new Publisher();
		Publisher seenSender = null;
		string seenArgs = null;
		_messaging.Subscribe<Publisher, string>(this, "msg", (sender, args) =>
		{
			seenSender = sender;
			seenArgs = args;
		}, null);

		//Act
		_messaging.Send(publisher, "msg", "payload");

		//Assert
		seenSender.Should().Be(publisher);
		seenArgs.Should().Be("payload");
	}

	[TestMethod]
	public void When_SubscribeFrom_Receives_Sender_Only()
	{
		//Arrange
		var publisher = new Publisher();
		Publisher seen = null;
		_messaging.SubscribeFrom<Publisher>(this, "ping", sender => seen = sender, null);

		//Act
		_messaging.Send(publisher, "ping");

		//Assert
		seen.Should().Be(publisher);
	}

	[TestMethod]
	public void When_Generic_Subscribe_Receives_Args_From_Any_Sender()
	{
		//Arrange
		string seen = null;
		_messaging.Subscribe<string>(this, "msg", args => seen = args);

		//Act
		_messaging.Send(new Publisher(), "msg", "payload");

		//Assert
		seen.Should().Be("payload");
	}

	[TestMethod]
	public void When_Message_Name_Differs_Subscriber_Is_Not_Invoked()
	{
		//Arrange
		var invoked = false;
		_messaging.SubscribeFrom<Publisher>(this, "expected", _ => invoked = true, null);

		//Act
		_messaging.Send(new Publisher(), "other");

		//Assert
		Assert.IsFalse(invoked);
	}

	[TestMethod]
	public void When_Source_Filter_Is_Set_Other_Senders_Are_Ignored()
	{
		//Arrange
		var wanted = new Publisher();
		var unwanted = new Publisher();
		var invocations = 0;
		_messaging.SubscribeFrom<Publisher>(this, "ping", _ => invocations++, wanted);

		//Act
		_messaging.Send(unwanted, "ping");
		_messaging.Send(wanted, "ping");

		//Assert
		invocations.Should().Be(1);
	}

	[TestMethod]
	public void When_Unsubscribed_Subscriber_Is_Not_Invoked()
	{
		//Arrange
		var invoked = false;
		_messaging.SubscribeFrom<Publisher>(this, "ping", _ => invoked = true, null);
		_messaging.UnsubscribeFrom<Publisher>(this, "ping");

		//Act
		_messaging.Send(new Publisher(), "ping");

		//Assert
		Assert.IsFalse(invoked);
	}

	[TestMethod]
	public async Task When_Async_Subscriber_Is_Invoked()
	{
		//Arrange
		var completion = new TaskCompletionSource<string>();
		_messaging.Subscribe<Publisher, string>(this, "msg", (_, args) =>
		{
			completion.SetResult(args);
			return Task.CompletedTask;
		}, null);

		//Act
		_messaging.Send(new Publisher(), "msg", "async payload");

		//Assert
		(await completion.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be("async payload");
	}

	[TestMethod]
	public void When_Send_Has_No_Subscribers_Does_Not_Throw() =>
		_messaging.Send(new Publisher(), "nobody-listens");

	[TestMethod]
	public void When_Send_With_Null_Sender_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => _messaging.Send<Publisher>(null, "msg"));

	[TestMethod]
	public void When_Static_Instance_Is_Available() =>
		Assert.IsNotNull(SimpleMessaging.Instance);
}
