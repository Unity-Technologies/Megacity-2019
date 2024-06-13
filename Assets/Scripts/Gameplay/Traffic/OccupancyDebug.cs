using UnityEditor;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Draws gizmos to debug vehicle occupancy.
    /// </summary>
    public class OccupancyDebug : MonoBehaviour
    {
#if UNITY_EDITOR
        public static NativeArray<RoadSection> roadSections;
        public static NativeArray<Occupation> queueSlots;

        void OnDrawGizmos()
        {
            for (var r = 0; r < roadSections.Length; ++r)
            {
                var rs = roadSections[r];

                if (Camera.current != null && math.distance(rs.p1, Camera.current.transform.position) < 200.0f)
                {
                    var step = 1.0f / rs.occupationLimit;

                    for (int x = 0; x < 3; ++x)
                    {
                        for (int y = 1; y < 2; ++y)
                        {
                            for (int i = 0; i < rs.occupationLimit; ++i)
                            {
                                var o = queueSlots[r * Constants.RoadIndexMultiplier + i * 9 + x + y * 3];
                                Color c = o.occupied != 0 ? Color.red : Color.green;
                                var dir = rs.p2 - rs.p1;
                                var p1 = rs.p1 + dir * (float)i / rs.occupationLimit;

                                var laneWidth = rs.width / 3.0f;
                                var laneHeight = rs.height / 3.0f;

                                var right = math.cross(math.normalize(dir), new float3(0.0f, 1.0f, 0.0f));
                                //var up = math.normalize(math.cross(right, dir));

                                p1 += (-(x-1)-0.5f) * laneWidth * right;
                                //p1 += (y-1) * laneHeight * up;

                                var labelPos = p1 + 0.5f * laneWidth + math.normalize(dir) * (step / 2.0f);

                                Handles.Label(labelPos, $"Speed = {o.speed}\nRoad = {r}\n OccIdx = {i}\n LnIdx = {x+y*3}\n Vid = {o.occupied}");

                                var p2 = p1 + laneWidth * right;
                                var p3 = p1 + laneWidth * right + dir * step;
                                var p4 = p1 + dir * step;

                                Debug.DrawLine(p1, p2, c);
                                Debug.DrawLine(p2, p3, c);
                                Debug.DrawLine(p3, p4, c);
                                Debug.DrawLine(p4, p1, c);
                                Debug.DrawLine(p1, p3, c);
                                Debug.DrawLine(p2, p4, c);


                            }
                        }
                    }
                }
            }
        }
#endif
    }
}
