using Unity.Entities;
using Unity.MegaCity.CameraManagement;
using UnityEngine;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Create tag component for the player camera target
    /// </summary>
    public class PlayerCameraTargetAuthoring : MonoBehaviour
    {

        [BakingVersion("julian", 2)]
        public class PlayerCameraTargetBaker : Baker<PlayerCameraTargetAuthoring>
        {
            public override void Bake(PlayerCameraTargetAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent<PlayerCameraTarget>(entity);
            }
        }
    }
}
