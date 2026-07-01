using NWayland.Generator;

namespace GeneratorRunner;

internal class Program
{
	// Every protocol XML pinned under protocols/ that we generate bindings from.
	// To add a protocol: copy its MIT XML from upstream into protocols/, list it here,
	// re-run, and commit the diff (see README.md; PORTING-NOTES.txt records upstream SHAs).
	private static readonly string[] ProtocolXmlFiles =
	{
		"protocols/wayland/wayland.xml",
		"protocols/wayland-protocols/xdg-shell.xml",
		"protocols/wayland-protocols/xdg-decoration-unstable-v1.xml",
		"protocols/wayland-protocols/viewporter.xml",
		"protocols/wayland-protocols/fractional-scale-v1.xml",
		"protocols/wayland-protocols/cursor-shape-v1.xml",
		"protocols/wayland-protocols/tablet-v2.xml", // dependency of cursor-shape-v1
		"protocols/wayland-protocols/text-input-unstable-v3.xml",
		"protocols/wayland-protocols/primary-selection-unstable-v1.xml",
	};

	// Namespace the generated protocol classes live in; each protocol gets a
	// Pascalized sub-namespace (e.g. .Wayland, .XdgShell) appended by the generator.
	private const string ProtocolsNamespace = "CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols";

	private const string OutputFolder = "src/Platform.UI.Runtime.Skia.Wayland/Wayland_Bindings";

	private static int Main()
	{
		// Locate the repo root from wherever we were launched (the tool folder sits at
		// <repo>/tools/WaylandBindingsGenerator).
		var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "tools", "WaylandBindingsGenerator", "PORTING-NOTES.txt")))
			dir = dir.Parent;
		if (dir == null)
		{
			Console.Error.WriteLine("Could not locate the CodeBrix.Platform repo root (run from inside the repo).");
			return 1;
		}
		var repoRoot = dir.FullName;
		var toolRoot = Path.Combine(repoRoot, "tools", "WaylandBindingsGenerator");
		var outDir = Path.Combine(repoRoot, OutputFolder.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(outDir);

		NwgSourceText Source(string relativePath) =>
			new(relativePath, File.ReadAllText(Path.Combine(toolRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))), ProtocolsNamespace);

		var sources = ProtocolXmlFiles.Select(Source).ToArray().ToEquatableArray();

		// Array-typed event arguments the XML cannot describe precisely (same hints the
		// upstream NWayland build applies via NWaylandArrayHint items).
		var arrayMappings = new[]
		{
			new NwgArrayTypeMapping("wayland", "wl_keyboard", "enter", "keys", "int"),
			new NwgArrayTypeMapping("xdg_shell", "xdg_toplevel", "configure", "states", "StateEnum"),
			new NwgArrayTypeMapping("xdg_shell", "xdg_toplevel", "wm_capabilities", "capabilities", "WmCapabilitiesEnum"),
		}.ToEquatableArray();

		var model = new NwgInputModel(sources, arrayMappings, Array.Empty<NwgExternalTypeMapping>().ToEquatableArray());

		var count = 0;
		WaylandProtocolGenerator.GenerateModel(model, (name, source) =>
		{
			var fileName = $"waylandbindings_{name}.g.cs";
			File.WriteAllText(Path.Combine(outDir, fileName), source);
			Console.WriteLine($"  {fileName}  ({source.Length:N0} chars)");
			count++;
		});

		Console.WriteLine($"Wrote {count} generated files to {outDir}");
		return 0;
	}
}
