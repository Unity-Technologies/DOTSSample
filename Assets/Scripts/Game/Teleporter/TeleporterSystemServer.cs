using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class TeleporterSystemServer : JobComponentSystem
{
    NativeList<Entity> characters;
    NativeList<float3> positions;

    protected override void OnCreate()
    {
        characters = new NativeList<Entity>(Allocator.Persistent);
        positions = new NativeList<float3>(Allocator.Persistent);
    }
    protected override void OnDestroy()
    {
        characters.Dispose();
        positions.Dispose();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();

        var na_characters = characters;
        var na_positions = positions;

        na_characters.Clear();
        na_positions.Clear();

        Entities
            .ForEach((Entity entity, ref Character.Settings charSettings, ref Unity.Transforms.LocalToWorld t) =>
        {
            na_characters.Add(entity);
            na_positions.Add(t.Position);
        }).Run();

        var characterSettingsFromEntity = GetComponentDataFromEntity<Character.Settings>(false);
        var teleporterServerFromEntity = GetComponentDataFromEntity<TeleporterServer>(true);
        var teleporterPresentationDataFromEntity = GetComponentDataFromEntity<TeleporterPresentationData>(false);

        Entities
            .ForEach((Entity entity, ref Unity.Transforms.LocalToWorld lw, ref TeleporterServer teleporter, ref TeleporterPresentationData presentation) =>
        {
            float3 teleporterPos = teleporter.triggerPos;// lw.Position;
            for(int i = 0, c = na_characters.Length; i <c; ++i)
            {
                if (math.distance(na_positions[i], teleporterPos) > teleporter.triggerDist)
                    continue;

                var character = na_characters[i];
                var charSettings = characterSettingsFromEntity[character];
                var targetTeleporter = teleporterServerFromEntity[teleporter.targetTeleporter];

                Character.TeleportTo(ref charSettings, targetTeleporter.spawnPos, targetTeleporter.spawnRot);

                characterSettingsFromEntity[character] = charSettings;

                var targetTeleporterPresentation = teleporterPresentationDataFromEntity[teleporter.targetTeleporter];
                targetTeleporterPresentation.effectTick = globalTime.gameTime.tick;
                teleporterPresentationDataFromEntity[teleporter.targetTeleporter] = targetTeleporterPresentation;

                break;
            }

        }).Run();

        return default;
    }

}
