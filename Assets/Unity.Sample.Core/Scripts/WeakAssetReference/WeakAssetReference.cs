using System;
using System.Collections.Generic;
using Unity.Sample.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum BuildType
{
    Client = 1 << 0,
    Server = 1 << 1,
}


public interface IBundledAssetProvider
{
    void AddBundledAssets(BuildType buildType, List<WeakAssetReference> assets);
}

/// <summary>
/// Use this attribute to limit the types allowed on a weak asset reference field
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class AssetTypeAttribute : Attribute
{
    public Type assetType;

    public AssetTypeAttribute(Type t)
    {
        assetType = t;
    }
}

/// <summary>
/// Weak asset reference that does not result in assets getting pulled in. Use has
/// responsibility to find another way to actually get asset loaded
/// </summary>
[System.Serializable]
public struct WeakAssetReference
{
    public static WeakAssetReference Default  => new WeakAssetReference { };

    public int val0;
    public int val1;
    public int val2;
    public int val3;

    public WeakAssetReference(string guid)
    {
        var g = new Guid(guid);
        byte[] gb = g.ToByteArray();
        val0 = BitConverter.ToInt32(gb, 0);
        val1 = BitConverter.ToInt32(gb, 4);
        val2 = BitConverter.ToInt32(gb, 8);
        val3 = BitConverter.ToInt32(gb, 12);
    }

    public WeakAssetReference(int val0, int val1, int val2, int val3)
    {
        this.val0 = val0;
        this.val1 = val1;
        this.val2 = val2;
        this.val3 = val3;
    }

    public static bool operator==(WeakAssetReference x, WeakAssetReference y)
    {
        return x.val0 == y.val0 && x.val1 == y.val1 && x.val2 == y.val2 && x.val3 == y.val3;
    }

    public static bool operator!=(WeakAssetReference x, WeakAssetReference y)
    {
        return !(x == y);
    }

    public bool IsSet()
    {
        return val0 != 0 || val1 != 0 || val2 != 0 || val3 != 0;
    }

    public Guid GetGuid()
    {
        byte[] gb = new byte[16];

        byte[] buf;
        buf = BitConverter.GetBytes(val0);
        Array.Copy(buf, 0, gb, 0, 4);
        buf = BitConverter.GetBytes(val1);
        Array.Copy(buf, 0, gb, 4, 4);
        buf = BitConverter.GetBytes(val2);
        Array.Copy(buf, 0, gb, 8, 4);
        buf = BitConverter.GetBytes(val3);
        Array.Copy(buf, 0, gb, 12, 4);

        return new Guid(gb);
    }

    public string ToGuidStr()
    {
        return GetGuid().ToString("N");
    }

#if UNITY_EDITOR
    public T LoadAsset<T>() where T : UnityEngine.Object
    {
        var path = AssetDatabase.GUIDToAssetPath(ToGuidStr());
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }



#endif

    public override bool Equals(object obj)
    {
        if (!(obj is WeakAssetReference))
        {
            return false;
        }

        var reference = (WeakAssetReference)obj;
        return val0 == reference.val0 &&
               val1 == reference.val1 &&
               val2 == reference.val2 &&
               val3 == reference.val3;
    }

    public override int GetHashCode()
    {
        var hashCode = -345130910;
        hashCode = hashCode * -1521134295 + val0.GetHashCode();
        hashCode = hashCode * -1521134295 + val1.GetHashCode();
        hashCode = hashCode * -1521134295 + val2.GetHashCode();
        hashCode = hashCode * -1521134295 + val3.GetHashCode();
        return hashCode;
    }
}

// This base is here to allow CustomPropertyDrawer to pick it up
[System.Serializable]
public class WeakBase
{
    public string guid = "";
}

// Derive from this to create a typed weak asset reference
[System.Serializable]
public class Weak<T> : WeakBase
{
}

#if UNITY_EDITOR


public class WeakAssetReferenceCollection
{
    private List<WeakAssetReference> references = new List<WeakAssetReference>();

    public List<WeakAssetReference> References
    {
        get { return references; }
    }

