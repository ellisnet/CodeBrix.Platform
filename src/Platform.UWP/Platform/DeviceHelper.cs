namespace CodeBrix.Platform
{
	internal static class DeviceHelper
	{
		internal static bool IsSimulator { get; }
#if false
			= ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR;
#endif
	}
}
