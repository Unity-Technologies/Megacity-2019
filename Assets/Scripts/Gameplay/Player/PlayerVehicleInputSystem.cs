using Unity.Entities;
using Unity.Mathematics;
using Unity.MegaCity.CameraManagement;
using UnityEditor;
using UnityEngine;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// System to collect the player input and send it to the player vehicle.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerVehicleInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ControlSettings>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (HybridCameraManager.Instance == null || HybridCameraManager.Instance.m_CameraTargetMode !=
                HybridCameraManager.CameraTargetMode.FollowPlayer)
                return;

            var controlSettings = SystemAPI.GetSingleton<ControlSettings>();
            var invertHorizontal = controlSettings.InverseLookHorizontal ? -1 : 1;
            var invertVertical = controlSettings.InverseLookVertical ? -1 : 1;

            var input = new PlayerVehicleInput
            {
                Acceleration = Input.GetAxis("Vertical"),
                RightRoll = Input.GetAxis("RightTrigger2"),
                LeftRoll = Input.GetAxis("LeftTrigger2"),
                ControlDirection = new float3(
                    math.clamp(-Input.GetAxis("Mouse Y") - Input.GetAxis("VerticalArrow"), -1f, 1f) * invertVertical,
                    math.clamp(Input.GetAxis("Mouse X") + Input.GetAxis("Horizontal"), -1f, 1f) * invertHorizontal,
                    0) * controlSettings.MouseSensitivity,
                GamepadDirection = new float3(Input.GetAxis("RightStickY") * invertVertical,
                    Input.GetAxis("RightStickX") * invertHorizontal, 0) * controlSettings.MouseSensitivity,
                Shoot = Input.GetButton("Jump"),
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Cheat_1 = Input.GetKey(KeyCode.Alpha1)
#endif
            };
#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.H))
            {
                EditorApplication.isPaused = true;
            }
#endif
            var job = new PlayerVehicleInputJob {CollectedInput = input};
            state.Dependency = job.Schedule(state.Dependency);
        }
    }
}