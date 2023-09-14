using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Entities.SystemAPI;

namespace Unity.Megacity.Audio
{
    /// <summary>
    /// The system draws Normals for all static emitters such as buildings, air conditions and so on.
    /// This is useful to understand where are all the static emitters placed in the different scenes.
    /// This system runs for both Editor (Edit Mode) and PlayMode, and draws red lines from the origin to the Normal Direction.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial struct SoundDebugSystem : ISystem
    {
        static void DrawCross(float3 position)
        {
            const float length = 0.25f;
            Debug.DrawLine(position - new float3(length, 0, 0), position + new float3(length, 0, 0));
            Debug.DrawLine(position - new float3(0, length, 0), position + new float3(0, length, 0));
            Debug.DrawLine(position - new float3(0, 0, length), position + new float3(0, 0, length));
        }

        public void OnCreate(ref SystemState state)
        {
            // Debug system, enable through the systems window
            state.Enabled = false;
            state.RequireForUpdate<AudioBlobRef>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var audioBlobRef = GetSingleton<AudioBlobRef>();
            ref var emitters = ref audioBlobRef.Data.Value.Emitters;
            for (int i = 0; i < emitters.Length; i++)
            {
                DrawCross(emitters[i].Position);
                Debug.DrawRay(emitters[i].Position, emitters[i].Direction, Color.red);
            }
        }
    }
}
