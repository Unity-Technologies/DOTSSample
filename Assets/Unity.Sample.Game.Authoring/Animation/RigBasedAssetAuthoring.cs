

using System.Collections.Generic;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Sample.Core;
using Unity.Transforms;
#if UNITY_EDITOR
using Unity.Animation.Hybrid;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

[RequireComponent(typeof(RigComponent))]
public class RigBasedAssetAuthoring : MonoBehaviour
{

    public List<GameObject> Excluded = new List<GameObject>();
    
    
    
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class ConvertionSystem : GameObjectConversionSystem
    {
        private EntityQuery Query;

        protected override void OnCreate()
        {
            base.OnCreate();

            Query = GetEntityQuery(typeof(RigBasedAssetAuthoring));
        }

        protected override void OnUpdate()
        {
            var assetAuthoringArray = Query.ToComponentArray<RigBasedAssetAuthoring>();

            foreach (var assetAuthoring in assetAuthoringArray)
            {
                GameDebug.Log("Converting RigBasedAssetAuthoring GO:" + assetAuthoring.gameObject);

                var assetEntity = GetPrimaryEntity(assetAuthoring.gameObject);
                DstEntityManager.AddComponentData(assetEntity, new RigBasedAsset.Base());

                var rigComponent = assetAuthoring.GetComponent<RigComponent>();
                var rigDefSetup = DstEntityManager.GetComponentData<RigDefinitionSetup>(assetEntity);
                
                ConvertSkinnedMeshRenderes(assetAuthoring, assetEntity, rigComponent);

                ConvertAttahments(assetAuthoring, assetEntity, rigComponent, rigDefSetup.Value);
             
                // TODO (mogensh) check that everything that is not deleted is either in attachment or skin
                
                // Delete skeleton bones
                foreach (var bone in assetAuthoring.Excluded)
                {
                    var boneEntity =  GetPrimaryEntity(bone);
                    DstEntityManager.DestroyEntity(boneEntity);
                }
            }
        }

        struct AttachmentData
        {
            public GameObject go;
            public int boneIndex;
        }
        
        void ConvertAttahments(RigBasedAssetAuthoring assetAuthoring, Entity assetEntity, RigComponent rigComponent, BlobAssetReference<RigDefinition> rig)
        {

            var attachmentInfos = new List<AttachmentData>();
            var rigComponentBones = new List<Transform>(rigComponent.Bones);
            for (int nBone = 0; nBone < rigComponentBones.Count; nBone++)
            {
                var localBone = rigComponentBones[nBone];
                for (int nChild = 0; nChild < localBone.childCount; nChild++)
                {
                    var child = localBone.GetChild(nChild);

                    // Ignore children that are also bones
                    if (rigComponentBones.Contains(child))
                    {
//                        GameDebug.Log("bone" + child + " should not be attached");
                        continue;
                    }
                    attachmentInfos.Add(new AttachmentData {
                        go = child.gameObject,
                        boneIndex = nBone,
                    });
                }
            }


            if (attachmentInfos.Count > 0)
            {
                DstEntityManager.AddBuffer<RigBasedAsset.Attachment>(assetEntity);
            }
           
            // Find objects attached to bones
            for (int i = 0; i < attachmentInfos.Count; i++)
            {
                var attachmentInfo = attachmentInfos[i];
                var attachmentEntity = GetPrimaryEntity(attachmentInfo.go);

                var assetAttachments = DstEntityManager.GetBuffer<RigBasedAsset.Attachment>(assetEntity);
                assetAttachments.Add(new RigBasedAsset.Attachment() {Value = attachmentEntity});
                                
                DstEntityManager.AddComponentData(attachmentEntity, new RigAttacher.AttachEntity()
                {
                    Value = attachmentEntity,
                });
                
                
                // Remove from parent
                if(DstEntityManager.HasComponent<Parent>(attachmentEntity))
                    DstEntityManager.RemoveComponent<Parent>(attachmentEntity);
                if(DstEntityManager.HasComponent<LocalToParent>(attachmentEntity))
                    DstEntityManager.RemoveComponent<LocalToParent>(attachmentEntity);
                if(!DstEntityManager.HasComponent<Static>(attachmentEntity))
                    DstEntityManager.AddComponent<Static>(attachmentEntity);

                // Add rig attacher
                var boneRef = RuntimeBoneReference.Default;
                boneRef.BoneIndex = attachmentInfo.boneIndex;
                boneRef.ReferenceRig = rig;
                RigAttacher.AddRigAttacher(attachmentEntity, DstEntityManager, boneRef);


                GameDebug.Log("  Found attrachment:{0} on bone:{1} rig:{2}", attachmentInfo.go,boneRef.BoneIndex,rigComponent);
            }
        }
        
        
        void ConvertSkinnedMeshRenderes(RigBasedAssetAuthoring assetAuthoring, Entity assetEntity, 
            RigComponent rigComponent)
        {

            var skinnedMeshRenderers = assetAuthoring.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (skinnedMeshRenderers.Length > 0)
            {
                DstEntityManager.AddBuffer<RigBasedAsset.SkinnedMeshRenderer>(assetEntity);    
            }
            foreach (var meshRenderer in skinnedMeshRenderers)
            {
                var skinEntity = GetPrimaryEntity(meshRenderer.gameObject);
                //var skinEntity = conversionSystem.CreateAdditionalEntity(meshRenderer);
    #if UNITY_EDITOR
                DstEntityManager.SetName(skinEntity, "Entity " + skinEntity.Index + " Skin_" + meshRenderer.gameObject.name);
    #endif

                var skinRendererBuffer = DstEntityManager.GetBuffer<RigBasedAsset.SkinnedMeshRenderer>(assetEntity);
                skinRendererBuffer.Add(new RigBasedAsset.SkinnedMeshRenderer { Value = skinEntity, });

                //var rigEntity = GetPrimaryEntity(src.Rig);
                //DstEntityManager.AddComponentData(entity, new SkinnedMeshComponentData { RigEntity = rigEntity });
                DstEntityManager.AddComponentData(skinEntity, new SkinnedMeshRigEntity { Value = assetEntity });
                DstEntityManager.AddComponentData(skinEntity, new LocalToWorld());
                DstEntityManager.AddComponentData(skinEntity, new BoneIndexOffset());

                DstEntityManager.AddBuffer<SkinnedMeshToRigIndex>(skinEntity);
                DstEntityManager.AddBuffer<BindPose>(skinEntity);
                DstEntityManager.AddBuffer<SkinMatrix>(skinEntity);

                var skeletonIndexArray = DstEntityManager.GetBuffer<SkinnedMeshToRigIndex>(skinEntity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(skinEntity);
                var skinMatrices = DstEntityManager.GetBuffer<SkinMatrix>(skinEntity);

                var smBones = meshRenderer.bones;
                skeletonIndexArray.ResizeUninitialized(smBones.Length);
                bindPoseArray.ResizeUninitialized(smBones.Length);
                skinMatrices.ResizeUninitialized(smBones.Length);

                //GameDebug.Log("skin smBones");
                //for (int i = 0; i < smBones.Length; i++)
                //{
                //    var relativePath = RigGenerator.ComputeRelativePath(smBones[i], transform);
                //    var id = (StringHash)relativePath;
                //    GameDebug.Log("  " + i + ":" + id.Id + " path:" + relativePath);
                //}

                for (int j = 0; j != smBones.Length; ++j)
                {
                    var remap = new SkinnedMeshToRigIndex { Value = -1 };

                    var smBoneRelativePath = RigGenerator.ComputeRelativePath(smBones[j], assetAuthoring.transform);
                    var smBoneId = (StringHash)smBoneRelativePath;

                    for (int k = 0; k != rigComponent.Bones.Length; ++k)
                    {
                        var relativePath = RigGenerator.ComputeRelativePath(rigComponent.Bones[k], rigComponent.transform);
                        var id = (StringHash)relativePath;

                        if (smBoneId.Equals(id))
                        {
                            remap.Value = k;
                            break;
                        }
                    }
                    skeletonIndexArray[j] = remap;

                    var bindPose = meshRenderer.sharedMesh.bindposes[j];
                    bindPoseArray[j] = new BindPose { Value = bindPose };

                    var skinMat = math.mul(meshRenderer.bones[j].localToWorldMatrix, bindPose);
                    skinMatrices[j] = new SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
                }
            }
        }
    }
}




#endif