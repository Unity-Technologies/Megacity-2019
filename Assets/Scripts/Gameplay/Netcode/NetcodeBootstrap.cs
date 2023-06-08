using System;
using Gameplay;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.MegaCity.Gameplay;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.MegaCity.UI;
using Unity.Networking.Transport;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using Unity.Jobs;
using Unity.Services.Samples.GameServerHosting;

namespace Unity.MegaCity.Traffic
{
    /// <summary>
    /// The bootstrap needs to extend `ClientServerBootstrap`, there can only be one class extending it in the project 
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class NetCodeBootstrap : ClientServerBootstrap
    {
        const string k_ServicesDataResourceLocation = "GameServiceData";
        public static NetworkEndpoint MegaCityServerIp => NetworkEndpoint.Parse("128.14.159.58", 7979);

        /// <summary>
        /// Limitation imposed to ensure UTP send/receiveQueueSize's are set appropriately.
        /// <see cref="MegaCityDriverConstructor"/>.
        /// </summary>
        public const int MaxPlayerCount = 200;

        // The initialize method is what entities calls to create the default worlds
        public override bool Initialize(string defaultWorldName)
        {
            // Handle max player count globally.
            NetworkStreamReceiveSystem.DriverConstructor = new MegaCityDriverConstructor();
            
#if UNITY_SERVER && !UNITY_EDITOR
            UnityEngine.Application.targetFrameRate = 60; 
            CreateDefaultClientServerWorlds();
            TryStartUgs();

            // Niki.Walker: Disabled as UNITY_SERVER does not support creating thin client worlds.
            // // On the server, also create thin clients (if requested).
            // TryCreateThinClientsIfRequested();

            return true;
#else

            // Try and auto-connect.
#if UNITY_EDITOR
            if (RequestedPlayType == PlayType.Client)
            {
                if (MultiplayerPlayModePreferences.IsEditorInputtedAddressValidForConnect(out var editorSpecifiedEndpoint))
                {
                    AutoConnectPort = editorSpecifiedEndpoint.Port;
                    DefaultConnectAddress = editorSpecifiedEndpoint;
                    UnityEngine.Debug.Log($"Detected auto-connection preference in 'Multiplayer PlayMode Tool' targeting '{editorSpecifiedEndpoint}' (Port: '{AutoConnectPort}')!");
                }
            }

#else
                // We always set the DefaultConnectAddress in a player, because it's unlikely you'll want to test locally here.
                DefaultConnectAddress = ModeBootstrap.Options.UserSpecifiedEndpoint;

                if (TryCreateThinClientsIfRequested())
                    return true;
#endif

            // Netcode worlds are always created, regardless.
            CreateDefaultClientServerWorlds();
            return true;
#endif
        }

        [UsedImplicitly]
        private static bool TryCreateThinClientsIfRequested()
        {
            if (ModeBootstrap.Options.IsThinClient)
            {
                var requestedNumThinClients = ModeBootstrap.Options.TargetThinClientWorldCount;
                if (requestedNumThinClients > 0)
                {
                    // Hardcoded DefaultConnectAddress for the MegaCity demo.
                    AutoConnectPort = ModeBootstrap.Options.UserSpecifiedEndpoint.Port;

                    for (var i = 0; i < requestedNumThinClients; i++)
                    {
                        Debug.Log($"Creating a Thin Client World! {(i + 1)} of {requestedNumThinClients}...");
                        var world = CreateThinClientWorld();
                        if (i == 0 || World.DefaultGameObjectInjectionWorld == null || !World.DefaultGameObjectInjectionWorld.IsCreated)
                        {
                            World.DefaultGameObjectInjectionWorld = world;
                            Debug.Log($"Setting DefaultGameObjectInjectionWorld to world '{world.Name}'.");
                        }
                    }

                    Debug.Log($"Detected headless client! Automatically creating {requestedNumThinClients} ThinClients, and connecting them to the hardcoded endpoint '{DefaultConnectAddress}' (Port: '{AutoConnectPort}')!");
                    return true;
                }

                Debug.LogError($"Detected headless client, but TargetThinClientWorldCount is {requestedNumThinClients}! Cannot initialize!");
            }

            return false;
        }

        /// <summary>
        /// Runs parallel to the initialization thread from here.
        /// </summary>
        private async void TryStartUgs()
        {
            var gameInfo = Resources.Load<GameServerInfo_Data>(k_ServicesDataResourceLocation);
            if (!gameInfo)
            {
                Debug.LogError($"[GSH] No Game Server Info Object at 'Assets/Resources/{k_ServicesDataResourceLocation}'");
                return;
            }

            var multiplayGameServerManager = new GameObject("MultiplayServer").AddComponent<GameServerManager>();
            multiplayGameServerManager.Init(gameInfo);
            Debug.LogError($"[GSH] mMltiplay GameServer Manager {gameInfo.Data}'");
            
            if (!await multiplayGameServerManager.InitServices())
                return;
            Debug.LogError($"[GSH] Try Start Server {ModeBootstrap.Options.UserSpecifiedEndpoint.Address}");
            await multiplayGameServerManager.TryStartServer(ModeBootstrap.Options.UserSpecifiedEndpoint.Address);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(NetworkStreamReceiveSystem))]
    public partial struct ServerInGame : ISystem
    {
        #region Jobs

        [BurstCompile]
        private partial struct GetPositionJob : IJob
        {
            public NativeArray<SpawnPointElement> SpawnPoints;
            public NativeList<float3> UsedPositions;
            public Mathematics.Random Random;

