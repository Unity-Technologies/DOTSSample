using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.DebugDisplay;
using Unity.Sample.Core;


public static class PlayerCameraControl
{
    public struct State : IComponentData
    {
        public int isEnabled;
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
    }

    public struct CameraEntity : ISystemStateComponentData
    {
        public Entity Value;
    }


    [DisableAutoCreation]
    public class HandlePlayerCameraControlSpawn : JobComponentSystem
    {
        public HandlePlayerCameraControlSpawn()
        {
            m_cameraPrefab = (GameObject)Resources.Load("Prefabs/PlayerCamera");
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            inputDependencies.Complete();

            Entities
                .WithStructuralChanges()
                .WithNone<CameraEntity>()
                .WithAll<PlayerCameraControl.State>()
                .ForEach((Entity entity) =>
                {
                    var cameraEntity = PrefabAssetManager.CreateEntity(World, m_cameraPrefab);

                    var camera = EntityManager.GetComponentObject<Camera>(cameraEntity);
                    camera.enabled = false;

                    var audioListener = EntityManager.GetComponentObject<AudioListener>(cameraEntity);
                    audioListener.enabled = false;

                    EntityManager.AddComponentData(entity, new CameraEntity
                    {
                        Value = cameraEntity,
                    });
                }).Run();

            return default;
        }

        GameObject m_cameraPrefab;
    }

    [DisableAutoCreation]
    public class UpdatePlayerCameras : JobComponentSystem
    {
        protected override void OnCreate()
        {
            if(Overlay.Managed.instance != null)
            {
                movehist_x = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(128);
                movehist_y = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(128);
                movehist_z = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(128);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .WithoutBurst()
                .ForEach((Entity entity, in PlayerCameraControl.State state, in PlayerCameraControl.CameraEntity cameraEntity) =>
            {
                // We get Camera here as it might be disabled and therefore does not appear in query
                var camera = EntityManager.GetComponentObject<Camera>(cameraEntity.Value);
                var enabled = state.isEnabled;
                var isEnabled = camera.enabled;
                if (enabled == 0)
                {
                    if (isEnabled)
                    {
                        GameApp.CameraStack.PopCamera(camera);
                        camera.enabled = false;

                        var audioListener = EntityManager.GetComponentObject<AudioListener>(cameraEntity.Value);
                        audioListener.enabled = false;

                    }
                    return;
                }

                if (!isEnabled)
                {
                    camera.enabled = true;

                    var audioListener = EntityManager.GetComponentObject<AudioListener>(cameraEntity.Value);
                    audioListener.enabled = true;

                    GameApp.CameraStack.PushCamera(camera);
                }

                camera.fieldOfView = state.fieldOfView;
                if (debugCameraDetach.IntValue == 0)
                {
                // Normal movement
                camera.transform.position = state.position;
                    camera.transform.rotation = state.rotation;
                }
                else if (debugCameraDetach.IntValue == 1)
                {
                // Move char but still camera
            }


                if (debugCameraDetach.ChangeCheck())
                {
                // Block normal input
                GatedInput.SetBlock(GatedInput.Blocker.Debug, debugCameraDetach.IntValue == 2);
                }
                if (debugCameraDetach.IntValue == 2 && !Console.IsOpen())
                {
                    var eu = camera.transform.localEulerAngles;
                    if (eu.x > 180.0f) eu.x -= 360.0f;
                    eu.x = Mathf.Clamp(eu.x, -70.0f, 70.0f);
                    eu += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
                    float invertY = InputSystem.configInvertY.IntValue > 0 ? 1.0f : -1.0f;
                    eu += Time.DeltaTime * (new Vector3(-invertY * Input.GetAxisRaw("RightStickY") * InputSystem.s_JoystickLookSensitivity.y, Input.GetAxisRaw("RightStickX") * InputSystem.s_JoystickLookSensitivity.x, 0));
                    camera.transform.localEulerAngles = eu;
                    m_DetachedMoveSpeed += Input.GetAxisRaw("Mouse ScrollWheel");
                    float verticalMove = (Input.GetKey(KeyCode.R) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.F) ? -1.0f : 0.0f);
                    verticalMove += Input.GetAxisRaw("Trigger");
                    camera.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), verticalMove, Input.GetAxisRaw("Vertical")) * Time.DeltaTime * m_DetachedMoveSpeed);
                }

                if (debugCameraMove.IntValue > 0)
                {
                // Only show for one player
                if (lastUsedFrame < UnityEngine.Time.frameCount)
                    {
                        lastUsedFrame = UnityEngine.Time.frameCount;

                        var rot = camera.transform.localEulerAngles;
                        movehist_x.AddValue(rot.x % 90.0f);
                        movehist_y.AddValue(rot.y % 90.0f);
                        movehist_z.AddValue(rot.z % 90.0f);

                        using (var graph = Overlay.Managed.instance.m_Unmanaged.m_GraphReservations.Reserve(3))
                        {
                            graph.AddGraph(4, 4, 10, 5, new Overlay.Graph.Sample
                            {
                                data = movehist_x.GetData(),
                                xMax = UnityEngine.Time.frameCount,
                                xMin = UnityEngine.Time.frameCount - 10 * Overlay.Text.kCellPixelsWide,
                                color = Overlay.Color.Red,
                                yMin = 0,
                                yMax = 10.0f
                            });
                            graph.AddGraph(4, 12, 10, 5, new Overlay.Graph.Sample
                            {
                                data = movehist_y.GetData(),
                                xMax = UnityEngine.Time.frameCount,
                                xMin = UnityEngine.Time.frameCount - 10 * Overlay.Text.kCellPixelsWide,
                                color = Overlay.Color.Green,
                                yMin = 0,
                                yMax = 10.0f

                            });
                            graph.AddGraph(4, 20, 10, 5, new Overlay.Graph.Sample
                            {
                                data = movehist_z.GetData(),
                                xMax = UnityEngine.Time.frameCount,
                                xMin = UnityEngine.Time.frameCount - 10 * Overlay.Text.kCellPixelsWide,
                                color = Overlay.Color.Blue,
                                yMin = 0,
                                yMax = 10.0f
                            });
                        }
                    }
                }
            }).Run();

            return default;
        }

        // Debugging graphs to show player movement in 3 axis
        Overlay.Graph.Data.Reservation movehist_x;
        Overlay.Graph.Data.Reservation movehist_y;
        Overlay.Graph.Data.Reservation movehist_z;
        static float lastUsedFrame;

        [ConfigVar(Name = "debug.cameramove", Description = "Show graphs of first person camera rotation", DefaultValue = "0")]
        public static ConfigVar debugCameraMove;
        [ConfigVar(Name = "debug.cameradetach", Description = "Detach player camera from player", DefaultValue = "0")]
        public static ConfigVar debugCameraDetach;

        float m_DetachedMoveSpeed = 4.0f;
    }

}

