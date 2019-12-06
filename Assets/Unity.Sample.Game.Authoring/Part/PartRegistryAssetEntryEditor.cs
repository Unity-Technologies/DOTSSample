using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(PartRegistryAssetEntry))]
public class PartRegistryAssetEntryEditor : Editor
{
    private WeakAssetReference m_lastReference;
    private Object lastObject;

    public override void OnInspectorGUI()
    {
        var entry = target as PartRegistryAssetEntry;

        var registry = entry.GetComponentInParent<PartRegistryAuthoring>();

        EditorGUI.BeginChangeCheck();

        // TODO (mogensh) make general method to make WeakAssetReference field
        var guidStr = "";
        if (entry.Asset.IsSet() && m_lastReference != entry.Asset)
        {
            guidStr = entry.Asset.ToGuidStr();
            var path = AssetDatabase.GUIDToAssetPath(guidStr);

            var obj =  AssetDatabase.LoadAssetAtPath(path,typeof(GameObject));

            lastObject = obj;
            m_lastReference = entry.Asset;
        }
        GUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel( new GUIContent("Asset" + "(" + guidStr + ")"));
        var newObj = EditorGUILayout.ObjectField(lastObject, typeof(GameObject), false);
        GUILayout.EndHorizontal();
        if (newObj != lastObject)
        {
            if (newObj != null)
            {
                var path = AssetDatabase.GetAssetPath(newObj);
                entry.Asset = new WeakAssetReference(AssetDatabase.AssetPathToGUID(path));
            }
        }


        var catNames = new string[registry.Categories.Count];
        for (var i = 0; i < registry.Categories.Count; i++)
            catNames[i] = registry.Categories[i].Name;

        entry.CategoryIndex = EditorGUILayout.Popup("Category", entry.CategoryIndex, catNames);

        var parts = registry.Categories[entry.CategoryIndex].Parts;
        var partNames = new string[parts.Count];
        for (var i = 0; i < parts.Count; i++)
        {
            partNames[i] = parts[i].Name;
        }
        entry.PartIndex = EditorGUILayout.Popup("Part", entry.PartIndex, partNames);

        // Build type
        var buildTypeNames = Enum.GetNames(typeof(BuildType));
        entry.BuildTypeFlags = EditorGUILayout.MaskField("BuildTypes",entry.BuildTypeFlags, buildTypeNames);


        // Rig
        if (registry.Rigs.Count > 1)
        {
            var rigNames = new string[registry.Rigs.Count];
            for (var i = 0; i < registry.Rigs.Count; i++)
                rigNames[i] = i + ":" + registry.Rigs[i];

            entry.RigFlags = EditorGUILayout.MaskField("Rig",entry.RigFlags, rigNames);
        }

        // LOD
        if (registry.LODlevels.Count > 1)
        {
            var LODNames = new string[registry.LODlevels.Count];
            for (int i = 0; i < registry.LODlevels.Count;i++)
            {
                LODNames[i] = "LOD" + i;
            }
            entry.LODFlags = EditorGUILayout.MaskField("LOD",entry.LODFlags, LODNames);
        }

        var buildString = "Build:" + GetFlagString(entry.BuildTypeFlags, Enum.GetNames(typeof(BuildType)).Length);
        var rigString = registry.Rigs.Count > 1 ? "Rig:" + GetFlagString(entry.RigFlags, registry.Rigs.Count)  : "";
        var lodString = registry.LODlevels.Count > 1 ? "LOD:" + GetFlagString(entry.LODFlags, registry.LODlevels.Count) : "";
        var partStr = parts[entry.PartIndex].Name;

        entry.name = registry.Categories[entry.CategoryIndex].Name + "." + partStr + "<" + buildString + " " + rigString + " " + lodString + ">";

        var change = EditorGUI.EndChangeCheck();
        if (change)
        {
            EditorUtility.SetDirty(entry);
            EditorUtility.SetDirty(entry.gameObject);
            EditorUtility.SetDirty(registry.gameObject);
        }

    }



    string GetFlagString(int LOD, int lodCount)
    {
        var strBuilder = new StringBuilder();

        for (int i = 0; i < lodCount; i++)
        {
            var flag = 1 << i;

            if ((LOD & flag) != 0)
                strBuilder.Append("1");
            else
                strBuilder.Append("x");
        }

        return strBuilder.ToString();
    }
}
