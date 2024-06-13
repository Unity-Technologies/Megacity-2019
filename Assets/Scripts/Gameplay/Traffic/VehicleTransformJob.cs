using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Sets the npc vehicles transform and calculate their bank
    /// </summary>
    [BurstCompile]
    internal partial struct VehicleTransformJob : IJobEntity
    {
        public void Execute(
            in VehiclePhysicsState physicsState,
            ref LocalTransform transformAspect)
        {
            transformAspect.Position = physicsState.Position;

            var orient = quaternion.LookRotation(physicsState.Heading, up());
            var bankQuaternion = FastBankQuaternion(physicsState.BankRadians);
            transformAspect.Rotation = mul(orient, bankQuaternion);
        }

        // Uses unexpanded Taylor root expression (x, 1 respectively) for sin(), cos().
        // This looks good where x is within -1 to 1.
        private quaternion FastBankQuaternion(float radians)
        {
            // Create a unit quaternion
            // Length of quaternion = Sqrt ( w^2 + x^2 + y^2 + z^2)
            var length = sqrt(1.0f + radians * 0.5f * (radians * 0.5f));
            return new quaternion(0, 0, radians * 0.5f / length, 1.0f / length);
        }
    }
}
