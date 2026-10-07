using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.SourceGenerators.XamlGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

/// <summary>
/// The registrations App.xaml emits for every [assembly: ApiExtension(...)] an application references. Each one is
/// guarded by ApiExtensibility.IsRegistered, so an extension registered earlier (for example a test double installed
/// before the application object is created, or a second application object in the same process) wins and the
/// generated registration does not throw on the duplicate.
/// </summary>
[TestClass]
public class Given_ApiExtensionRegistration
{
	private static string Emit(params object?[] arguments)
	{
		var builder = new IndentedStringBuilder();
		XamlFileGenerator.WriteApiExtensionRegistration(builder, arguments);
		return builder.ToString().Replace("\r\n", "\n");
	}

	[TestMethod]
	public void When_Two_Arguments_Registration_Is_Guarded()
	{
		var code = Emit("My.IExtension", "My.Extension");

		code.Should().Be(
			"if (!global::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.IsRegistered<global::My.IExtension>())\n" +
			"{\n" +
			"\tglobal::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.Register(typeof(global::My.IExtension), o => new global::My.Extension(o));\n" +
			"}\n");
	}

	[TestMethod]
	public void When_Owner_Argument_Registration_Is_Guarded()
	{
		var code = Emit("My.IExtension", "My.Extension", "My.Owner");

		code.Should().Be(
			"if (!global::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.IsRegistered<global::My.IExtension>())\n" +
			"{\n" +
			"\tglobal::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.Register<global::My.Owner>(typeof(global::My.IExtension), o => new global::My.Extension(o));\n" +
			"}\n");
	}

	[TestMethod]
	public void When_Operating_System_Argument_Registration_Is_Guarded_Inside_The_Condition()
	{
		var code = Emit("My.IExtension", "My.Extension", "My.Owner", "linux");

		code.Should().Be(
			"if (OperatingSystem.IsOSPlatform(\"linux\"))\n" +
			"{\n" +
			"\tif (!global::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.IsRegistered<global::My.IExtension>())\n" +
			"\t{\n" +
			"\t\tglobal::CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.Register<global::My.Owner>(typeof(global::My.IExtension), o => new global::My.Extension(o));\n" +
			"\t}\n" +
			"}\n");
	}

	[TestMethod]
	public void When_Argument_Count_Is_Wrong_Throws()
	{
		Assert.ThrowsExactly<InvalidOperationException>(() => Emit("My.IExtension"));
	}
}
