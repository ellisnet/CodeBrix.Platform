using System;
using System.Collections.Generic;
using System.IO;

// ReSharper disable CheckNamespace

namespace CommandBarDemo.Views;

/// <summary>
/// The demo's icon set: SVG artwork in light and dark pairs, one PNG and one JPEG.
/// </summary>
/// <remarks>
/// <para>
/// The files are written to a folder under the system temporary directory the first time they are
/// asked for, and the demo points its icon sources at those files. That is deliberate: it keeps the
/// demo free of asset plumbing, so what it shows is the add-in's icon story and nothing else, and it
/// makes every head - including the ones with no packaged assets - show the same pictures.
/// </para>
/// <para>
/// A real application ships its icons as content and writes <c>ms-appx:///Icons/new.svg</c>, or
/// embeds them in a library and writes <c>cb-res://MyLib/new.svg</c>. Both are file paths as far as
/// the icon sources are concerned; this one just makes them at start-up.
/// </para>
/// </remarks>
public static class DemoIcons
{
	/// <summary>The ink a light theme's artwork is drawn in.</summary>
	private const string LightInk = "#1B1B1B";

	/// <summary>The ink a dark theme's artwork is drawn in.</summary>
	private const string DarkInk = "#F2F2F2";

	/// <summary>A 24x24 PNG magnifier with an alpha channel, so a tint has a mask to paint.</summary>
	private const string MagnifierPngBase64 =
		"iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA+UlEQVRIx+2TQQ7BUBCGscWyTdzDAXAFXMROl7gQdQM7QYOF"
		+ "lYUjaK372fxNGqKZV8TGJJNJXv+Z773pTKXyt7IG+MAUiICbPNKZ927xARDz2q5Av2zxIZCq0BzoAHV5F1joW+oMUVuym48L"
		+ "dEHuJfZ2qb8Ac4M2lHbiAtgrqWPQ9qSNXACJkhoGbVPapEhXM9OfraqYugDOim0DoP2QYwKEiiMDINMszW9+GNOgQDeTJnbe"
		+ "am1xtmihpqUh7wHL3EZfAN8JIEhfS/TKYhUHOJaFeMAE2Gp8E2CnM09+EOQEtJwhxkv8IWaIrx8OsP44IAfZAKuvAH5id+Zs"
		+ "zBAl/hrkAAAAAElFTkSuQmCC";

	/// <summary>A 24x24 JPEG score thumbnail: opaque, as a JPEG always is.</summary>
	private const string ScoreJpegBase64 =
		"/9j/4AAQSkZJRgABAQIAJQAlAAD/2wBDAAUDBAQEAwUEBAQFBQUGBwwIBwcHBw8LCwkMEQ8SEhEPERETFhwXExQaFRERGCEY"
		+ "Gh0dHx8fExciJCIeJBweHx7/2wBDAQUFBQcGBw4ICA4eFBEUHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4eHh4e"
		+ "Hh4eHh4eHh4eHh4eHh7/wAARCAAYABgDASIAAhEBAxEB/8QAGAABAAMBAAAAAAAAAAAAAAAAAAQFBgH/xAAwEAAABQMCAgYL"
		+ "AAAAAAAAAAABAgMEBQAGEQchEjEjJzM3cbEyNTZRYWdzgqKkwv/EABUBAQEAAAAAAAAAAAAAAAAAAAUE/8QAIBEAAgICAgID"
		+ "AAAAAAAAAAAAAREC"
		+ "AwAEEjEFIiFBwf/aAAwDAQACEQMRAD8Avb4uDUub1BuJCAeXKshHv1W4IxYrcCRCnMUmQT2ARAg7jzwN"
		+ "VfXT8wf3K1dpMyv9Vr3amfXkxA8urleCcFRSL0y3bmN+IBkfTrQSrMrB8o1K+1yfATGF2TgqyRvAxfIcDQAvqlcajZ79p/Ke"
		+ "AjXsnA2klMjMRY1walwmoNuoT7y5UkJB+k3FGUFbgVIY5SnwCmwiAGDcOWQpUyX7x7E7wfW6XtV9ZHsf6+ylJ6bHIPLfHMCU"
		+ "W0fzOXFF6k2rqhPSMFCyzls9fquMINVFm65DHMYnEBNhEAOPxDI++qqVea0P3yjorG8mIHxhBk2dIpF8Cl8xyNKUfPQpF5uA"
		+ "9un9rJrISi6xM8W08l2Nb2pc3qDbq8+zuVVCPfpOBWlAW4EiFOUx8CpsAiBQ2DngKUpSWpARiTlujUIQJfef/9k=";

	private static readonly object Lock = new();
	private static string _folder;

