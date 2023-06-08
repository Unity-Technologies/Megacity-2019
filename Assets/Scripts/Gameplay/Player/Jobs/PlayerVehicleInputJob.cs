using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Job to collect the player input and send it to the player vehicle.
    /// </summary>
    [BurstCompile]
    public partial struct PlayerVehicleInputJob : IJobEntity
    {
        public PlayerVehicleInput CollectedInput;

        [BurstCompile]
        private void Execute(in PlayerVehicleSettings vehicleSettings, ref PlayerVehicleInput inputSentToEntity)
        {
            if (CollectedInput.ControlDirection.x == 0 && math.any(CollectedInput.GamepadDirection))
            {
                CollectedInput.ControlDirection = CollectedInput.GamepadDirection;
                if (vehicleSettings.InvertPitch)
                {
                    CollectedInput.ControlDirection.x = -CollectedInput.ControlDirection.x;
                }
            }

            inputSentToEntity = CollectedInput;
        }
    }
}
