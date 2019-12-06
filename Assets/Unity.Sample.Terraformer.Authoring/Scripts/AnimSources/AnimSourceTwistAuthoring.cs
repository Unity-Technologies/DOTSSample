#if UNITY_EDITOR

using Unity.Entities;

public class AnimSourceTwistAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public Unity.Animation.Hybrid.RigComponent RigReference;
    public AnimSourceTwist.BoneReferences BoneReferences;
    public AnimSourceTwist.Factors Factors;
    public bool invertTwist;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceTwist.Settings();
        settings.rigReference = RigDefinitionAsset.ConvertRig(RigReference);
        settings.boneReferences = BoneReferences;
        settings.factors = Factors;
        settings.twistMult = invertTwist ? -1f : 1f;

        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
    }
}

#endif