            [BurstCompile]
            public void Execute()
            {
                var availablePositions = CreateAvailablePositions();

                // If all player positions have been used, reset the list
                if (availablePositions.Length == 0)
                {
                    UsedPositions.Clear();
                    availablePositions = CreateAvailablePositions();
                }

                // Choose a random position from the list of available player names
                var randomIndex = Random.NextInt(0, availablePositions.Length);
                var position = availablePositions[randomIndex];
                UsedPositions.Add(position);
            }

            private NativeList<float3> CreateAvailablePositions()
            {
                var availablePositions = new NativeList<float3>(Allocator.TempJob);

                // Get a list of spawnPoints that have not been used
                foreach (var position in SpawnPoints)
                {
                    if (!UsedPositions.Contains(position.Value))
                    {
                        availablePositions.Add(position.Value);
                    }
                }

                return availablePositions;
            }
        }

        [BurstCompile]
        partial struct UpdateConnectionPositionSystemJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

            public void Execute(ref GhostConnectionPosition conPos, in CommandTarget target)
            {
                if (!transformLookup.HasComponent(target.targetEntity))
                    return;
                conPos = new GhostConnectionPosition
                {
                    Position = transformLookup[target.targetEntity].Position
                };
            }
        }

        #endregion

        private NativeList<float3> m_UsedPositions;
        private Mathematics.Random m_Random;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawner>();
            state.RequireForUpdate<SpawnPointElement>();
            m_UsedPositions = new NativeList<float3>(Allocator.Persistent);
            var currentTime = DateTime.Now;
            var seed = currentTime.Minute + currentTime.Second + currentTime.Millisecond + 1;
            m_Random = new Mathematics.Random((uint)seed);

            GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(ModeBootstrap.Options.UserSpecifiedEndpoint.Port));

            const int tileSize = 256;
            var grid = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(grid, "GhostImportanceSingleton");
            state.EntityManager.AddComponentData(grid, new GhostDistanceData
            {
                TileSize = new int3(tileSize, 1024 * 8, tileSize),
                TileCenter = new int3(0, 0, 0),
                TileBorderWidth = new float3(5f),
            });
            state.EntityManager.AddComponentData(grid, new GhostImportance
            {
                ScaleImportanceFunction = GhostDistanceImportance.ScaleFunctionPointer,
                GhostConnectionComponentType = ComponentType.ReadOnly<GhostConnectionPosition>(),
                GhostImportanceDataType = ComponentType.ReadOnly<GhostDistanceData>(),
                GhostImportancePerChunkDataType = ComponentType.ReadOnly<GhostDistancePartitionShared>(),
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawnBuffer = GetSingletonBuffer<SpawnPointElement>();
            var prefab = GetSingleton<PlayerSpawner>().Player;
            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            var originalTrans = state.EntityManager.GetComponentData<LocalTransform>(prefab);
            var health = state.EntityManager.GetComponentData<VehicleHealth>(prefab);
            state.EntityManager.GetName(prefab, out var prefabName);

            foreach (var (netId, entity) in Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                var findNewPosition = new GetPositionJob
                {
                    SpawnPoints = spawnBuffer.ToNativeArray(Allocator.TempJob),
                    UsedPositions = m_UsedPositions,
                    Random = m_Random
                };
                state.Dependency = findNewPosition.Schedule(state.Dependency);
                state.Dependency.Complete();

                cmdBuffer.AddComponent<NetworkStreamInGame>(entity);
                var player = cmdBuffer.Instantiate(prefab);
                var networkIdValue = netId.ValueRO.Value;
                cmdBuffer.SetComponent(player, new GhostOwner { NetworkId = networkIdValue });
                var newTrans = originalTrans;
                newTrans.Position = m_UsedPositions[m_UsedPositions.Length - 1];

                cmdBuffer.SetComponent(player, newTrans);
                cmdBuffer.AppendToBuffer(entity, new LinkedEntityGroup { Value = player });
                cmdBuffer.SetComponent(player, health);

                cmdBuffer.AddComponent<GhostConnectionPosition>(entity);
                cmdBuffer.SetComponent(entity, new CommandTarget { targetEntity = player });
            }

            cmdBuffer.Playback(state.EntityManager);

            var updateJob = new UpdateConnectionPositionSystemJob
            {
                transformLookup = GetComponentLookup<LocalTransform>(true)
            };

            state.Dependency = updateJob.ScheduleParallel(state.Dependency);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ClientInGame : ISystem
    {
        private bool m_HasRegisteredSmoothingAction;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawner>();

            var tickRate = NetworkTimeSystem.DefaultClientTickRate;
            tickRate.MaxExtrapolationTimeSimTicks = 120;
            tickRate.InterpolationTimeMS = 150;
            state.EntityManager.CreateSingleton(tickRate);

            // Niki.Walker: Client-side optimizations:
            var ghostSendSystemData = new GhostSendSystemData
            {
                MinSendImportance = 2
            };

            // Don't frequently resend the same bot vehicles.
            //ghostSendSystemData.FirstSendImportanceMultiplier = 100; // Significantly bias towards sending new ghosts.
            // Disabled as it ruins the start of the game.
            state.EntityManager.CreateSingleton(ghostSendSystemData);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!m_HasRegisteredSmoothingAction && TryGetSingletonRW<GhostPredictionSmoothing>(out var ghostPredictionSmoothing))
            {
                m_HasRegisteredSmoothingAction = true;
                ghostPredictionSmoothing.ValueRW.RegisterSmoothingAction<LocalTransform>(state.EntityManager, MegaCitySmoothingAction.Action);
            }

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (netId, entity) in Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                cmdBuffer.AddComponent<NetworkStreamInGame>(entity);
            }

            cmdBuffer.Playback(state.EntityManager);
        }
    }
}