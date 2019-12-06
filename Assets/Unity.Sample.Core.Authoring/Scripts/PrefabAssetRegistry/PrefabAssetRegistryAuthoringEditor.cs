using UnityEditor;
using UnityEngine;
using Unity.NetCode;
using Unity.Sample.Core;

#if UNITY_EDITOR

[CustomEditor(typeof(PrefabAssetRegistryAuthoring))]
public class PrefabAssetRegistryAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Gather prefabs from current game config");

        var prefabAssetRef = target as PrefabAssetRegistryAuthoring;

        if (GUILayout.Button("Gather Prefabs"))
        {
            var refCollection = new WeakAssetReferenceCollection();
            GatherAssetReferences(refCollection, BuildType.Client);
            GatherAssetReferences(refCollection, BuildType.Server);



            prefabAssetRef.Assets.Clear();

            foreach (var reference in refCollection.References)
            {
                if(!reference.IsSet())
                    continue;

                var path = AssetDatabase.GUIDToAssetPath(reference.ToGuidStr());
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (asset == null)
                {
                    GameDebug.LogWarning("Loading asset:" + reference.ToGuidStr() + " failed. Not a gameobject ?");
                    continue;
                }

                prefabAssetRef.Assets.Add(asset);
            }

            EditorUtility.SetDirty(target);
        }


        var assetsProperty = serializedObject.FindProperty("Assets");
        EditorGUILayout.PropertyField(assetsProperty);

//        DrawDefaultInspector();
    }


    void GatherAssetReferences(WeakAssetReferenceCollection refCollection, BuildType buildType)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:" + typeof(GameObject).Name);
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if(go.GetComponent<GhostAuthoringComponent>() != null)
                refCollection.AddReference(new WeakAssetReference(guid));
            if(go.GetComponent<IPrefabAsset>() != null)
                refCollection.AddReference(new WeakAssetReference(guid));


        }

        refCollection.ResolveDerivedDependencies(buildType);
    }
}

#endif
