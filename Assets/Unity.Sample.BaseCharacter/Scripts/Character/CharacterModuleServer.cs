using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Profiling;




public struct CharacterSpawnRequest : IComponentData
{
    public int characterType;
    public Vector3 position;
    public Quaternion rotation;
    public Entity playerEntity;

    private CharacterSpawnRequest(int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
    {
        this.characterType = characterType;
        this.position = position;
        this.rotation = rotation;
        this.playerEntity = playerEntity;
    }

    public static void Create(EntityCommandBuffer commandBuffer, int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
    {
        var data = new CharacterSpawnRequest(characterType, position, rotation, playerEntity);
        Entity entity = commandBuffer.CreateEntity();
        commandBuffer.AddComponent(entity, data);
    }
}

public struct CharacterDespawnRequest : IComponentData
{
    public Entity characterEntity;

    public static void Create(World world, Entity characterEntity)
    {
        var data = new CharacterDespawnRequest()
        {
            characterEntity = characterEntity,
        };
        var entity = world.EntityManager.CreateEntity(typeof(CharacterDespawnRequest));
        world.EntityManager.SetComponentData(entity, data);
    }

    public static void Create(EntityCommandBuffer commandBuffer, Entity characterEntity)
    {
        var data = new CharacterDespawnRequest()
        {
            characterEntity = characterEntity,
        };
        Entity entity = commandBuffer.CreateEntity();
        commandBuffer.AddComponent(entity, data);
    }
}

[DisableAutoCreation]
[UpdateBefore(typeof(HandleCharacterSpawn))]
public class HandleCharacterSpawnRequests : ComponentSystem
{
    EntityQuery SpawnGroup;

    protected override void OnCreate()
    {
        base.OnCreate();
        SpawnGroup = GetEntityQuery(typeof(CharacterSpawnRequest));
    }

    protected override void OnUpdate()
    {
        var requestArray = SpawnGroup.ToComponentDataArray<CharacterSpawnRequest>(Allocator.Persistent);
        if (requestArray.Length == 0)
        {
            requestArray.Dispose();
            return;
        }


        var requestEntityArray = SpawnGroup.ToEntityArray(Allocator.Persistent);

        // Copy requests as spawning will invalidate Group
        var spawnRequests = new CharacterSpawnRequest[requestArray.Length];
        for (var i = 0; i < requestArray.Length; i++)
        {
            spawnRequests[i] = requestArray[i];
            PostUpdateCommands.DestroyEntity(requestEntityArray[i]);
        }

        for (var i = 0; i < spawnRequests.Length; i++)
        {
            var request = spawnRequests[i];

            var playerState = EntityManager.GetComponentData<Player.State>(request.playerEntity);
            var charEntity = SpawnCharacter( playerState.playerId, request.position, request.rotation, request.characterType);

            GameDebug.Log(World, Character.ShowLifetime, "Spawning character:{0} ", charEntity);


            //We cannot serialize references yet so we hook up manually on client side based on PlayerId.
            //but we set it here none the less for our server side uses if any
            playerState.controlledEntity = charEntity;
            EntityManager.SetComponentData(request.playerEntity,playerState);

        }
        requestEntityArray.Dispose();

        requestArray.Dispose();
    }


    List<Entity> abilityList = new List<Entity>(16);
    public Entity SpawnCharacter(int playerId, Vector3 position, Quaternion rotation,
        int heroIndex)
    {
        var heroRegistry = HeroRegistry.GetRegistry(EntityManager);
        var heroCount = heroRegistry.Value.Heroes.Length;

        heroIndex = Mathf.Clamp(heroIndex, 0,heroCount-1);

        var charEntity = PrefabAssetManager.CreateEntity(EntityManager, heroRegistry.Value.Heroes[heroIndex].characterPrefab);

        var charSettings = EntityManager.GetComponentData<Character.Settings>(charEntity);
        Character.TeleportTo(ref charSettings, position, rotation);
        PostUpdateCommands.SetComponent(charEntity,charSettings);

        var charRepAll = EntityManager.GetComponentData<Character.ReplicatedData>(charEntity);
        charRepAll.heroTypeIndex = heroIndex;
        PostUpdateCommands.SetComponent(charEntity,charRepAll);
        PostUpdateCommands.SetComponent(charEntity,new Player.OwnerPlayerId
        {
            Value = playerId,
        });

        // TODO (mogensh) move this to inventory code (server part)
        // Spawn items in inventory
        var itemCount = heroRegistry.Value.Heroes[heroIndex].Items.Length;
        for(int nItem=0;nItem<itemCount;nItem++)
        {

            var entry = heroRegistry.Value.Heroes[heroIndex].Items[nItem];

            if (!entry.asset.IsSet())
                continue;

            var itemEntity = PrefabAssetManager.CreateEntity(EntityManager, entry.asset);

            var itemState = EntityManager.GetComponentData<Item.InputState>(itemEntity);
            itemState.owner = charEntity;
            itemState.slot = entry.slot;
            itemState.playerId = playerId;
            PostUpdateCommands.SetComponent(itemEntity,itemState);
        }

        return charEntity;
    }
}



[DisableAutoCreation]
[UpdateBefore(typeof(HandleCharacterDespawn))]
public class HandleCharacterDespawnRequests : ComponentSystem
{
    EntityQuery DespawnGroup;

    protected override void OnCreate()
    {
        base.OnCreate();
        DespawnGroup = GetEntityQuery(typeof(CharacterDespawnRequest));
    }

    protected override void OnUpdate()
    {
        var requestArray = DespawnGroup.ToComponentDataArray<CharacterDespawnRequest>(Allocator.Persistent);
        if (requestArray.Length == 0)
        {
            requestArray.Dispose();
            return;
        }

        Profiler.BeginSample("HandleCharacterDespawnRequests");

        var requestEntityArray = DespawnGroup.ToEntityArray(Allocator.Persistent);

        for (var i = 0; i < requestArray.Length; i++)
        {
            var request = requestArray[i];

            GameDebug.Assert(EntityManager.HasComponent<Character.State>(request.characterEntity), "Character despawn requst entity is not a character");

           Inventory.Server_DestroyAll(EntityManager, PostUpdateCommands, request.characterEntity);

            GameDebug.Log(World, Character.ShowLifetime, "Despawning character:{0}", request.characterEntity);

            PrefabAssetManager.DestroyEntity(EntityManager,request.characterEntity);
            PostUpdateCommands.DestroyEntity(requestEntityArray[i]);

            if(Character.ShowLifetime.IntValue > 0)
                AnimationGraphHelper.DumpState(World);
        }
        requestEntityArray.Dispose();
        requestArray.Dispose();
        Profiler.EndSample();
    }
}

[DisableAutoCreation]
[UpdateInGroup(typeof(HandleDamageSystemGroup))]
[UpdateAfter(typeof(DamageManager))]
[AlwaysSynchronizeSystem]
public class HandleDamage : JobComponentSystem
{
    private EntityQuery TimeQuery;

    protected override void OnCreate()
    {
        TimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var globalTime = TimeQuery.GetSingleton<GlobalGameTime>();
        var damageHistoryDataFromEntity = GetComponentDataFromEntity<DamageHistoryData>(false);

        Entities
                .ForEach((Entity entity, DynamicBuffer<DamageEvent> damageBuffer, ref HealthStateData healthState, ref HitColliderOwner.State collOwner, ref Character.PredictedData charPredictedState) =>
        {
            if (healthState.health <= 0)
                return;

            var isDamaged = false;
            var impulseVec = Vector3.zero;
            var damage = 0.0f;
            var damageVec = Vector3.zero;

            // Apply hitcollider damage events
            for (var eventIndex = 0; eventIndex < damageBuffer.Length; eventIndex++)
            {
                isDamaged = true;

                var damageEvent = damageBuffer[eventIndex];

                //GameDebug.Log(string.Format("ApplyDamage. Target:{0} Instigator:{1} Dam:{2}", healthState.name, m_world.GetGameObjectFromEntity(damageEvent.instigator), damageEvent.damage ));
                healthState.ApplyDamage(damageEvent, globalTime.gameTime.tick);

                impulseVec += damageEvent.Direction * damageEvent.Impulse;
                damageVec += damageEvent.Direction * damageEvent.Damage;
                damage += damageEvent.Damage;

                //damageHistory.ApplyDamage(ref damageEvent, m_world.worldTime.tick);

                if (damageHistoryDataFromEntity.HasComponent(damageEvent.Instigator))
                {
                    var instigatorDamageHistory = damageHistoryDataFromEntity[damageEvent.Instigator];
                    if (globalTime.gameTime.tick > instigatorDamageHistory.inflictedDamage.tick)
                    {
                        instigatorDamageHistory.inflictedDamage.tick = globalTime.gameTime.tick;
                        instigatorDamageHistory.inflictedDamage.lethal = 0;
                    }
                    if (healthState.health <= 0)
                        instigatorDamageHistory.inflictedDamage.lethal = 1;

                    damageHistoryDataFromEntity[damageEvent.Instigator] = instigatorDamageHistory;
                }

                collOwner.collisionEnabled = healthState.health > 0 ? 1 : 0;
            }
            damageBuffer.Clear();
            // TODO (mogensh) make sure damagebuffer also is cleared on clients


            if (isDamaged)
            {
                var damageImpulse = impulseVec.magnitude;
                var damageDir = damageImpulse > 0 ? impulseVec.normalized : damageVec.normalized;

                charPredictedState.damageTick = globalTime.gameTime.tick;
                charPredictedState.damageDirection = damageDir;
                charPredictedState.damageImpulse = damageImpulse;

                if (healthState.health <= 0)
                {
                    //                    var ragdollState =  EntityManager.GetComponentData<RagdollStateData>(entity);
                    //                    ragdollState.ragdollActive = 1;
                    //                    ragdollState.impulse = impulseVec;
                    //                    EntityManager.SetComponentData(entity, ragdollState);
                }
            }
        }).Run();

        return default;
    }
}
