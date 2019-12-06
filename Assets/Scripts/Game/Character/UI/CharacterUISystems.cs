using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;


[DisableAutoCreation]
[AlwaysUpdateSystem]
public class UpdateCharacterUI : ComponentSystem
{
    private IngameHUD m_Hud;
    private NamePlate m_NamePlate;
    private List<NamePlate> m_NamePlates = new List<NamePlate>();

    public struct NamePlateEntityHolder : ISystemStateComponentData
    {
        public int namePlateIdx;
    }

    protected override void OnCreate()
    {
        m_Hud = Resources.Load<IngameHUD>("Prefabs/CharacterHUD");
        m_NamePlate = Resources.Load<NamePlate>("Prefabs/NamePlate");
    }

    protected override void OnUpdate()
    {
        // Create and update health UI
        int self_id = -1;
        int selfTeamIdx = -1;
        Entities.ForEach((Entity e, ref LocalPlayer localPlayer) =>
        {
            self_id = localPlayer.playerId;
            if(localPlayer.hudEntity == Entity.Null)
                localPlayer.hudEntity = PrefabAssetManager.CreateEntity(EntityManager.World, m_Hud.gameObject);

            if(localPlayer.controlledEntity != Entity.Null)
            {
                var h = EntityManager.GetComponentData<HealthStateData>(localPlayer.controlledEntity);
                var hud = EntityManager.GetComponentObject<IngameHUD>(localPlayer.hudEntity);
                hud.FrameUpdate();
                hud.m_Health.UpdateUI(ref h);
            }

            // Store our team idx for nameplate coloring below
            if(EntityManager.Exists(localPlayer.playerEntity))
            {
                var ps = EntityManager.GetComponentData<Player.State>(localPlayer.playerEntity);
                selfTeamIdx = ps.teamIndex;
            }
        });

        // Create nameplate for new players
        Entities.WithNone<NamePlateEntityHolder>().ForEach((Entity e, ref Player.State player) =>
        {
            if (player.playerId == self_id)
                return;

            var npe = new NamePlateEntityHolder();
            var avail = m_NamePlates.IndexOf(null);
            if(avail < 0)
            {
                avail = m_NamePlates.Count;
                m_NamePlates.Add(null);
            }
            npe.namePlateIdx = avail;
            m_NamePlates[avail] = GameObject.Instantiate<NamePlate>(m_NamePlate);
            EntityManager.AddComponentData(e, npe);
        });

        // Destroy nameplate for players gone
        Entities.WithNone<Player.State>().ForEach((Entity e, ref NamePlateEntityHolder namePlateEntity) =>
        {
            GameObject.Destroy(m_NamePlates[namePlateEntity.namePlateIdx]);
            m_NamePlates[namePlateEntity.namePlateIdx] = null;
            EntityManager.RemoveComponent(e, typeof(NamePlateEntityHolder));
        });

        var physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
        // Update nameplates
        Entities.ForEach((Entity e, ref Player.State player, ref NamePlateEntityHolder plateHolder) =>
        {
            // Skip players not controlling anything
            if (player.controlledEntity == Entity.Null)
                return;

            // Skip self
            if (player.playerId == self_id)
                return;

            // Only if controlling somethin with pos
            if (!EntityManager.HasComponent<Unity.Transforms.LocalToWorld>(player.controlledEntity))
                return;

            // Get screenpos
            var ltw = EntityManager.GetComponentData<Unity.Transforms.LocalToWorld>(player.controlledEntity);
	        var camera = GameApp.CameraStack.TopCamera();
            var platePos = ltw.Position + new float3(0, 1.8f, 0);
            var screenPos = camera.WorldToScreenPoint(platePos);

            var namePlate = m_NamePlates[plateHolder.namePlateIdx];

            // Update visibility
            bool visible = screenPos.z > 0;

            if(visible)
            {
                // Check for occlusion
	            var rayStart = camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));

                var filter = CollisionFilter.Default;
                filter.CollidesWith = 1 << 0;
                var ray = new RaycastInput()
                {
                    Start = rayStart,
                    End = platePos,
                    Filter = filter
                };
                Unity.Physics.RaycastHit hit;
                if (physicsWorldSystem.PhysicsWorld.CollisionWorld.CastRay(ray, out hit))
                {
                    visible = false;
                }
            }

            if (!visible && namePlate.gameObject.activeSelf)
                namePlate.gameObject.SetActive(false);
            else if (visible && !namePlate.gameObject.activeSelf)
                namePlate.gameObject.SetActive(true);

            if (!visible)
                return;

            var friendly = player.teamIndex == selfTeamIdx;

            // Update position and name and color
            namePlate.namePlateRoot.transform.position = screenPos;
            namePlate.nameText.Set(ref player.playerName);
            namePlate.nameText.color = Game.game.gameColors[friendly ? (int)Game.GameColor.Friend : (int)Game.GameColor.Enemy];
        });
    }

    protected override void OnDestroy()
    {
        Entities.ForEach((Entity e, ref LocalPlayer lp) =>
        {
            if(lp.hudEntity != Entity.Null)
                PrefabAssetManager.DestroyEntity(EntityManager, lp.hudEntity);
        });
        foreach(var e in m_NamePlates)
        {
            if (e != null)
                GameObject.Destroy(e);
        }
    }
}