    public void AddReference(WeakAssetReference reference)
    {
        if (!reference.IsSet())
            return;

        if (references.Contains(reference))
            return;

        var path = AssetDatabase.GUIDToAssetPath(reference.ToGuidStr());
        if (String.IsNullOrEmpty(path))
        {
            GameDebug.LogWarning("Asset does not exist:" + reference.ToGuidStr());
            return;
        }

        GameDebug.Log("Adding asset:" + reference.ToGuidStr() + " " + path);

        references.Add(reference);
    }

    public void ResolveDerivedDependencies(BuildType buildType)
    {
        GameDebug.Log("Resolving derived dependencies");

        int i = 0;
        while (i < references.Count)
        {
            EditorUtility.DisplayProgressBar("Resolve derived dependencies",
                "Weakassetreference " + i + "/" + references.Count, (float) i / references.Count);

            var path = AssetDatabase.GUIDToAssetPath(references[i].ToGuidStr());
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if(go != null)
                AddDerivedAsserts(go, buildType);

            i++;
        }

        EditorUtility.ClearProgressBar();
    }

    void AddDerivedAsserts(GameObject go, BuildType buildType)
    {
        foreach (var component in go.GetComponentsInChildren<Component>())
        {
            var provider = component as IBundledAssetProvider;
            if (provider != null)
            {
                var refs = new List<WeakAssetReference>();
                provider.AddBundledAssets(buildType, refs);
                foreach (var reference in refs)
                {
                    AddReference(reference);
                }
            }
        }
    }

//
//
//    void AddDerivedAsserts(GameObject go)
//    {
////        Debug.Log("AddDerivedAsserts:" + go);
//        if (go == null)
//            return;
//
//        var monoBehaviours = go.GetComponents<MonoBehaviour>();
//        foreach (var monoBehaviour in monoBehaviours)
//        {
//            if (monoBehaviour == null)
//                continue;
//
////            Debug.Log(" monoBehaviours:" + monoBehaviour);
//            var serialized = new SerializedObject(monoBehaviour);
//
//            // TODO (mogensh) get someone to explain to me how this SerializedProperty stuff works - because I am just confuddled
//            SerializedProperty property = serialized.GetIterator();
//            while (property.NextVisible(true))
//            {
//                if (property.propertyType != SerializedPropertyType.Generic)
//                    continue;
//
////                Debug.Log("  property:" + property.name);
//
//                if (property.type != "WeakAssetReference")
//                    continue;
//
//                System.Object parentObject = property.serializedObject.targetObject;
//                var parentType = parentObject.GetType();
//                System.Reflection.FieldInfo field = null;
//                string[] splitFieldPath = property.propertyPath.Split('.');
//                for(int i=0;i<splitFieldPath.Length;i++)
//                {
//                    var fieldName = splitFieldPath[i];
//                    field = parentType.GetField(fieldName);
//
//                    if (field == null)
//                    {
////                        GameDebug.LogError("Failed to parse field. Object:" + monoBehaviour + " field:" + property.propertyPath);
//                        break;
//                    }
//
//
//                    if (i <  splitFieldPath.Length - 1)
//                    {
//                        parentType = field.FieldType;
//                        parentObject = field.GetValue(parentObject);
//                    }
//                }
//
//                if (field != null)
//                {
//                    if (field.FieldType == typeof(WeakAssetReference))
//                    {
//                        var dontDerive = (DontDeriveAssetAttribute) Attribute.GetCustomAttribute(field, typeof (DontDeriveAssetAttribute));
//                        if (dontDerive != null)
//                        {
//                            continue;
//                        }
//
//
//
//                        var value = field.GetValue(parentObject);
//                        var weakAssetRef = (WeakAssetReference) value;
//                        AddReference(weakAssetRef);
//                        continue;
//                    }
//
//                    if (field.FieldType == typeof(WeakAssetReference[]))
//                    {
//                        var value = field.GetValue(parentObject);
//                        var weakAssetRefs = (WeakAssetReference[]) value;
//
//                        for (int i = 0; i < weakAssetRefs.Length; i++)
//                        {
//                            AddReference(weakAssetRefs[i]);
//                        }
//                        continue;
//                    }
//                }
//            }
//        }
//
//        // Check children
//        for (int i = 0; i < go.transform.childCount; i++)
//        {
//            AddDerivedAsserts(go.transform.GetChild(i).gameObject);
//        }
//    }

}


#endif
