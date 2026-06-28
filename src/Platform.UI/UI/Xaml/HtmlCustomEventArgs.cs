using System;

namespace CodeBrix.Platform.Extensions
{
	public class HtmlCustomEventArgs : EventArgs
	{
		public string Detail { get; }

		public HtmlCustomEventArgs(string detail)
		{
			Detail = detail;
		}
	}
}
