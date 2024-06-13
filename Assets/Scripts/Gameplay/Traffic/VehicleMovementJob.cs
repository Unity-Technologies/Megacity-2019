using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Unity.Megacity.Traffic
{
    /// <summary>
    /// Moves the NPC vehicles according to the target position
    /// </summary>
    [BurstCompile]
    public partial struct VehicleMovementJob : IJobEntity
    {
        public float TimeStep;
        [ReadOnly]
        public NativeParallelMultiHashMap<int, VehicleCell> Cells;

        private float3 CellAvoidance(int key, float3 pos, float3 velocity, float radius)
        {
            VehicleCell cell;
            NativeParallelMultiHashMapIterator<int> iter;

            if (length(velocity) < 0.001f || !Cells.TryGetFirstValue(key, out cell, out iter))
            {
                return default;
            }

            var vnorm = normalize(velocity);

            var right = cross(vnorm, float3(0.0f, 1.0f, 0.0f));
            var up = normalize(cross(right, vnorm));

            var ownAnticipated = pos + velocity * TimeStep;

            var maxScanRangeMeters = 64.0f;

            var closestDist = float.MaxValue;
            var xa = 0.0f;
            var ya = 0.0f;
            var mag = 0.0f;

            do
            {
                // For the vehicle in the cell, calculate its anticipated position
                var anticipated = cell.Position + cell.Velocity * TimeStep;

                var currDelta = pos - cell.Position;
                var delta = anticipated - ownAnticipated;

                // Don't avoid self
                if (lengthsq(currDelta) < 0.3f)
                {
                    continue;
                }

                var dz = dot(delta, vnorm);

                // Ignore this vehicle if it's behind or too far away
                if (dz < 0.0f || dz > maxScanRangeMeters)
                {
                    continue;
                }

                var lsqDelta = lengthsq(delta);

                // Only update if the distance between anticipated positions is less than the current closest and radii
                if (lsqDelta < closestDist && lsqDelta < (cell.Radius + radius) * (cell.Radius + radius))
                {
                    var dx = dot(delta, right);
                    var dy = dot(delta, up);

                    closestDist = lsqDelta;

                    xa = dx;
                    ya = dy;
                    mag = cell.Radius + radius;
                }
            } while (Cells.TryGetNextValue(out cell, ref iter));

            var result = default(float3);

            if (xa < 0.0f)
            {
                result += (mag + xa) * right;
            }
            else if (xa > 0.0f)
            {
                result -= (mag - xa) * right;
            }

            if (ya < 0.0f)
            {
                result += (mag + ya) * up;
            }
            else if (ya > 0.0f)
            {
                result -= (mag - ya) * up;
            }
            result *= maxScanRangeMeters - sqrt(closestDist) / maxScanRangeMeters;
            return result;
        }

        private float3 Avoidance(float3 pos, float3 velocity, float radius)
        {
            var cellSize = VehicleHashJob.kCellSize;
            var steering = default(float3);

            var hash = GridHash.Hash(pos, cellSize);

            // Check to see if the vehicle will be in another cell (in a few frames)
            var projectedHash = GridHash.Hash(pos + velocity * 2.0f, cellSize);

            steering += CellAvoidance(hash, pos, velocity, radius);

            // The vehicle is projected to be in the same cell, so just return steering
            if (hash == projectedHash)
            {
                return steering;
            }

            // The vehicle is projected to be in another, so calculate Avoidance on things in that cell as well
            steering += CellAvoidance(projectedHash, pos, velocity, radius);
            return steering;
        }

        private float3 Seek(float3 target, float3 curr, float3 velocity, float speedMult)
        {
            var targetOffset = target - curr;
            var distance = length(targetOffset);
            var rampedSpeed = Constants.MaxSpeedMetersPerSecond * speedMult *
                              (distance / Constants.SlowingDistanceMeters);
            var clippedSpeed = min(rampedSpeed, Constants.MaxSpeedMetersPerSecond * speedMult);

            // Compute velocity based on target position
            var desiredVelocity = targetOffset * (clippedSpeed / distance);

            var steering = desiredVelocity - velocity;

            if (lengthsq(steering) < 0.5f)
            {
                return default;
            }

            return steering;
        }

        public void Execute(in VehicleTargetPosition targetPos, ref VehiclePhysicsState state)
        {
            // 2 steering vectors, seeking the road and avoidance
            var seekSteering = Seek(targetPos.IdealPosition, state.Position, state.Velocity, state.SpeedMult);
            var avoidSteering = Avoidance(state.Position, state.Velocity, Constants.AvoidanceRadius);

            // If there is any avoidSteering value, select that vector otherwise just seek the curve of the road
            var steering = lengthsq(avoidSteering) > 0.01f ? avoidSteering : seekSteering;

            var maxSpeed = Constants.MaxSpeedMetersPerSecond * state.SpeedMult;

            // Update the VehiclePhysicsState
            var targetHeading = normalizesafe(steering);
            state.Heading = state.Heading + targetHeading * TimeStep;
            state.Heading = normalizesafe(state.Heading);
            state.Velocity = state.Heading * targetPos.IdealSpeed;
            state.Position = state.Position + state.Velocity * TimeStep;

            if (dot(targetHeading, state.Velocity) > 0)
            {
                var bankAmount = 0.0f;
                var kBankMaxRadians = PI / 16.0f * state.SpeedMult; // 11.25 degrees * speed multiplier

                var speedRatio = 100.0f * kBankMaxRadians / maxSpeed;

                bankAmount = -dot(targetHeading.zx * float2(-1.0f, 1.0f), state.Velocity.xz);
                bankAmount = clamp(bankAmount * speedRatio, -kBankMaxRadians, kBankMaxRadians);

                var bankDifference = state.BankRadians - bankAmount;

                state.BankRadians = bankAmount > .001f || bankAmount < -.001f
                    ? state.BankRadians - bankDifference * TimeStep
                    : 0.0f;
            }
        }
    }
}
