

using System;
#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Sample.Core;

[DisallowMultipleComponent]
public class AnimSourceControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IBundledAssetProvider
{
    [Serializable]
    public struct RigLOD
    {
        public Unity.Animation.Hybrid.RigComponent Rig;
        public float MaxDist;
    }

    public List<RigLOD> Rigs = new List<RigLOD>();

    public WeakAssetReference AnimSourceRoot;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        GameDebug.Log(string.Format("Convert DotsAnimStateController:{0}", this));

        var settings = AnimSourceController.Settings.Default;
        settings.RootAnimSource = AnimSourceRoot;
        dstManager.AddComponentData(entity, settings);

        var rigDataBuffer = dstManager.AddBuffer<AnimSourceController.RigData>(entity);
        for (int i = 0; i < Rigs.Count; i++)
        {
            var rigData = new AnimSourceController.RigData
            {
                Rig = RigDefinitionAsset.ConvertRig(Rigs[i].Rig),
                MaxDist = Rigs[i].MaxDist,
            };
            rigDataBuffer.Add(rigData);
        }

        var conversionSettings = conversionSystem.GetBuildSettingsComponent<NetCodeConversionSettings>();
        var server = conversionSettings != null ? conversionSettings.Target == NetcodeConversionTarget.Server :
            dstManager.World.GetExistingSystem<Unity.NetCode.ServerSimulationSystemGroup>() != null;
        var color = server ? Color.gray : Color.green;
        dstManager.AddComponentData(entity, new SkeletonRenderer { Color = color, });
    }

    public void AddBundledAssets(BuildType buildType, List<WeakAssetReference> assets)
    {
        assets.Add(AnimSourceRoot);
    }
}

#endif
