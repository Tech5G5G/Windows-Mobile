using Windows.Networking.Connectivity;

namespace Windows_Mobile.Networking
{
    public static class Extensions
    {
        public static NetworkType GetNetworkType(this ConnectionProfile profile)
        {
            if (profile is null)
                return NetworkType.None;
            else if (profile.IsWlanConnectionProfile)
                return NetworkType.WiFi;
            else if (profile.IsWwanConnectionProfile)
                return NetworkType.Cellular;
            else
                return NetworkType.Ethernet;
        }
    }
}
