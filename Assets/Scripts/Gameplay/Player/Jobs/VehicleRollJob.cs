using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Job to handle the vehicle roll.
    /// </summary>
    [WithAll(typeof(Simulate))]
    internal partial struct VehicleRollJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<VehicleBraking> VehicleBrakingLookup;
        [ReadOnly] public ComponentLookup<VehicleRoll> VehicleRollLookup;

        public void Execute(ref LocalTransform transform, in PlayerVehicleCosmeticPhysics cosmetic)
        {
            var vehicleBraking = VehicleBrakingLookup[cosmetic.VehicleEntity];
            var vehicleRoll = VehicleRollLookup[cosmetic.VehicleEntity];
            var roll = vehicleRoll.ManualRollValue != 0 ? vehicleRoll.ManualRollValue : vehicleRoll.BankAmount;
            var eulerZXY = math.radians(new float3(vehicleBraking.PitchPseudoBraking, vehicleBraking.YawBreakRotation, roll));
            transform.Rotation = quaternion.EulerZXY(eulerZXY);
        }
    }
}
