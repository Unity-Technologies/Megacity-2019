using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Capture the user input and apply them to a component for later uses
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerVehicleInput : IInputComponentData
    {
        public float3 ControlDirection;
        public float3 GamepadDirection;

        public float Acceleration; // acceleration
        public float Brake; // brake
        public float RightRoll; // manual roll to right
        public float LeftRoll; // manual roll to left
        public bool Shoot; // Shoot laser (X or A)

        // Cheats
        public bool Cheat_1; // Autohurt
    }
}
