using System.Collections;
using System.Collections.Generic;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

public struct SkeletonRenderer : IComponentData
{
    public Color Color;
}


[UpdateInGroup(typeof(PresentationSystemGroup))]
public class UpdateSkeletonRenderer : ComponentSystem
{
    [ConfigVar(Name = "animation.show.skeleton", DefaultValue = "0", Description = "Show skeleton", Flags = ConfigVar.Flags.None)]
    public static ConfigVar ShowSkeleton;
    
   
    protected override void OnUpdate()
    {
        if (ShowSkeleton.IntValue == 0)
            return;
        
        var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;// TODO (mogensh) cant we find better way to test for server?
        if (ShowSkeleton.IntValue == 2 && !isServer)
            return;
        if (ShowSkeleton.IntValue == 3 && isServer)
            return;
        
        Entities.WithAll<AnimatedLocalToWorld>().ForEach((Entity entity, ref SkeletonRenderer skeletonRenderer) =>
        {
            var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(entity);
                
            var localToWorldBuffer = EntityManager.GetBuffer<AnimatedLocalToWorld>(entity).Reinterpret<float4x4>();

            Draw(ref sharedRigDef.Value.Value.Skeleton.ParentIndexes, ref localToWorldBuffer, skeletonRenderer.Color);
        });
    }

    void Draw(ref BlobArray<int> parentIndexes, ref DynamicBuffer<float4x4> localToWorldBuffer, Color color)
    {
        for (int i = 1; i != parentIndexes.Length; ++i)
        {
            var localToWorld = localToWorldBuffer[i];

            var pIdx = parentIndexes[i];
            var parentLocalToWorld = localToWorldBuffer[pIdx];

            Vector3 p1 = localToWorld.c3.xyz;
            Vector3 p2 = parentLocalToWorld.c3.xyz;
//            DebugOverlay.DrawLine3D(p1, p2, color);
            Debug.DrawLine(p1, p2, color);
        }
    }
}
