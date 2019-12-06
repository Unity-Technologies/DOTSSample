using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public class TeleporterSystemClient : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
        var vfxSystem = World.GetExistingSystem<VFXSystem>();

        Entities
            .WithoutBurst()
            .ForEach((TeleporterClientDataClass tcds, ref TeleporterPresentationData presentation, ref TeleporterClientData tcd, ref LocalToWorld ltw) =>
        {
            // TODO: effectTick cannot yet be synchronized across net so no effect yet
            if(tcd.effectEvent.Update(globalTime.gameTime, presentation.effectTick))
            {
                var epos = math.mul(ltw.Value, new float4(tcd.effectPos, 1.0f));
                vfxSystem.SpawnPointEffect(tcds.effect, epos.xyz, new float3(0,1,0));
            }
        }).Run();

        return default;
    }
}
