
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SoundRegistryAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public List<SoundDef> soundDefs;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var sr = new SoundRegistry();
        sr.soundDefs = soundDefs.ToArray();
        sr.assetRefs = new WeakAssetReference[sr.soundDefs.Length];
#if UNITY_EDITOR
        for(int i = 0; i < sr.soundDefs.Length; ++i)
        {
            sr.assetRefs[i] = new WeakAssetReference(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sr.soundDefs[i])));
        }
#endif
        dstManager.AddSharedComponentData(entity, sr);
    }
}

// TODO: This could become an class IComponentData and we could get rid of IEquatable requirement
[Serializable]
public struct SoundRegistry : ISharedComponentData, IEquatable<SoundRegistry>
{
    public SoundDef[] soundDefs;
    public WeakAssetReference[] assetRefs;

    public SoundDef GetSoundDef(WeakAssetReference assetRef)
    {
        if (assetRefs == null)
            return null;
        var i = assetRefs.IndexOf(assetRef);
        return i > -1 ? soundDefs[i] : null;
    }

    public bool Equals(SoundRegistry other)
    {
        if ((soundDefs == null) && (other.soundDefs != null))
            return false;
        if ((assetRefs == null) && (other.assetRefs != null))
            return false;
        if (other.soundDefs.Length != soundDefs.Length)
            return false;
        if (other.assetRefs.Length != assetRefs.Length)
            return false;
        for(int i = 0, c = assetRefs.Length; i < c; ++i)
        {
            if (!assetRefs[i].Equals(other.assetRefs[i]))
                return false;
        }
        for (int i = 0, c = soundDefs.Length; i < c; ++i)
        {
            if (!soundDefs[i].Equals(other.soundDefs[i]))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        if (assetRefs == null)
            return 0;
        int h = 0;
        foreach (var a in assetRefs)
            h ^= a.GetHashCode();
        return h;
    }
}
