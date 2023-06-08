using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.Extensions;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// System to update the player name tags.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct UpdatePlayerNameTag : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (PlayerInfoController.Instance == null)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (playerName, health, entity) in Query<RefRO<PlayerName>, RefRO<VehicleHealth>>().WithNone<PlayerNameTag, GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                var name = playerName.ValueRO.Name.ToString();
                var currentHealth = health.ValueRO.Value;
                PlayerInfoController.Instance.CreateNameTag(name, entity, currentHealth);
                commandBuffer.AddComponent<PlayerNameTag>(entity);
            }

            var physicsWorld = GetSingleton<PhysicsWorldSingleton>();
            var collisionWorld = physicsWorld.CollisionWorld;
            foreach (var (l2w, health, player, entity) in Query<RefRO<LocalToWorld>, RefRO <VehicleHealth>, RefRO<PlayerName>> ().WithEntityAccess())
            {
                var healthValue = health.ValueRO.Value;
                var playerName = player.ValueRO.Name.ToString();
                PlayerInfoController.Instance.UpdateNamePosition(entity, playerName, healthValue, l2w.ValueRO, collisionWorld);
            }

            PlayerInfoController.Instance.RefreshNameTags(state.EntityManager);
            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
    }
}
