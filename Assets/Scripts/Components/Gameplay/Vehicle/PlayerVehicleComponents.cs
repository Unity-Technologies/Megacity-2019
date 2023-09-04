using Unity.Entities;
using Unity.Physics;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// Set of components required for player vehicle movement and control
    /// </summary>
    public struct PlayerVehicleSettings : IComponentData
    {
        public float Acceleration;
        public float Deceleration;
        public float MaxSpeed;
        public float DragBreakForce;
        public float PitchForce;
        public float YawKickBack;
        public PhysicsDamping Damping;
        public float RollAutoLevelVelocity;
        public float MaxBankAngle;
        public float MaxYawAngle;
        public float MaxPitchAngle;
        public float BankVolatility;
        public float ManualRollMaxSpeed;
        public float ManualRollAcceleration;
        public float SteeringSpeed;
        public float InvMaxVelocity;
        public bool InvertPitch;
        public float TargetFollowDamping;
        public float TargetSqLerpThreshold;
    }
}
