namespace CodeBrix.Platform.UI //Was previously: Uno.UI
{
	public static class CodeBrixMetrics
	{
		public static class TextBlock
		{
			public static long MeasureCacheMisses { get; internal set; }
			public static long MeasureCacheHits { get; internal set; }
		}
	}
}
