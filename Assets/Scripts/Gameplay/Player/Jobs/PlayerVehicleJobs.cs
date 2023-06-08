using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Set of jobs to control the player car movement and breaking
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(Simulate))]
    internal partial struct VehicleBankingJob : IJobEntity
    {
        public void Execute(
            ref VehicleRoll vehicleRoll,
            in VehicleVolatilityCurves curves,
            in LocalTransform worldTransform,
            in PhysicsVelocity velocity,
            in PlayerVehicleSettings vehicleSettings,
            in PlayerVehicleInput controlInput,
            ref VehicleHealth health)

        {
            if (health.Value <= 0)
                return;

            vehicleRoll.BankAmount = CalculateBanking(ref curves.BankVolatilityCurve.Value, in worldTransform,
                in velocity,
                in vehicleSettings);
            RollVehicle(in vehicleSettings, in controlInput, ref vehicleRoll);
        }

        private float CalculateBanking(
            ref AnimationCurveBlob bankVolatilityCurve,
            in LocalTransform worldTransform,
            in PhysicsVelocity velocity,
            in PlayerVehicleSettings vehicleSettings)
        {
            var vehicleVelocityLocal = math.mul(math.inverse(worldTransform.Rotation), velocity.Linear);
            var momentumRaw = vehicleVelocityLocal.x * vehicleSettings.InvMaxVelocity * vehicleSettings.BankVolatility;
            const float threshold = 0.05f;
            var momentum = math.clamp(momentumRaw, -1 + threshold, 1 - threshold);
            var bankAmount = vehicleSettings.MaxBankAngle * math.sign(momentum) * bankVolatilityCurve.Evaluate(math.abs(momentum));
            return bankAmount;
        }

        private void RollVehicle(
            in PlayerVehicleSettings vehicleSettings,
            in PlayerVehicleInput input,
            ref VehicleRoll vehicleRoll)
        {
            if (input.RightRoll > 0 || input.LeftRoll > 0)
            {
                if (vehicleRoll.ManualRollValue == 0)
                {
                    vehicleRoll.ManualRollValue = vehicleRoll.BankAmount;
                }

                vehicleRoll.ManualRollSpeed += input.RightRoll > 0
                    ? -vehicleSettings.ManualRollAcceleration
                    : vehicleSettings.ManualRollAcceleration;

                if (math.abs(vehicleRoll.ManualRollSpeed) > vehicleSettings.ManualRollMaxSpeed)
                {
                    vehicleRoll.ManualRollSpeed =
                        vehicleSettings.ManualRollMaxSpeed * math.sign(vehicleRoll.ManualRollSpeed);
                }

                vehicleRoll.ManualRollValue += vehicleRoll.ManualRollSpeed;
                vehicleRoll.ManualRollValue %= 360;
            }
            else if (math.abs(vehicleRoll.ManualRollValue) > 0)
            {
                var bA = (vehicleRoll.BankAmount + 360) % 360;
                var mR = (vehicleRoll.ManualRollValue + 360) % 360;

                var outerRot = bA > mR ? -mR - (360 - bA) : bA + (360 - mR);
                var innerRot = bA - mR;

                var sD = vehicleRoll.ManualRollSpeed * vehicleRoll.ManualRollSpeed /
                         (2 * vehicleSettings.ManualRollAcceleration);

                // stopping distance if going wrong direction
                if (innerRot * vehicleRoll.ManualRollValue < 0)
                {
                    innerRot += math.sign(innerRot) * sD;
                }

                if (outerRot * vehicleRoll.ManualRollValue < 0)
                {
                    outerRot += math.sign(outerRot) * sD;
                }

                // overshoot distance if sD > ((ManualRollSpeed * ManualRollSpeed) / (2 * ManualRollAcceleration) > distanceToTarget)
                if (sD > math.abs(innerRot))
                {
                    innerRot += math.sign(innerRot) * (sD - math.abs(innerRot));
                }

                if (sD > math.abs(outerRot))
                {
                    outerRot += math.sign(outerRot) * (sD - math.abs(outerRot));
                }

                var target = math.abs(outerRot) < math.abs(innerRot)
                    ? bA > mR ? -mR - (360 - bA) : bA + (360 - mR)
                    : bA - mR;

                if (math.abs(target) < math.abs(vehicleRoll.ManualRollSpeed) &&
                    math.abs(vehicleRoll.ManualRollSpeed) <= 1)
                {
                    vehicleRoll.ManualRollValue = 0;
                    vehicleRoll.ManualRollSpeed = 0;
                    return;
                }

                if (vehicleRoll.ManualRollSpeed * vehicleRoll.ManualRollSpeed /
                    (2 * vehicleSettings.ManualRollAcceleration) > math.abs(target)) // s = (v^2-u^2) / 2a
                {
                    if (vehicleRoll.ManualRollSpeed > 0)
                    {
                        vehicleRoll.ManualRollSpeed -= vehicleSettings.ManualRollAcceleration;
                    }
                    else
                    {
                        vehicleRoll.ManualRollSpeed += vehicleSettings.ManualRollAcceleration;
                    }
                }
                else
                {
                    vehicleRoll.ManualRollSpeed += target < 0
                        ? -vehicleSettings.ManualRollAcceleration
                        : vehicleSettings.ManualRollAcceleration;

                    if (math.abs(vehicleRoll.ManualRollSpeed) > vehicleSettings.ManualRollMaxSpeed)
                    {
                        vehicleRoll.ManualRollSpeed =
                            vehicleSettings.ManualRollMaxSpeed * math.sign(vehicleRoll.ManualRollSpeed);
                    }
                }

                vehicleRoll.ManualRollValue += vehicleRoll.ManualRollSpeed;
                vehicleRoll.ManualRollValue %= 360;
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    internal partial struct VehicleBreakingPseudoPhysicsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in LocalTransform worldTransform,
            in PhysicsVelocity velocity,
            in VehicleVolatilityCurves curves,
            in VehicleThrust vehicleThrust,
            in PlayerVehicleSettings vehicleSettings,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref VehicleBraking vehicleBraking)
        {
            if (controlInput.Brake > 0 && vehicleThrust.Thrust >= 0)
            {
                ApplyBreakingPseudoPhysics(
                    in velocity, in worldTransform, in vehicleSettings, DeltaTime, ref cameraSettings,
                    ref curves.PitchVolatilityCurve.Value, ref curves.YawVolatilityCurve.Value,
                    ref vehicleBraking);
            }
            else if (math.abs(vehicleBraking.PitchPseudoBraking) > 0.01f ||
                     math.abs(vehicleBraking.YawBreakRotation) > 0.01f ||
                     math.abs(cameraSettings.FollowCameraZOffset - cameraSettings.FollowCameraZFollow) > 0.01f)
            {
                RevertBreakingPseudoPhysics(in vehicleSettings, DeltaTime, ref cameraSettings, ref vehicleBraking);
            }
        }

        void RevertBreakingPseudoPhysics(
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref VehicleBraking vehicleBraking)
        {
            var noYaw = vehicleBraking.YawBreakRotation > 180f ? 360f : 0f;
            vehicleBraking.YawBreakRotation =
                math.lerp(vehicleBraking.YawBreakRotation, noYaw, deltaTime * vehicleSettings.YawKickBack);

            vehicleBraking.PitchPseudoBraking =
                math.lerp(vehicleBraking.PitchPseudoBraking, 0f, deltaTime * vehicleSettings.PitchForce);

            if (math.abs(cameraSettings.FollowCameraZOffset - cameraSettings.FollowCameraZFollow) > 0.01f)
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow, deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }
        }

        private void ApplyBreakingPseudoPhysics(
            in PhysicsVelocity velocity,
            in LocalTransform worldTransform,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref PlayerVehicleCameraSettings cameraSettings,
            ref AnimationCurveBlob pitchVolatilityCurve,
            ref AnimationCurveBlob yawVolatilityCurve,
            ref VehicleBraking vehicleBraking)
        {
            var vehicleVelocityLocal = math.mul(math.inverse(worldTransform.Rotation), velocity.Linear);
            vehicleVelocityLocal.y = 0;

            var localSpeed = math.length(vehicleVelocityLocal);
            var relSpeed = math.clamp(localSpeed * vehicleSettings.InvMaxVelocity, 0, 1);
            var pitchAmount = vehicleSettings.MaxPitchAngle * pitchVolatilityCurve.Evaluate(math.abs(relSpeed));
            var lerpTime = pitchAmount > vehicleBraking.PitchPseudoBraking
                ? deltaTime * vehicleSettings.PitchForce * (localSpeed * vehicleSettings.InvMaxVelocity)
                : deltaTime * vehicleSettings.PitchForce;
            vehicleBraking.PitchPseudoBraking = math.lerp(vehicleBraking.PitchPseudoBraking, pitchAmount, lerpTime);

            if (pitchAmount > vehicleBraking.PitchPseudoBraking)
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow + cameraSettings.FollowCameraZBreakZoom,
                    localSpeed * vehicleSettings.InvMaxVelocity * deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }
            else
            {
                cameraSettings.FollowCameraZOffset = math.lerp(cameraSettings.FollowCameraZOffset,
                    cameraSettings.FollowCameraZFollow, deltaTime * cameraSettings.FollowCameraZBreakSpeed);
            }

            var momentum = math.clamp(vehicleVelocityLocal.x * vehicleSettings.InvMaxVelocity, -1, 1);
            var targetYawAmount = vehicleSettings.MaxYawAngle * -math.sign(momentum) *
                                  yawVolatilityCurve.Evaluate(math.abs(momentum));
            vehicleBraking.YawBreakRotation = math.lerp(vehicleBraking.YawBreakRotation, targetYawAmount, deltaTime);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    internal partial struct ThrustJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            ref VehicleThrust vehicleThrust,
            ref PhysicsDamping damping)
        {
            CalculateThrust(in controlInput, in vehicleSettings, DeltaTime, ref vehicleThrust, ref damping);
            CalculateThrustDepreciation(in controlInput, in vehicleSettings, DeltaTime, ref vehicleThrust);
        }

        private void CalculateThrust(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref VehicleThrust vehicleThrust,
            ref PhysicsDamping damping)
        {
            vehicleThrust.Thrust += vehicleSettings.Acceleration * deltaTime * controlInput.Acceleration;

            if (controlInput.Brake > 0)
            {
                damping.Linear = vehicleSettings.DragBreakForce * vehicleSettings.Damping.Linear;
            }
            else
            {
                damping.Linear = vehicleSettings.Damping.Linear;
            }

            var maxThrust = vehicleSettings.MaxSpeed * controlInput.Acceleration;
            if (math.abs(vehicleThrust.Thrust) > math.abs(maxThrust))
            {
                vehicleThrust.Thrust = maxThrust;
            }
        }

        private void CalculateThrustDepreciation(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            float deltaTime,
            ref VehicleThrust vehicleThrust)
        {
            if (math.abs(controlInput.Acceleration) > 0.1f)
                return;

            switch (vehicleThrust.Thrust)
            {
                case > 0:
                    vehicleThrust.Thrust -= vehicleSettings.Deceleration * deltaTime;
                    break;
                case < 0:
                    vehicleThrust.Thrust += vehicleSettings.Deceleration * deltaTime;
                    break;
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    internal partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        public NetworkTick Tick;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in VehicleThrust vehicleThrust,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            ref VehicleHealth health,
            ref PhysicsVelocity velocity,
            ref PhysicsGravityFactor grav,
            ref PhysicsDamping damping,
            ref LocalTransform localTransform)
        {
            // Let control continue for half a sec to improve prediction
            if (health.Value == 0 && Tick.TicksSince(health.AliveStateChangeTick) > 30)
            {
                // 5 sec
                if (Tick.TicksSince(health.AliveStateChangeTick) > 300)
                {
                    health.Value = 100;
                    health.AliveStateChangeTick = NetworkTick.Invalid;
                    health.ReviveStateChangeTick = Tick;
                }
                else
                {
                    grav.Value = 1;
                    damping.Linear = 0;
                    return;
                }
            }


            var xRotation = quaternion.AxisAngle(math.up(), controlInput.ControlDirection.y);
            var yRotation = quaternion.AxisAngle(math.right(), controlInput.ControlDirection.x);
            var newRotation = math.mul(math.mul(localTransform.Rotation, xRotation), yRotation);
            localTransform.Rotation = math.slerp(localTransform.Rotation, newRotation, DeltaTime * vehicleSettings.SteeringSpeed);

            velocity.Linear += localToWorld.Forward * vehicleThrust.Thrust * DeltaTime * mass.InverseMass;

            if (math.lengthsq(velocity.Linear) > vehicleSettings.MaxSpeed * vehicleSettings.MaxSpeed)
            {
                velocity.Linear = math.normalize(velocity.Linear) * vehicleSettings.MaxSpeed;
            }

            grav.Value = 0;
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    internal partial struct AutoLevelJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            ref PhysicsVelocity velocity)
        {
            AutoLevelCar(in controlInput, in vehicleSettings, in localToWorld, in mass, DeltaTime, ref velocity);
        }

        private void AutoLevelCar(
            in PlayerVehicleInput controlInput,
            in PlayerVehicleSettings vehicleSettings,
            in LocalToWorld localToWorld,
            in PhysicsMass mass,
            float deltaTime,
            ref PhysicsVelocity velocity)
        {
            if (controlInput.RightRoll > 0 || controlInput.LeftRoll > 0)
            {
                return;
            }

            velocity.Angular += math.forward() * math.dot(localToWorld.Right, math.up()) *
                                -vehicleSettings.RollAutoLevelVelocity * deltaTime * mass.InverseInertia;
        }
    }
}