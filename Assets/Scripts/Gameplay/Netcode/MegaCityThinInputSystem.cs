using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// System to handle input for the thin client
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct MegaCityThinInputSystem : ISystem
    {
        private NativeReference<Random> m_Rand;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NetworkTime>();
            m_Rand = new NativeReference<Random>(Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Rand.Value = Random.CreateFromIndex((uint) Stopwatch.GetTimestamp());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_Rand.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<CommandTarget>(out var commandTargetRw))
                return;

            // Ensure AI has input entity:
            if (commandTargetRw.ValueRO.targetEntity == Entity.Null || !state.EntityManager.HasComponent<PlayerVehicleInput>(commandTargetRw.ValueRO.targetEntity))
            {
                var inputEntity = state.EntityManager.CreateEntity();
                commandTargetRw.ValueRW.targetEntity = inputEntity;

                var connectionId = SystemAPI.GetSingleton<NetworkId>().Value;

                state.EntityManager.SetName(inputEntity, $"{nameof(MegaCityThinInputSystem)}-RandInput");
                state.EntityManager.AddComponent<PlayerVehicleInput>(inputEntity);
                // NOTE: The buffer type might not be recognized in your IDE but it will be generated and Unity will recognize it.
                // TODO : verify this buffer
                //state.EntityManager.AddBuffer<Unity.MegaCity.Gameplay.Generated.PlayerVehicleInputInputBufferData>(inputEntity);

                state.EntityManager.AddComponentData(inputEntity, new GhostOwner { NetworkId = connectionId });

                return; // Return as the EntityManager has now had a structural change, invalidating `commandTargetRw`.
            }

            // Recalculate AI action every x ticks:
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (networkTime.ServerTick.TickIndexForValidTick % 30 == 0)
            {
                var input = state.EntityManager.GetComponentData<PlayerVehicleInput>(commandTargetRw.ValueRO.targetEntity);
                var rand = m_Rand.Value;

                // Movement:
                input.ControlDirection = (rand.NextFloat3() * 2) - new float3(1);
                input.GamepadDirection = (rand.NextFloat3() * 2) - new float3(1);

                // Drive:
                input.Acceleration = rand.NextInt() < 0.8f ? 1 : 0;
                input.Brake = rand.NextInt() < 0.1f ? 1 : 0;

                // Roll:
                input.LeftRoll = rand.NextInt() < 0.1f ? 1 : 0;
                input.RightRoll = input.LeftRoll <= 0f && rand.NextInt() < 0.1f ? 1 : 0;

                // Shooting:
                input.Shoot = rand.NextInt() < 0.3f;

                m_Rand.Value = rand;
                state.EntityManager.SetComponentData(commandTargetRw.ValueRO.targetEntity, input);
            }
        }
    }
}
