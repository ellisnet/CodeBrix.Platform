using Windows.Networking.Connectivity;
using Windows.Win32;

namespace CodeBrix.Platform.UI.Runtime.Skia.Win32; //Was previously: Uno.UI.Runtime.Skia.Win32

internal class Win32ConnectionProfileExtension : IConnectionProfileExtension
{
	public static Win32ConnectionProfileExtension Instance { get; } = new();

	private Win32ConnectionProfileExtension()
	{
	}

	public NetworkConnectivityLevel GetNetworkConnectivityLevel() =>
		PInvoke.InternetGetConnectedState(out _) ?
			NetworkConnectivityLevel.InternetAccess : NetworkConnectivityLevel.None;
}
