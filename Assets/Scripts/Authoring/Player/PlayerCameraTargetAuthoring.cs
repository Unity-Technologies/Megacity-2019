using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.CameraManagement;
using UnityEngine;

namespace Unity.Megacity.Authoring
{
    /// <summary>
    /// Create tag component for the player camera target
    /// </summary>
    public class PlayerCameraTargetAuthoring : MonoBehaviour
    {
        public float3 PositionOffset;
        [BakingVersion("julian", 2)]
        public class PlayerCameraTargetBaker : Baker<PlayerCameraTargetAuthoring>
        {
            public override void Bake(PlayerCameraTargetAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent<PlayerCameraTarget>(entity, new PlayerCameraTarget{PositionOffset = authoring.PositionOffset});
            }
        }
    }
}
