using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Logging;

namespace Windows.System.Power;

partial class PowerManager
{
	/// <summary>
	/// Initializes the PowerManager.
	/// </summary>
	/// <returns>A value indicating whether the initialization succeeded.</returns>
	public static Task<bool> InitializeAsync()
	{
#if false
		return InitializePlatformAsync();
#elif false
		return Task.FromResult(true);
#else
		if (typeof(PowerManager).Log().IsEnabled(LogLevel.Error))
		{
			typeof(PowerManager).Log().LogError("PowerManager is not implemented on this platform");
		}

		return Task.FromResult(false);
#endif
	}
}
