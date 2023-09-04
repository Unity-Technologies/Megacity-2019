using Unity.Entities;
using Unity.Mathematics;
using Unity.Megacity.CameraManagement;
#if UNITY_ANDROID || UNITY_IPHONE || ENABLED_VIRTUAL_JOYSTICK
using UnityEngine;
using Unity.Megacity.UI;
#else
using UnityEngine;
#endif
using Unity.NetCode;

namespace Unity.Megacity.Gameplay
{
    /// <summary>
    /// System to collect the player input and send it to the player vehicle.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class PlayerVehicleInputSystemBase : SystemBase
    { 
        private GameInput m_GameInput;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<ControlSettings>();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            m_GameInput?.Disable();
        }

        protected override void OnUpdate()
        {
            if (HybridCameraManager.Instance == null || !HybridCameraManager.Instance.IsFollowCamera)
                return;

            if (m_GameInput == null)
            {
                m_GameInput = new GameInput();
                m_GameInput.Enable();
                m_GameInput.Gameplay.Enable();
            }

            var controlSettings = SystemAPI.GetSingleton<ControlSettings>();
            var invertHorizontal = controlSettings.InverseLookHorizontal ? -1 : 1;
            var invertVertical = controlSettings.InverseLookVertical ? -1 : 1;
            var accelerationRange = controlSettings.AccelerationRange;
            var directionRange = controlSettings.DirectionRange;
#if UNITY_ANDROID || UNITY_IPHONE || ENABLED_VIRTUAL_JOYSTICK
            
            var input = new PlayerVehicleInput
            {
                Acceleration = math.clamp(-HUD.Instance.JoystickRight.Delta.y, accelerationRange.x, accelerationRange.y),
                //Roll = gameplayInputActions.Roll.ReadValue<float>(),
                ControlDirection = new float3(
                    math.clamp(HUD.Instance.JoystickLeft.Delta.y, directionRange.x, directionRange.y) * invertVertical,
                    math.clamp(HUD.Instance.JoystickLeft.Delta.x, directionRange.x, directionRange.y) * invertHorizontal,
                    0) * controlSettings.MouseSensitivity,
                Shoot = m_GameInput.Gameplay.Fire.IsPressed(),
            };

#else        
            var gameplayInputActions = m_GameInput.Gameplay;
            var input = new PlayerVehicleInput
            {
                Acceleration = math.clamp(gameplayInputActions.Move.ReadValue<float>(), accelerationRange.x, accelerationRange.y), 
                Roll = gameplayInputActions.Roll.ReadValue<float>(),
                ControlDirection = new float3(
                    math.clamp(-gameplayInputActions.Look.ReadValue<Vector2>().y, directionRange.x, directionRange.y) * invertVertical,
                    math.clamp(gameplayInputActions.Look.ReadValue<Vector2>().x, directionRange.x, directionRange.y) * invertHorizontal,
                    0) * controlSettings.MouseSensitivity,
                Shoot = gameplayInputActions.Fire.IsPressed(),
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Cheat_1 = gameplayInputActions.Cheat_1.IsPressed()
#endif
            };
#endif            
            var job = new PlayerVehicleInputJob {CollectedInput = input};
            job.Schedule();
            
            
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class PlayerVehicleInputSystemSinglePlayer : PlayerVehicleInputSystemBase
    { 
    }
}