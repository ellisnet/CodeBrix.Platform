#nullable enable

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeBrix.Platform.UI.WebView.Skia.Linux;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebView.Tests;

public class WebViewScriptsTests
{
	private const string Prefix = "(function(){var v=window.chrome&&window.chrome.webview;if(!v||typeof v.__codebrixDispatchMessage!=='function'){return -1;}return v.__codebrixDispatchMessage(";
	private const string Suffix = ");})()";

	[Theory]
	[InlineData("hello")]
	[InlineData("he said \"hi\"")]
	[InlineData("line one\nline two\r\n")]
	[InlineData("back\\slash and 'single'")]
	[InlineData("</script><script>alert(1)</script>")]
	[InlineData("separators \u2028 and \u2029")]
	[InlineData("non-ASCII: Nöldeke, Müller, \U0001F600")]
	[InlineData("")]
	public void a_string_message_reaches_the_page_only_as_a_json_string_literal_of_exactly_that_text(string message)
	{
		//Act
		var script = WebViewScripts.StringMessageDelivery(message);

		//Assert
		script.Should().StartWith(Prefix);
		script.Should().EndWith(Suffix);
		var literal = script[Prefix.Length..^Suffix.Length];
		JsonSerializer.Deserialize<string>(literal).Should().Be(message);
		AssertSafeLiteral(literal);
	}

	[Fact]
	public void a_null_string_message_is_sent_as_an_empty_string()
	{
		//Act
		var script = WebViewScripts.StringMessageDelivery(null);

		//Assert
		script.Should().Be(Prefix + "\"\"" + Suffix);
	}

	[Theory]
	[InlineData("{\"n\":1,\"s\":\"a\\\"b\"}")]
	[InlineData("[1,\"two\",true,null]")]
	[InlineData("\"just a string\"")]
	[InlineData("42")]
	[InlineData("{\"html\":\"</script>\",\"sep\":\"\\u2028\"}")]
	public void a_json_message_reaches_the_page_as_a_string_literal_the_page_parses(string json)
	{
		//Act
		var script = WebViewScripts.JsonMessageDelivery(json);

		//Assert
		script.Should().StartWith(Prefix + "JSON.parse(");
		script.Should().EndWith(")" + Suffix);
		var literal = script[(Prefix.Length + "JSON.parse(".Length)..^(")".Length + Suffix.Length)];
		JsonSerializer.Deserialize<string>(literal).Should().Be(json, "the page parses exactly the JSON text the app posted");
		AssertSafeLiteral(literal);
	}

	[Theory]
	[InlineData("{not json")]
	[InlineData("")]
	[InlineData("alert(1)")]
	public void a_json_message_that_is_not_json_is_refused(string json)
	{
		//Act
		var refuse = () => WebViewScripts.JsonMessageDelivery(json);

		//Assert
		refuse.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void a_null_json_message_is_refused()
	{
		//Act
		var refuse = () => WebViewScripts.JsonMessageDelivery(null);

		//Assert
		refuse.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void the_listener_script_offers_the_webview2_message_api_and_keeps_its_listeners_in_the_page()
	{
		//Act
		var script = WebViewScripts.MessageListenerScript;

		//Assert
		script.Should().Contain("v.addEventListener=function(t,f){if(t==='message'");
		script.Should().Contain("v.removeEventListener=function(t,f){if(t==='message')");
		script.Should().Contain("v.onmessage=null");
		script.Should().Contain("Object.defineProperty(v,'__codebrixDispatchMessage'");
		script.Should().Contain("data:d");
		script.Should().Contain("var ls=[]");
		script.Should().StartWith("(function(){");
		script.Should().EndWith("})();");
	}

	[Fact]
	public void the_no_channel_result_is_what_a_delivery_answers_when_the_page_has_no_channel()
	{
		//Act
		var script = WebViewScripts.StringMessageDelivery("x");

		//Assert
		script.Should().Contain("{return " + WebViewScripts.NoChannelResult + ";}");
		WebViewScripts.NoChannelResult.Should().Be("-1");
	}

	// The literal must not be able to end the string, end a <script> element, or break a line in a script.
	private static void AssertSafeLiteral(string literal)
	{
		literal.Should().StartWith("\"");
		literal.Should().EndWith("\"");
		var body = literal[1..^1];
		Regex.IsMatch(body, "(?<!\\\\)(\\\\\\\\)*\"").Should().BeFalse("no unescaped quote may end the literal early");
		body.Should().NotContain("\n");
		body.Should().NotContain("\r");
		body.Should().NotContain("\u2028");
		body.Should().NotContain("\u2029");
		body.Should().NotContain("<");
		body.Should().NotContain(">");
	}
}