// TODO: mogensh I disabled this out when removing presentationentity
//[DisableAutoCreation]
//public class UpdateCharacterUI : BaseComponentSystem
//{
//    EntityQuery Group;
//    Entity hud;
//
//    public UpdateCharacterUI(GameWorld world) : base(world)
//    {
//        m_prefab = Resources.Load<IngameHUD>("Prefabs/CharacterHUD");
//    }
//
//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        Group = GetEntityQuery(typeof(LocalPlayer), typeof(PlayerCameraSettings.State),
//            typeof(LocalPlayerCharacterControl.State));
//
//        hud = PrefabAssetManager.CreateEntity(World, m_prefab.gameObject);
//    }
//
//    protected override void OnDestroy()
//    {
//        PrefabAssetManager.DestroyEntity(EntityManager, hud);
//    }
//
//    protected override void OnUpdate()
//    {
//        var entityArray = Group.ToEntityArray(Allocator.TempJob);
//        var localPlayerArray = Group.ToComponentArray<LocalPlayer>();
//        var playerCamSettingsArray = Group.ToComponentDataArray<PlayerCameraSettings.State>(Allocator.TempJob);
//        var charControlArray = Group.ToComponentDataArray<LocalPlayerCharacterControl.State>(Allocator.TempJob);
//
//        GameDebug.Assert(localPlayerArray.Length <= 1, "There should never be more than 1 local player!");
//
//        for (var i = 0; i < localPlayerArray.Length; i++)
//        {
//            var entity = entityArray[i];
//            var localPlayer = localPlayerArray[i];
//            var characterControl = charControlArray[i];
//            var cameraSettings = playerCamSettingsArray[i];
//
//            var ingameHUD = EntityManager.GetComponentObject<IngameHUD>(hud);
//
//            // Handle controlled entity change
//            if (characterControl.lastRegisteredControlledEntity != localPlayer.controlledEntity)
//            {
//                // Delete all current UI elements
//                if (characterControl.healthUI != Entity.Null)
//                {
//                    PrefabAssetManager.DestroyEntity(EntityManager,characterControl.healthUI);
//                }
//                characterControl.healthUI = Entity.Null;
//
//                {
//                    var abilityUIBuffer = EntityManager.GetBuffer<LocalPlayerCharacterControl.AbilityUIElement>(entity).ToNativeArray(Allocator.Temp);
//                    EntityManager.GetBuffer<LocalPlayerCharacterControl.AbilityUIElement>(entity).Clear();
//                    for (var j = 0; j < abilityUIBuffer.Length; j++)
//                    {
//                        PrefabAssetManager.DestroyEntity(EntityManager,abilityUIBuffer[j].entity);
//                    }
//                }
//
//                //
//                characterControl.lastRegisteredControlledEntity = Entity.Null;
//
//                // Set new controlled entity
//                if (EntityManager.HasComponent<Character.State>(localPlayer.controlledEntity))
//                {
//                    characterControl.lastRegisteredControlledEntity = localPlayer.controlledEntity;
//                }
//
//
//                // Build new UI elements
//                if (characterControl.lastRegisteredControlledEntity != Entity.Null &&
//                    EntityManager.Exists(characterControl.lastRegisteredControlledEntity))
//                {
//                    var characterEntity = characterControl.lastRegisteredControlledEntity;
//
//                    if (EntityManager.HasComponent<PresentationOwner.State>(characterEntity))
//                    {
//                                            var presentationOwner =
//                        EntityManager.GetComponentData<PresentationOwner.State>(characterEntity);
//
//                    var charPresentation = presentationOwner.currentPresentation;
//
//                    if (EntityManager.HasComponent<CharacterUISetup>(charPresentation))
//                    {
//                        // TODO (mogensh) we should move UI setup out to Hero setup (or something similar clientside)
//                        var uiSetup = EntityManager.GetComponentObject<CharacterUISetup>(charPresentation);
//                        if (uiSetup.healthUIPrefab != null)
//                        {
//                            characterControl.healthUI =
//                                PrefabAssetManager.CreateEntity(World, uiSetup.healthUIPrefab.gameObject);
//
//                            var healthUI =
//                                EntityManager.GetComponentObject<CharacterHealthUI>(characterControl.healthUI);
//
//                            healthUI.transform.SetParent(ingameHUD.transform, false);
//                        }
//                    }
//
//                    // TODO (mogensh) this has moved to PresentationUI. But this UI is a mess and needs to be rethought
////                    var presentationsBuffer = EntityManager.GetBuffer<Character.Presentation>(characterEntity).ToNativeArray(Allocator.Temp);
////                    for (int j = 0; j < presentationsBuffer.Length; j++)
////                    {
////                        var presentationEntity = presentationsBuffer[j].entity;
////                        if (EntityManager.HasComponent<CharacterPresentationSetup>(presentationEntity))
////                        {
////                            var charPresSetup = EntityManager.GetComponentObject<CharacterPresentationSetup>(presentationEntity);
////
////                            if (charPresSetup.uiPrefabs == null || charPresSetup.uiPrefabs.Length == 0)
////                                continue;
////
////                            foreach (var uiPrefab in charPresSetup.uiPrefabs)
////                            {
////                                var abilityUIEntity = PrefabAssetManager.CreateEntity(World, uiPrefab.gameObject);
////                                var abilityUI = EntityManager.GetComponentData<AbilityUI.State>(abilityUIEntity);
////                                abilityUI.abilityOwner = characterEntity;
////                                EntityManager.SetComponentData(abilityUIEntity,abilityUI);
////
////                                var transform = EntityManager.GetComponentObject<RectTransform>(abilityUIEntity);
////                                transform.SetParent(ingameHUD.transform, false);
////
////                                var abilityUIBuffer = EntityManager.GetBuffer<LocalPlayerCharacterControl.AbilityUIElement>(entity);
////                                abilityUIBuffer.Add(new LocalPlayerCharacterControl.AbilityUIElement
////                                {
////                                    entity = abilityUIEntity
////                                });
////                            }
////                        }
////                    }
////                    presentationsBuffer.Dispose();
//                    }
//                }
//            }
//
//
//            // Update current setup
//            if (characterControl.lastRegisteredControlledEntity == Entity.Null)
//                continue;
//
//            // Check for damage inflicted and recieved
//            var damageHistory = EntityManager.GetComponentData<DamageHistoryData>(characterControl.lastRegisteredControlledEntity);
//            if (damageHistory.inflictedDamage.tick > characterControl.lastDamageInflictedTick)
//            {
//                characterControl.lastDamageInflictedTick = damageHistory.inflictedDamage.tick;
//                ingameHUD.ShowHitMarker(damageHistory.inflictedDamage.lethal == 1);
//            }
//
//            var charAnimState =
//                EntityManager.GetComponentData<Character.InterpolatedData>(characterControl
//                    .lastRegisteredControlledEntity);
//            if (charAnimState.damageTick > characterControl.lastDamageReceivedTick)
//            {
//                ingameHUD.m_Crosshair.ShowHitDirectionIndicator(charAnimState.damageDirection);
//                characterControl.lastDamageReceivedTick = charAnimState.damageTick;
//            }
//
//            // Update health
//            if (characterControl.healthUI != Entity.Null)
//            {
//                var healthState = EntityManager.GetComponentData<HealthStateData>(characterControl.lastRegisteredControlledEntity);
//
//                var healthUI =
//                    EntityManager.GetComponentObject<CharacterHealthUI>(characterControl.healthUI);
//
//                healthUI.UpdateUI(ref healthState);
//            }
//
//            var playerState = EntityManager.GetComponentData<Player.State>(localPlayer.playerEntity);
//            ingameHUD.FrameUpdate(ref playerState, cameraSettings);
//
//            EntityManager.SetComponentData(entity,characterControl);
//
//        }
//
//        entityArray.Dispose();
//        charControlArray.Dispose();
//        playerCamSettingsArray.Dispose();
//    }
//
//    IngameHUD m_prefab;
//}
