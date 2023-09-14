using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Scripting;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Variants for the Megacity project
    /// </summary>
    [BakingVersion("niki", 2)]
    public sealed partial class MegacityVariants : DefaultVariantSystemBase
    {
        protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
        {
            defaultVariants.Add(ComponentType.ReadOnly<LocalTransform>(), Rule.OnlyParents(typeof(MegacityLocalTransform)));
            defaultVariants.Add(ComponentType.ReadOnly<PhysicsVelocity>(), Rule.OnlyParents(typeof(MegacityPhysicsVelocity)));
            defaultVariants.Add(ComponentType.ReadOnly<PhysicsDamping>(), Rule.OnlyParents(typeof(PhysicsDampingMegacity)));
        }
    }

    [Preserve]
    [GhostComponentVariation(typeof(LocalTransform), "Megacity - LocalTransform")]
    [GhostComponent(PrefabType=GhostPrefabType.All, SendTypeOptimization=GhostSendType.AllClients)]
    public struct MegacityLocalTransform
    {
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.InterpolateAndExtrapolate, MaxSmoothingDistance = 20)]
        public float3 Position;

        [GhostField(Quantization=1000, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
        public quaternion Rotation;
    }

    [GhostComponentVariation(typeof(PhysicsVelocity), "Megacity - PhysicsVelocity")]
    [GhostComponent(PrefabType = GhostPrefabType.All, SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    public struct MegacityPhysicsVelocity
    {
        [GhostField(Quantization = 1000)] public float3 Linear;
        [GhostField(Quantization = 1000)] public float3 Angular;
    }

    // This should not be removed!!
    [GhostComponentVariation(typeof(PhysicsDamping), "Megacity - PhysicsDamping")]
    [GhostComponent(PrefabType = GhostPrefabType.All, SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    public struct PhysicsDampingMegacity
    {
        [GhostField(Quantization = 1000)] public float Linear;
        [GhostField(Quantization = 1000)] public float Angular;
    }
}