	/// <summary>The folder the artwork was written to.</summary>
	public static string Folder
	{
		get
		{
			EnsureWritten();
			return _folder;
		}
	}

	/// <summary>The light-theme artwork for one icon.</summary>
	/// <param name="name">The icon's name, such as "new".</param>
	/// <returns>A file URI.</returns>
	public static Uri Light(string name) => FileUri(name + ".svg");

	/// <summary>The dark-theme artwork for one icon.</summary>
	/// <param name="name">The icon's name, such as "new".</param>
	/// <returns>A file URI.</returns>
	public static Uri Dark(string name) => FileUri(name + ".dark.svg");

	/// <summary>The back-a-page icon, drawn with <c>currentColor</c> so a tint decides its colour.</summary>
	public static Uri TintablePrevious => FileUri("pager-previous.svg");

	/// <summary>The on-a-page icon, drawn with <c>currentColor</c> so a tint decides its colour.</summary>
	public static Uri TintableNext => FileUri("pager-next.svg");

	/// <summary>The PNG icon: a magnifier with an alpha channel.</summary>
	public static Uri Png => FileUri("magnifier.png");

	/// <summary>The JPEG icon: an opaque score thumbnail.</summary>
	public static Uri Jpeg => FileUri("score.jpg");

	/// <summary>The file URI of one written icon.</summary>
	/// <param name="fileName">The file's name inside <see cref="Folder"/>.</param>
	/// <returns>A file URI.</returns>
	public static Uri FileUri(string fileName) => new(Path.Combine(Folder, fileName));

	private static void EnsureWritten()
	{
		lock (Lock)
		{
			if (_folder != null)
			{
				return;
			}

			var folder = Path.Combine(Path.GetTempPath(), "commandbardemo-icons");
			Directory.CreateDirectory(folder);

			foreach (var pair in Artwork)
			{
				File.WriteAllText(Path.Combine(folder, pair.Key + ".svg"), Svg(pair.Value, LightInk));
				File.WriteAllText(Path.Combine(folder, pair.Key + ".dark.svg"), Svg(pair.Value, DarkInk));
			}

			//The two that state no colour at all: currentColor, which the add-in resolves against
			//each button's Tint through a stylesheet handed to the SVG parser.
			File.WriteAllText(Path.Combine(folder, "pager-previous.svg"), Svg(PagerPrevious, "currentColor"));
			File.WriteAllText(Path.Combine(folder, "pager-next.svg"), Svg(PagerNext, "currentColor"));

			File.WriteAllBytes(Path.Combine(folder, "magnifier.png"), Convert.FromBase64String(MagnifierPngBase64));
			File.WriteAllBytes(Path.Combine(folder, "score.jpg"), Convert.FromBase64String(ScoreJpegBase64));

			_folder = folder;
		}
	}

	private static string Svg(string body, string ink)
		=> "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" "
			+ $"fill=\"none\" stroke=\"{ink}\" stroke-width=\"2\" stroke-linecap=\"round\" "
			+ $"stroke-linejoin=\"round\">{body}</svg>";

	/// <summary>The shape of each icon, drawn as strokes so one ink colour describes the whole thing.</summary>
	private static readonly Dictionary<string, string> Artwork = new()
	{
		//A page with a plus: new score.
		["new"] = "<path d='M6 3h8l4 4v14H6z'/><path d='M14 3v4h4'/><path d='M12 11v6'/><path d='M9 14h6'/>",
		//A folder: open.
		["open"] = "<path d='M3 6h6l2 3h10v10H3z'/>",
		//A floppy disk: save.
		["save"] = "<path d='M4 4h12l4 4v12H4z'/><path d='M8 4v6h8V4'/><path d='M8 20v-6h8v6'/>",
		//A printer: print.
		["print"] = "<path d='M7 9V3h10v6'/><path d='M4 9h16v7H4z'/><path d='M7 14h10v7H7z'/>",
		//A crotchet: engrave.
		["engrave"] = "<circle cx='8' cy='17' r='3.5'/><path d='M11.5 17V4l8 2'/>",
		//A wrench: engrave options, so the menu items have artwork of their own.
		["mode"] = "<path d='M16 4a5 5 0 0 0-6 6L4 16l4 4 6-6a5 5 0 0 0 6-6l-3 3-3-3z'/>",
	};

	/// <summary>Two chevrons pointing back, drawn in currentColor so the pager follows its tint.</summary>
	private const string PagerPrevious = "<path d='M11 6l-5 6 5 6'/><path d='M18 6l-5 6 5 6'/>";

	/// <summary>Two chevrons pointing on, the mirror of <see cref="PagerPrevious"/>.</summary>
	private const string PagerNext = "<path d='M13 6l5 6-5 6'/><path d='M6 6l5 6-5 6'/>";
}
