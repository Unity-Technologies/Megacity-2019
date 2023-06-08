using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using Unity.MegaCity.Gameplay;

namespace Unity.MegaCity.Authoring
{
    /// <summary>
    /// Create all the required player vehicle components
    /// </summary>
    public class PlayerVehicleControllerAuthoring : MonoBehaviour
    {
        [Header("Speed")]
        public float MaxSpeed = 50.0f;
        public float Acceleration = 5.0f;
        public float Deceleration = 10.0f;
        public float SteeringSpeed = 5.0f;
        public float DragBreakForce = 1.8f;

        [Header("Cosmetic Yaw")] 
        public float MaxYawAngle = 45.0f;
        public float YawKickBack = 2.0f;
        public AnimationCurve YawVolatilityCurve = new();

        [Header("Cosmetic Pitch")] 
        public bool InvertPitch;
        public float MaxPitchAngle = 20.0f;
        public float PitchForce = 4.0f;
        public AnimationCurve PitchVolatilityCurve = new();

        [Header("Cosmetic Roll")] 
        public float ManualRollMaxSpeed = 4.0f;
        public float ManualRollAcceleration = 0.5f;
        public float RollAutoLevelVelocity = 5.0f;

        [Header("Cosmetic Banking")] 
        public float MaxBankAngle = 45.0f;
        public float BankVolatility = 2.0f;
        public AnimationCurve BankVolatilityCurve = new();

        [Header("Camera")] 
        public float FollowCameraZBreakZoom = 2.0f;
        public float FollowCameraZBreakSpeed = 2.0f;
        public float TargetFollowDamping = 2.0f;
        public float TargetSqLerpThreshold = 2500;

        [Header("Laser")]
        public float ForwardOffset = 5f;
        public float LaserLength = 100f;
        public float InitialEnergy = 100f;
        public float ChargeSpeed = 2.5f;

        [Header("Immunity")]
        public float ImmunityTickAmount = 600.0f;

        [BakingVersion("julian", 4)]
        public class PlayerVehicleControllerBaker : Baker<PlayerVehicleControllerAuthoring>
        {
            public override void Bake(PlayerVehicleControllerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new VehicleLaser
                {
                    ForwardOffset = authoring.ForwardOffset,
                    Length = authoring.LaserLength,
                    Energy = authoring.InitialEnergy,
                    ChargerSpeed = authoring.ChargeSpeed
                });
                AddComponent(entity, new VehicleHealth { Value = 100 });
                AddComponent(entity, new PlayerScore { Value = 0 });
                AddComponent(entity, new Immunity { TickAmount = authoring.ImmunityTickAmount, StateChangeTick = NetworkTick.Invalid });
                // Add player inputs component
                var input = new PlayerVehicleInput
                {
                    ControlDirection = float3.zero,
                    Acceleration = 0,
                    Brake = 0,
                    RightRoll = 0,
                    LeftRoll = 0
                };
                AddComponent(entity, input);
                // Get physics damping data
                var physicsBody = GetComponent<PhysicsBodyAuthoring>();
                var damping = new PhysicsDamping
                {
                    Angular = physicsBody.AngularDamping,
                    Linear = physicsBody.LinearDamping
                };

                // Adding a really small constant (Time.fixedDeltaTime) to avoid subtraction to be Zero (0),
                // in case damping.Linear will be 1. Also this allows to modify the speed according to Physics values.
                var maxVelocity = (authoring.MaxSpeed / damping.Linear - Time.fixedDeltaTime * authoring.MaxSpeed) /
                                  physicsBody.Mass;

                // Add vehicle settings component
                var settings = new PlayerVehicleSettings
                {
                    Acceleration = authoring.Acceleration,
                    Deceleration = authoring.Deceleration,
                    MaxSpeed = authoring.MaxSpeed,
                    InvMaxVelocity = 1f / maxVelocity,
                    DragBreakForce = authoring.DragBreakForce,
                    YawKickBack = authoring.YawKickBack,
                    PitchForce = authoring.PitchForce,
                    Damping = damping,
                    RollAutoLevelVelocity = authoring.RollAutoLevelVelocity,
                    MaxBankAngle = authoring.MaxBankAngle,
                    MaxYawAngle = authoring.MaxYawAngle,
                    MaxPitchAngle = authoring.MaxPitchAngle,
                    BankVolatility = authoring.BankVolatility,
                    ManualRollMaxSpeed = authoring.ManualRollMaxSpeed,
                    ManualRollAcceleration = authoring.ManualRollAcceleration,
                    SteeringSpeed = authoring.SteeringSpeed,
                    InvertPitch = authoring.InvertPitch,
                    TargetFollowDamping = authoring.TargetFollowDamping,
                    TargetSqLerpThreshold = authoring.TargetSqLerpThreshold
                };
                AddComponent(entity, settings);

                // Add vehicle state component
                AddComponent<VehicleThrust>(entity);
                AddComponent<VehicleBraking>(entity);
                AddComponent<VehicleRoll>(entity);

                // Add player camera settings
                var cameraSettings = new PlayerVehicleCameraSettings
                {
                    FollowCameraZBreakZoom = authoring.FollowCameraZBreakZoom,
                    FollowCameraZBreakSpeed = authoring.FollowCameraZBreakSpeed,
                    FollowCameraZFollow = 0
                };
                AddComponent(entity, cameraSettings);

                // Bake vehicle animation curves
                var bankCurveBlob = AnimationCurveBlob.CreateBlob(authoring.BankVolatilityCurve, Allocator.Persistent);
                var pitchCurveBlob = AnimationCurveBlob.CreateBlob(authoring.PitchVolatilityCurve, Allocator.Persistent);
                var yawCurveBlob = AnimationCurveBlob.CreateBlob(authoring.YawVolatilityCurve, Allocator.Persistent);

                var vehicleVolatilityCurves = new VehicleVolatilityCurves
                {
                    BankVolatilityCurve = bankCurveBlob,
                    PitchVolatilityCurve = pitchCurveBlob,
                    YawVolatilityCurve = yawCurveBlob
                };
                AddComponent(entity, vehicleVolatilityCurves);
            }
        }
    }
}
