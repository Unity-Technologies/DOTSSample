//using System.Collections.Generic;
//using Unity.Animation;
//using Unity.Animation.Hybrid;
//using Unity.Entities;
//using Unity.Rendering;
//using Unity.Mathematics;
//using Unity.Sample.Core;
//using Unity.Transforms;
//using UnityEngine;
//using Skeleton = Unity.Animation.Hybrid.Skeleton;
//
//#if UNITY_EDITOR
//[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
//public class PartConversion : GameObjectConversionSystem
//{
//    private EntityQuery PartQuery;
//
//    protected override void OnCreate()
//    {
//        base.OnCreate();
//
//        PartQuery = GetEntityQuery(typeof(PartAuthoring));
//    }
//
//    protected override void OnUpdate()
//    {
//        var partAuthorings = PartQuery.ToComponentArray<PartAuthoring>();
//
//        foreach (var partAuthoring in partAuthorings)
//        {
//            GameDebug.Log("Converting PartAuthoring GO:" + partAuthoring.gameObject);
//
//            var partEntity = GetPrimaryEntity(partAuthoring.gameObject);
//            DstEntityManager.AddComponent<Part.Owner>(partEntity);
//
//            if (partAuthoring.RigDefinition != null)
//            {
//                var rig = RigDefinitionAsset.ConvertRig(partAuthoring.RigDefinition);
//
//                DstEntityManager.AddComponentData(partEntity,Part.Owner.Default);
//                
//                ConvertSkinnedMeshRenderes(partAuthoring, partEntity, partAuthoring.RigDefinition, DstEntityManager,
//                    this);
//                
//                var rigSkeleton = partAuthoring.RigDefinition.GetComponent<Skeleton>();
//                GameDebug.Assert(rigSkeleton != null,"RigDefinition:"+ partAuthoring.RigDefinition + " does not have Skeleton component");
//                GameDebug.Assert(partAuthoring.RigDefinition != null,"RigDefinition property is not set");
//
//                var gameObject = partAuthoring.gameObject;
//
//                var partAttachment = new Part.Attachment
//                {
//                    Part = partEntity,
//                };
//
//                // Find all local bones defined in rig
//                var localBoneList = new List<Transform>();
//                for (int nBone = 0; nBone < rigSkeleton.Bones.Length; nBone++)
//                {
//                    var rigBone = MapTransform(gameObject.transform, rigSkeleton.Bones[nBone]);
//                    if (rigBone == null)
//                    {
//                        GameDebug.LogError("Failed to map:" + rigSkeleton.Bones[nBone]);
//                    }
//
//                    localBoneList.Add(rigBone);
//                }
//
//                // Find objects attached to bones
//                for (int nBone = 0; nBone < localBoneList.Count; nBone++)
//                {
//                    var localBone = localBoneList[nBone];
//                    for (int nChild = 0; nChild < localBone.childCount; nChild++)
//                    {
//                        var child = localBone.GetChild(nChild);
//                        if (localBoneList.Contains(child))
//                        {
//    //                        GameDebug.Log("bone" + child + " should not be attached");
//                            continue;
//                        }
//
//                        var childEntity = GetPrimaryEntity(child.gameObject);
//
//                        DstEntityManager.AddComponentData(childEntity, partAttachment);
//
//                        // Remove from parent
//                        if(DstEntityManager.HasComponent<Parent>(childEntity))
//                            DstEntityManager.RemoveComponent<Parent>(childEntity);
//                        if(DstEntityManager.HasComponent<LocalToParent>(childEntity))
//                            DstEntityManager.RemoveComponent<LocalToParent>(childEntity);
//                        if(!DstEntityManager.HasComponent<Static>(childEntity))
//                            DstEntityManager.AddComponent<Static>(childEntity);
//
//                        // Add rig attacher
//                        var boneRef = RuntimeBoneReference.Default;
//                        boneRef.BoneIndex = nBone;
//                        boneRef.ReferenceRig = rig;
//                        RigAttacher.AddRigAttacher(childEntity, DstEntityManager, boneRef);
//
//    //                    DstEntityManager.GetBuffer<Part.Attachment>(partEntity).Add(new Part.Attachment
//    //                    {
//    //                        Entity = childEntity,
//    //                    });
//
//                        GameDebug.Log("  Found attrachment:{0} on bone:{1} rig:{2}", child,boneRef.BoneIndex,partAuthoring.RigDefinition);
//
//                        // TODO (mogensh) delete all skeleton bone entities. Throw error if they contain components!!
//                    }
//                }
//            }
//        }
//
//
//    }
//
//    public static void ConvertSkinnedMeshRenderes(PartAuthoring partAuthoring, Entity entity, Skeleton RigDefinition,
//        EntityManager dstManager, GameObjectConversionSystem conversionSystem)
//    {
//        dstManager.AddBuffer<Part.SkinnedMeshRendererElement>(entity);
//
//        var skinnedMeshRenderers = partAuthoring.GetComponentsInChildren<SkinnedMeshRenderer>();
//
//
//        foreach (var meshRenderer in skinnedMeshRenderers)
//        {
//            // TODO (mogensh) why not just keep entity already created for meshRenderer gameobject ??
//            var skinEntity = conversionSystem.CreateAdditionalEntity(meshRenderer);
//#if UNITY_EDITOR
//            dstManager.SetName(skinEntity, "Entity " + skinEntity.Index + " Skin_" + meshRenderer.gameObject.name);
//#endif
//
//            var skinRendererBuffer = dstManager.GetBuffer<Part.SkinnedMeshRendererElement>(entity);
//            skinRendererBuffer.Add(new Part.SkinnedMeshRendererElement { entity = skinEntity, });
//
//            //var rigEntity = GetPrimaryEntity(src.Rig);
//            //DstEntityManager.AddComponentData(entity, new SkinnedMeshComponentData { RigEntity = rigEntity });
//            dstManager.AddComponentData(skinEntity, new SkinnedMeshRigEntity());
//            dstManager.AddComponentData(skinEntity, new LocalToWorld());
//            dstManager.AddComponentData(skinEntity, new BoneIndexOffset());
//
//            dstManager.AddBuffer<SkinnedMeshToRigIndex>(skinEntity);
//            dstManager.AddBuffer<BindPose>(skinEntity);
//            dstManager.AddBuffer<SkinMatrix>(skinEntity);
//
//            var skeletonIndexArray = dstManager.GetBuffer<SkinnedMeshToRigIndex>(skinEntity);
//            var bindPoseArray = dstManager.GetBuffer<BindPose>(skinEntity);
//            var skinMatrices = dstManager.GetBuffer<SkinMatrix>(skinEntity);
//
//            var smBones = meshRenderer.bones;
//            skeletonIndexArray.ResizeUninitialized(smBones.Length);
//            bindPoseArray.ResizeUninitialized(smBones.Length);
//            skinMatrices.ResizeUninitialized(smBones.Length);
//
//            //GameDebug.Log("skin smBones");
//            //for (int i = 0; i < smBones.Length; i++)
//            //{
//            //    var relativePath = RigGenerator.ComputeRelativePath(smBones[i], transform);
//            //    var id = (StringHash)relativePath;
//            //    GameDebug.Log("  " + i + ":" + id.Id + " path:" + relativePath);
//            //}
//
//            for (int j = 0; j != smBones.Length; ++j)
//            {
//                var remap = new SkinnedMeshToRigIndex { Value = -1 };
//
//                var smBoneRelativePath = RigGenerator.ComputeRelativePath(smBones[j], partAuthoring.transform);
//                var smBoneId = (StringHash)smBoneRelativePath;
//
//                for (int k = 0; k != RigDefinition.Bones.Length; ++k)
//                {
//                    var relativePath = RigGenerator.ComputeRelativePath(RigDefinition.Bones[k], RigDefinition.transform);
//                    var id = (StringHash)relativePath;
//
//                    if (smBoneId.Equals(id))
//                    {
//                        remap.Value = k;
//                        break;
//                    }
//                }
//                skeletonIndexArray[j] = remap;
//
//                var bindPose = meshRenderer.sharedMesh.bindposes[j];
//                bindPoseArray[j] = new BindPose { Value = bindPose };
//
//                var skinMat = math.mul(meshRenderer.bones[j].localToWorldMatrix, bindPose);
//                skinMatrices[j] = new SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
//            }
//
//            foreach (var rendererEntity in conversionSystem.GetEntities(meshRenderer))
//            {
//                if (dstManager.HasComponent<SkinnedEntityReference>(rendererEntity))
//                {
//                    dstManager.SetComponentData(rendererEntity, new SkinnedEntityReference { Value = skinEntity });
//                }
//            }
//        }
//    }
//    
//    
//
//
//    Transform MapTransform(Transform root, Transform path)
//    {
//        var pathList = new List<Transform>();
//        do
//        {
//            pathList.Add(path);
//            path = path.parent;
//        } while (path != null);
//
//
//        var mapped = root;
//        for (int i = pathList.Count - 2; i >= 0; i--)
//        {
//            mapped = mapped.Find(pathList[i].name);
//            if (mapped == null)
//            {
//                return null;
//            }
//
//        }
//        return mapped;
//    }
//}
//
//#endif
