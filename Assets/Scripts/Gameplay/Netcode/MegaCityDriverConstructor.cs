using Unity.Entities;
using Unity.MegaCity.Traffic;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Gameplay
{
    /// <summary>
    /// Register client and server drivers, allowing huge player counts.
    /// </summary>
    public class MegaCityDriverConstructor : INetworkStreamDriverConstructor
    {
        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkSettings();
            // Left as default: FixSettingsForMegaCity(settings, ???);
            DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkServerSettings();
            // https://forum.unity.com/threads/mutiple-errorcode-5-related-warnings.1098229/#post-7731681
            // Assumed 5 packets queued per connection is overkill, but safe.
            // It's only extra memory consumption, which is relatively harmless.
            FixSettingsForMegaCity(settings, NetCodeBootstrap.MaxPlayerCount * 5);
            DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, settings);
        }

        private static void FixSettingsForMegaCity(NetworkSettings settings, int sendReceiveQueueCapacity)
        {
            if (settings.TryGet(out NetworkConfigParameter networkConfig))
            {
                networkConfig.sendQueueCapacity = networkConfig.receiveQueueCapacity = sendReceiveQueueCapacity;
                settings.AddRawParameterStruct(ref networkConfig);
            }
        }
    }
}
