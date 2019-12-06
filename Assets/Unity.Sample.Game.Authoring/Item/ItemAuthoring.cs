using System.Collections;
using System.Collections.Generic;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.NetCode;
using Unity.Sample.Core;

#if UNITY_EDITOR
public class ItemAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityCollectionAuthoring.AbilitySetup[] abilities = new AbilityCollectionAuthoring.AbilitySetup[0];

   // public BoneReferenceAuthoring attachBone; // TODO (mogensh) Use BoneReferenceAuthoring when all conversion happens in editor
    public BoneReference attachBone2;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        GameDebug.Log(string.Format("Convert Item:{0}",this));

        dstManager.AddComponentData(entity,Item.InputState.Default);

        AbilityCollectionAuthoring.AddAbilityComponents(entity, dstManager, conversionSystem, abilities);



        var runtimeBoneRef = RuntimeBoneReference.Default;
        runtimeBoneRef.ReferenceRig = RigDefinitionAsset.ConvertRig(attachBone2.RigAsset);
        runtimeBoneRef.BoneIndex = attachBone2.BoneIndex;
        RigAttacher.AddRigAttacher(entity, dstManager, runtimeBoneRef);
    }
}
#endif


