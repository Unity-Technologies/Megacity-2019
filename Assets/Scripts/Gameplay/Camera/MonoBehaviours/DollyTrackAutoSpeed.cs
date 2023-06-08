using UnityEngine;
using Cinemachine;
using Unity.Mathematics;

namespace Unity.MegaCity.CameraManagement
{
    /// <summary>
    /// Move the dolly cart on the defined track to allow the player car to drive in autopilot mode
    /// </summary>
    public class DollyTrackAutoSpeed : MonoBehaviour
    {
        public float m_BaseSpeed = 1.0f;
        public float m_MinSpeed = 0.2f;
        public float m_Acceleration = 0.01f;

        public float m_TurnAcceleration = 0.001f;
        public float m_TurnLookAheadPathDistance = 1.0f;
        public float m_TurnBreakForce = 0.1f;
        public int m_TurnSamplePositionCount = 10;
        [Range(0, 1)] public float m_MaxTurnSpeedModifier = 0.25f;
        [Range(0, 1)] private float m_TargetTurnSpeed = 1.0f;
        private float m_CurrentTurnSpeed = 1.0f;

        public float m_ClimbLookAheadPathDistance = 1.0f;
        public float m_ClimbModifier = 1.0f;
        [Range(0, 1)] private float m_ClimbDeceleration = 0.0f;

        public float m_FallAcceleration = 0.01f;
        public float m_FallDecelleration = 0.4f;
        public float m_MaxFallSpeedModifier = 2.0f;
        private float m_TargetFallSpeed = 1.0f;
        private float m_CurrentFallSpeed = 1.0f;

        private static float3 c_FORWARD = new float3(0, 0, 1);
        private static float3 c_XZPLANE = new float3(1, 0, 1);

        public CinemachineDollyCart m_DollyCart = null;
        private CinemachineSmoothPath m_Path = null;
        private bool m_StartAcceleration = true;

        public bool m_EnableDebugText = false;

        private void Awake()
        {
            m_Path = GetComponent<CinemachineSmoothPath>();
            m_DollyCart.m_Speed = 0;
        }

        private void StartAcceleration()
        {
            if (m_DollyCart.m_Speed < m_BaseSpeed)
                m_DollyCart.m_Speed += m_Acceleration;
            else
                m_StartAcceleration = false;
        }

        private void TurnSpeedModifier()
        {
            float3 startPosition = m_Path.EvaluatePositionAtUnit(m_DollyCart.m_Position, m_DollyCart.m_PositionUnits);
            float3 startTangent =
                math.mul(m_Path.EvaluateOrientationAtUnit(m_DollyCart.m_Position, m_DollyCart.m_PositionUnits),
                    c_XZPLANE);
            float3 prevTangent = startTangent;
            float3 nextTangent = float3.zero;

            float dotAverage = 0.0f;
            int count = 0;
            bool turnRight = false;

            for (int i = 1; i <= m_TurnSamplePositionCount; ++i)
            {
                nextTangent =
                    math.mul(
                        m_Path.EvaluateOrientationAtUnit(m_DollyCart.m_Position + (i * m_TurnLookAheadPathDistance),
                            m_DollyCart.m_PositionUnits), c_XZPLANE);

                if (i == 1)
                    turnRight = math.cross(startTangent, nextTangent).y < 0 ? true : false;

                if (math.cross(prevTangent, nextTangent).y > 0 && turnRight)
                    break;

                dotAverage += math.dot(math.normalizesafe(prevTangent), math.normalizesafe(nextTangent)) *
                              (1 - ((1 / m_TurnSamplePositionCount) * i));
                ++count;
            }

            dotAverage /= count;
            dotAverage = math.clamp(dotAverage, 0, 1);

            m_TargetTurnSpeed = math.clamp(1 - ((1 - dotAverage) * m_MaxTurnSpeedModifier), 0, 1);

            if (math.distance(m_CurrentTurnSpeed, m_TargetTurnSpeed) < 0.01f)
                m_TargetTurnSpeed = m_CurrentTurnSpeed;

            if (m_CurrentTurnSpeed != m_TargetTurnSpeed)
                m_CurrentTurnSpeed += (m_CurrentTurnSpeed < m_TargetTurnSpeed ? m_TurnAcceleration : -m_TurnBreakForce);

            m_CurrentTurnSpeed = math.clamp(m_CurrentTurnSpeed, 0, 1);
        }

        private void FallSpeedModifier()
        {
            float3 currDollyForward =
                math.mul(m_Path.EvaluateOrientationAtUnit(m_DollyCart.m_Position, m_DollyCart.m_PositionUnits),
                    c_FORWARD);

            if (currDollyForward.y < 0)
                m_TargetFallSpeed = math.abs(currDollyForward.y) * m_MaxFallSpeedModifier;

            m_TargetFallSpeed = math.clamp(m_TargetFallSpeed, 1, m_MaxFallSpeedModifier);

            if (m_CurrentFallSpeed != m_TargetFallSpeed)
                m_CurrentFallSpeed += (m_CurrentFallSpeed < m_TargetFallSpeed
                    ? m_FallAcceleration
                    : -m_FallDecelleration);

            m_CurrentFallSpeed = math.clamp(m_CurrentFallSpeed, 1, m_MaxFallSpeedModifier);
        }

        private void ClimbSpeedModifier()
        {
            float3 currDollyForward =
                math.mul(m_Path.EvaluateOrientationAtUnit(m_DollyCart.m_Position, m_DollyCart.m_PositionUnits),
                    c_FORWARD);
            float3 nextDollyForward =
                math.mul(
                    m_Path.EvaluateOrientationAtUnit(m_DollyCart.m_Position + m_ClimbLookAheadPathDistance,
                        m_DollyCart.m_PositionUnits), c_FORWARD);
            float dollyForwardYDiff = math.distance(currDollyForward.y, nextDollyForward.y);
            m_ClimbDeceleration = 1.0f;

            if (currDollyForward.y >= 0 && currDollyForward.y <= nextDollyForward.y)
            {
                m_ClimbDeceleration = 1 - (dollyForwardYDiff * m_ClimbModifier);
            }
        }

        private void FixedUpdate()
        {
            TurnSpeedModifier();
            FallSpeedModifier();
            ClimbSpeedModifier();

            float targetSpeed = m_BaseSpeed * m_TargetFallSpeed * m_ClimbDeceleration * m_CurrentTurnSpeed;

            if (targetSpeed < m_MinSpeed)
                targetSpeed = m_MinSpeed;

            if (m_EnableDebugText)
            {
                Debug.Log("targetSpeed: " + targetSpeed);
                Debug.Log("m_TargetFallSpeed: " + m_TargetFallSpeed);
                Debug.Log("m_ClimbDeceleration: " + m_ClimbDeceleration);
                Debug.Log("m_CurrentTurnSpeed: " + m_CurrentTurnSpeed);
            }

            if (m_StartAcceleration)
            {
                if (m_DollyCart.m_Speed < targetSpeed)
                    m_DollyCart.m_Speed += m_Acceleration;
                else
                    m_StartAcceleration = false;
            }
            else
            {
                m_DollyCart.m_Speed = targetSpeed;
            }
        }
    }
}
