#if !__NETSTD_REFERENCE__
namespace Windows.Networking.Connectivity;

internal interface IConnectionProfileExtension
{
	NetworkConnectivityLevel GetNetworkConnectivityLevel();
}
#endif
