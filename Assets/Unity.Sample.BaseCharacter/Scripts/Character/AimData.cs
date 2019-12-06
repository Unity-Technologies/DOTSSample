using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

public class AimData
{
    public struct Data : IComponentData
    {
        public static Data Default => new Data { CameraRightFraction = 1, };

        public float3 EyePosition;
        public float3 CameraAxisPos;
        public float3 AimRefPoint;
//        public float3 CameraAimPoint;
        public float3 CharacterAimPoint;
        public bool CameraAimPointVisible;
        public float CameraRightFraction;
    }

    [UpdateInGroup(typeof(AbilityPreparePhase))]
    [AlwaysSynchronizeSystem]
    class UpdateAimData : JobComponentSystem
    {
        BuildPhysicsWorld buildPhysicsWorld;

        protected override void OnCreate()
        {
            buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();
            buildPhysicsWorld.FinalJobHandle.Complete();

            var physicsWorld = buildPhysicsWorld.PhysicsWorld;
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities
                .ForEach((ref Data aimData, ref PlayerControlled.State playerCtrlState, ref Character.State charState, ref Character.PredictedData charPredicted, ref PredictedGhostComponent predictionData) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictionData))
                    return;
                if (playerCtrlState.IsButtonPressed(UserCommand.Button.CameraSideSwitch))
                {
                    aimData.CameraRightFraction = -aimData.CameraRightFraction;
                }

                // camHoriOffset = Mathf.SmoothDamp(camHoriOffset, cameraSide, ref camHoriOffsetVelocity, 0.1f, 1000f, Time.deltaTime);
                //cameraSettings.position += lookRotation * Vector3.right * 0.425f * camHoriOffset + lookRotation * Vector3.up * -0.0f;  //.45
                //                var eyePos = charPredicted.position + new float3(0,1,0) * charState.eyeHeight;

                //                camHoriOffset = Mathf.SmoothDamp(camHoriOffset, cameraSide, ref camHoriOffsetVelocity, 0.1f, 1000f, Time.deltaTime);
                //                cameraSettings.position += lookRotation * Vector3.right * 0.425f * camHoriOffset + lookRotation * Vector3.up * -0.0f;  //.45

                var staticGeomFilter = CollisionFilter.Default;
                staticGeomFilter.CollidesWith = 1 << 0;
                var cameraOffset = 0.425f;

                // Cal camera pos
                aimData.EyePosition = charPredicted.position + new float3(0, 1, 0) * charState.eyeHeight;
                var offset = math.mul(playerCtrlState.command.LookRotation, new float3(1, 0, 0)) * cameraOffset * aimData.CameraRightFraction;
                aimData.CameraAxisPos = aimData.EyePosition + offset;

                // Find aim point (camera ray intersection with static world)
                var aimDir = playerCtrlState.command.LookDir;
                var cameraAimPoint = aimData.CameraAxisPos + aimDir * 200f;
                aimData.AimRefPoint = aimData.CameraAxisPos + aimDir * 5f;

                var castInput = new RaycastInput
                {
                    Start = aimData.CameraAxisPos,
                    End = cameraAimPoint,
                    Filter = staticGeomFilter
                };
                var closestHit = new RaycastHit();
                if (physicsWorld.CollisionWorld.CastRay(castInput, out closestHit))
                    cameraAimPoint = closestHit.Position;
                //                aimData.CameraAimPoint = cameraAimPoint;
                //                Debug.DrawLine(aimData.CameraAxisPos, aimData.CharacterAimPoint, Color.blue);
                //                DebugDraw.Sphere(aimData.cameraAimPoint, 0.3f, Color.blue);

                // Do visibility check
                var charAimPoint = cameraAimPoint;
                castInput = new RaycastInput
                {
                    Start = aimData.EyePosition,
                    End = cameraAimPoint - aimDir * 0.1f,
                    Filter = staticGeomFilter
                };
                //Debug.DrawLine(castInput.Start, castInput.End, Color.cyan);
                var charAimHit = physicsWorld.CollisionWorld.CastRay(castInput, out closestHit);
                //                if (charAimHit)
                //                    charAimPoint = closestHit.Position;

                aimData.CharacterAimPoint = charAimPoint;
                aimData.CameraAimPointVisible = !charAimHit;

                //                DebugDraw.Sphere(aimData.CharacterAimPoint, 0.3f, aimData.CameraAimPointVisible ? Color.cyan : Color.red);
            }).Run();

            return default;
        }
    }

}



