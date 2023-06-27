using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Extensions
{
    [UpdateInGroup(typeof(NetCodePanelStats))]
    public partial struct ClientConnectionMonitorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableConnectionMonitor>();
            state.RequireForUpdate<ConnectionStateInfoRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (NetcodePanelStats.Instance == null && NetcodePanelStats.Instance.IsMonitorEnable)
                return;
            
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (info, request, requestEntity) in SystemAPI.Query<ConnectionStateInfoRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                var entityNetwork = request.SourceConnection;
                var networkId = SystemAPI.GetComponent<NetworkId>(entityNetwork);

                foreach (var (owner, playerName) in SystemAPI.Query<RefRO<GhostOwner>,RefRO<PlayerName>>())
                {
                    if (owner.ValueRO.NetworkId == networkId.Value)
                    {
                        //Notify connection state in the UI
                        commandBuffer.DestroyEntity(requestEntity);
                        NetcodePanelStats.Instance.Monitor.AddEntry(playerName.ValueRO.Name, (ConnectionState.State)info.State);
                        Debug.Log($"[Client] Player:{playerName.ValueRO.Name} [{networkId}] is {(ConnectionState.State)info.State}");
                        break;
                    }
                }
                
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}