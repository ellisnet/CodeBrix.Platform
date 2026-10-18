namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// The content-size limits a Wayland shell surface sends (xdg_toplevel min/max size, libdecor's min/max content
/// size), made valid for the protocol: no negative value, and a maximum that is set (non-zero) is never smaller than
/// the minimum on the same axis - either would be the protocol's invalid_size error, which ends the connection.
/// </summary>
internal static class WaylandContentLimits
{
	/// <summary>Normalizes requested limits in surface-local logical units; 0 means "no limit".</summary>
	/// <param name="minWidth">The requested minimum width.</param>
	/// <param name="minHeight">The requested minimum height.</param>
	/// <param name="maxWidth">The requested maximum width.</param>
	/// <param name="maxHeight">The requested maximum height.</param>
	/// <returns>The limits to send.</returns>
	internal static (int MinWidth, int MinHeight, int MaxWidth, int MaxHeight) Normalize(int minWidth, int minHeight, int maxWidth, int maxHeight)
	{
		minWidth = minWidth < 0 ? 0 : minWidth;
		minHeight = minHeight < 0 ? 0 : minHeight;
		maxWidth = maxWidth < 0 ? 0 : maxWidth;
		maxHeight = maxHeight < 0 ? 0 : maxHeight;

		if (maxWidth > 0 && maxWidth < minWidth)
		{
			maxWidth = minWidth;
		}

		if (maxHeight > 0 && maxHeight < minHeight)
		{
			maxHeight = minHeight;
		}

		return (minWidth, minHeight, maxWidth, maxHeight);
	}
}
