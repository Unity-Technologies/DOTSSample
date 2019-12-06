using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

[DisallowMultipleComponent]
public class NamePlateOwner : MonoBehaviour
{
    public struct State : ISystemStateComponentData
    {
        public int namePlateId;
    }

	public NamePlate namePlatePrefab;
	public Transform namePlateTransform;

	[NonSerialized] public bool visible = true;
	[NonSerialized] public string text;
	[NonSerialized] public int team;
	[NonSerialized] public float health;





    // NamePlate not blitable, so we register in array and store index
    static List<NamePlate> m_NamePlateArray = new List<NamePlate>(32);

    public static NamePlate GetNamePlate(int id)
    {
        return m_NamePlateArray[id];
    }

    public static int RegisterNamePlate(NamePlate namePlate)
    {
        for (int i = 0; i < m_NamePlateArray.Count; i++)
        {
            if (m_NamePlateArray[i] == null)
            {
                m_NamePlateArray[i] = namePlate;
                return i;
            }
        }
        m_NamePlateArray.Add(namePlate);
        return m_NamePlateArray.Count - 1;
    }

    public static void UnregisterNamePlate(int id)
    {
        m_NamePlateArray[id] = null;
    }
}

[UpdateInGroup(typeof(ClientLateUpdateGroup))]
[UpdateBefore(typeof(UpdateNamePlates))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class HandleNamePlateSpawn : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities.WithNone<NamePlateOwner.State>()
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // Instantiates GameObjects
            .ForEach((Entity entity, NamePlateOwner namePlateOwner) =>
        {
            var namePlate = GameObject.Instantiate(namePlateOwner.namePlatePrefab);

            var id = NamePlateOwner.RegisterNamePlate(namePlate);

            var state = new NamePlateOwner.State
            {
                  namePlateId = id,
            };
            PostUpdateCommands.AddComponent(entity,state);
        }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
        return default;
    }
}

[UpdateInGroup(typeof(ClientLateUpdateGroup))]
[UpdateBefore(typeof(UpdateNamePlates))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class HandleNamePlateDespawn : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities.WithNone<NamePlateOwner>()
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // Destroys GameObjects
            .ForEach((Entity entity, ref NamePlateOwner.State state) =>
        {
            var namePlate = NamePlateOwner.GetNamePlate(state.namePlateId);
            NamePlateOwner.UnregisterNamePlate(state.namePlateId);
            GameObject.Destroy(namePlate.gameObject);

            PostUpdateCommands.RemoveComponent<NamePlateOwner.State>(entity);
        }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
        return default;
    }
}


[UpdateInGroup(typeof(ClientLateUpdateGroup))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class UpdateNamePlates : JobComponentSystem
{
	EntityQuery LocalPlayerGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
        Entities
            .ForEach((Entity e, ref LocalPlayer localPlayer) =>
        {
		    if (localPlayer.playerEntity == Entity.Null)
			    return;
        }).Run();
        return default;


// TODO: mogensh I disabled this out when removing presentationentity
//
//	    Entities.ForEach((NamePlateOwner plateOwner, ref PresentationEntity presentation,
//	        ref NamePlateOwner.State state) =>
//	    {
//	        var namePlate = NamePlateOwner.GetNamePlate(state.namePlateId);
//
//	        if (namePlate == null)
//	        {
//	            GameDebug.LogError("namePlateOwner.namePlate == null");
//	            return;
//	        }
//
//	        var root = namePlate.namePlateRoot.gameObject;
//
//	        if (IngameHUD.showHud.IntValue == 0)
//	        {
//	            SetActiveIfNeeded(root, false);
//	            return;
//	        }
//
//	        if (!plateOwner.visible)
//	        {
//	            SetActiveIfNeeded(root, false);
//	            return;
//	        }
//
//	        // Dont show our own
//	        var character = presentation.ownerEntity;
//	        var localPlayerState = EntityManager.GetComponentData<Player.State>(localPlayer.playerEntity);
//	        if (character == localPlayerState.controlledEntity)
//	        {
//	            SetActiveIfNeeded(root, false);
//	            return;
//	        }
//
//	        // Dont show nameplate behinds
//	        var camera = Game.game.TopCamera(); // Camera.allCameras[0];
//	        var platePosWorld = plateOwner.namePlateTransform.position;
//	        var screenPos = camera.WorldToScreenPoint(platePosWorld);
//	        if (screenPos.z < 0)
//	        {
//	            SetActiveIfNeeded(root, false);
//	            return;
//	        }
//
//	        // Test occlusion
//	        var rayStart = camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
//	        var v = platePosWorld - rayStart;
//	        var distance = v.magnitude;
//	        const int defaultLayerMask = 1 << 0;
//	        var occluded = Physics.Raycast(rayStart, v.normalized, distance, defaultLayerMask);
//
//	        var friendly = plateOwner.team == localPlayerState.teamIndex;
//	        var color = friendly ? namePlate.friendColor : namePlate.enemyColor;
//
//	        var showPlate = friendly || !occluded;
//
//	        // Update plate
//	        if (!showPlate)
//	        {
//	            SetActiveIfNeeded(root, false);
//	            return;
//	        }
//
//	        namePlate.namePlateRoot.transform.position = screenPos;
//
//	        // Update icon
//	        var showIcon = friendly;
//	        SetActiveIfNeeded(namePlate.icon.gameObject, showIcon);
//	        if (showIcon)
//	        {
//	            namePlate.icon.color = color;
//	        }
//
//	        // Update name text
//	        var inNameTextDist = distance <= namePlate.maxNameDistance;
//	        var showNameText = !occluded && inNameTextDist;
//	        SetActiveIfNeeded(namePlate.nameText.gameObject, showNameText);
//	        if (showNameText)
//	        {
//	            namePlate.nameText.text = plateOwner.text;
//	            namePlate.nameText.color = color;
//	        }
//
//	        SetActiveIfNeeded(root, true);
//	    });
    }

	// Set settings active on UI Text creates garbage we check for whether active state has changed
	void SetActiveIfNeeded(GameObject go, bool active)
	{
		if (go.activeSelf != active)
		{
			go.SetActive(active);
		}
	}
}
