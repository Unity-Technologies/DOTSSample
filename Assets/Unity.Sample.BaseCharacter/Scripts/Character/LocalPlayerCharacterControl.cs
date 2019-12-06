using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Sample.Core;
using UnityEditor;




public partial class LocalPlayerCharacterControl : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new State());
        dstManager.AddBuffer<AbilityUIElement>(entity);
        dstManager.AddBuffer<PresentationElement>(entity);
    }
}

public partial class LocalPlayerCharacterControl
{
    [ConfigVar(Name = "char.showhistory", DefaultValue = "0", Description = "Show last char loco states")]
    public static ConfigVar ShowHistory;


    public struct State : IComponentData
    {
        public Entity lastRegisteredControlledEntity;

        public Entity healthUI;

        public int lastDamageInflictedTick;
        public int lastDamageReceivedTick;
    }

    public struct AbilityUIElement : IBufferElementData
    {
        public Entity entity;
    }

    public struct PresentationElement : IBufferElementData
    {
        public Entity entity;
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(ClientLateUpdateGroup))]
    [AlwaysSynchronizeSystem]
    public class UpdateCharacterCamera : JobComponentSystem
    {
        private const float k_default3PDist = 3f; // 1.6
        private float camDist3P = k_default3PDist;

        bool aimZoom = false;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var controlledEntityVar = controlledEntity;
            var camDist3PVar = camDist3P;
            var aimZoomVar = aimZoom;
            var configFovValue = 60; // Game.configFov.FloatValue;
            var mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);
            var characterStateFromEntity = GetComponentDataFromEntity<Character.State>(true);
            var abilityOwnerAimDataFromEntity = GetComponentDataFromEntity<AimData.Data>(true);
            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(true);
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);

            Entities
                .ForEach((Entity entity, ref LocalPlayer localPlayer, ref PlayerCameraControl.State cameraSettings, ref State characterControl) =>
            {
                if (localPlayer.controlledEntity == Entity.Null || !characterPredictedDataFromEntity.HasComponent(localPlayer.controlledEntity))
                {
                    controlledEntityVar = Entity.Null;
                    return;
                }

                var charState = characterStateFromEntity[localPlayer.controlledEntity];

                var aimData = abilityOwnerAimDataFromEntity[localPlayer.controlledEntity];

                var animState = characterInterpolatedDataFromEntity[localPlayer.controlledEntity];

                // Check if this is first time update is called with this controlled entity
                var characterChanged = localPlayer.controlledEntity != controlledEntityVar;
                if (characterChanged)
                {
                    controlledEntityVar = localPlayer.controlledEntity;
                }

                // Update character visibility
                var camProfile = CameraProfile.ThirdPerson; // charPredictedState.cameraProfile;

                // Update camera settings
                var userCommand = playerControlledStateFromEntity[localPlayer.controlledEntity];
                var lookRotation = userCommand.command.LookRotation;

                cameraSettings.isEnabled = 1;

                // Update FOV
                if (characterChanged)
                    cameraSettings.fieldOfView = configFovValue;
                //

                var targetFOV = animState.sprinting ? charState.sprintCameraSettings.FOVFactor * configFovValue : configFovValue;
                var speed = targetFOV > cameraSettings.fieldOfView ? charState.sprintCameraSettings.FOVInceraetSpeed : charState.sprintCameraSettings.FOVDecreaseSpeed;
                cameraSettings.fieldOfView = Mathf.MoveTowards(cameraSettings.fieldOfView, targetFOV, speed);

                switch (camProfile)
                {
                    case CameraProfile.FirstPerson:
                        {
                            //                        var eyePos = charPredictedState.position + Vector3.up * charState.eyeHeight;
                            //
                            //                        // Set camera position and adjust 1P char. As 1P char is scaled down we need to "up-scale" camera
                            //                        // animation to world space. We dont want to upscale cam transform relative to 1PChar so we adjust
                            //                        // position accordingly
                            //                        var camLocalOffset = character1P.cameraTransform.position - character1P.transform.position;
                            //                        var cameraRotationOffset = Quaternion.Inverse(character1P.transform.rotation) * character1P.cameraTransform.rotation;
                            //                        var camWorldOffset = camLocalOffset / character1P.transform.localScale.x;
                            //                        var camWorldPos = eyePos + camWorldOffset;
                            //                        var charWorldPos = camWorldPos - camLocalOffset;
                            //
                            //                        cameraSettings.position = camWorldPos;
                            //                        cameraSettings.rotation = userCommand.command.lookRotation * cameraRotationOffset;
                            //
                            //                        var char1PPresentation = EntityManager.GetComponentObject<CharacterPresentationSetup>(characterControl.firstPerson.char1P);
                            //                        char1PPresentation.transform.position = charWorldPos;
                            //                        char1PPresentation.transform.rotation = userCommand.command.lookRotation;

                            break;
                        }

                    case CameraProfile.Shoulder:
                    case CameraProfile.ThirdPerson:
                        {
#if UNITY_EDITOR
                            if (mouseScrollWheel > 0)
                            {
                                camDist3PVar -= 0.2f;
                            }
                            if (mouseScrollWheel < 0)
                            {
                                camDist3PVar += 0.2f;
                            }


                            aimZoomVar = userCommand.command.buttons.IsSet(UserCommand.Button.Ability2);

#endif

                            cameraSettings.position = aimData.CameraAxisPos;

                            cameraSettings.rotation = lookRotation;

                            // Simple offset of camera for better 3rd person view. This is only for animation debug atm
                            var viewDir = cameraSettings.rotation * Vector3.forward;

                            if (aimZoomVar)
                            {
                                cameraSettings.position += -camDist3PVar * 0.5f * viewDir;
                            }
                            else
                            {
                                var zoom = 1f;
                                if (userCommand.command.lookPitch > 90f)
                                {
                                    zoom = math.remap(90f, 180f, 1f, 0.1f, userCommand.command.lookPitch);
                                }

                                cameraSettings.position += -camDist3PVar * zoom * viewDir;
                            }


                            break;
                        }
                }


                //                // TODO (mogensh) find better place to put this.
                //                if (LocalPlayerCharacterControl.ShowHistory.IntValue > 0)
                //                {
                //                    charState.ShowHistory(time.tick);
                //                }
            }).Run();

            controlledEntity = controlledEntityVar;
            camDist3P = camDist3PVar;
            aimZoom = aimZoomVar;

            return default;
        }

        Entity controlledEntity;
    }

}



