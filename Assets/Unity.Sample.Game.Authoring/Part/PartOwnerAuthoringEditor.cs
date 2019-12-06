using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PartOwnerAuthoring))]
public class PartOwnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var partOwner = target as PartOwnerAuthoring;

        EditorGUI.BeginChangeCheck();

        partOwner.PartRegistryAsset =
            (GameObject)EditorGUILayout.ObjectField("PartRegistry", partOwner.PartRegistryAsset, typeof(GameObject), true);

        if (partOwner.PartRegistryAsset != null)
        {
            var registry = partOwner.PartRegistryAsset.GetComponent<PartRegistryAuthoring>();

            while(partOwner.PartIds.Count < registry.Categories.Count)
                partOwner.PartIds.Add(0);

            for (int categoryIndex = 0; categoryIndex < registry.Categories.Count; categoryIndex++)
            {
                var partCount = registry.Categories[categoryIndex].Parts.Count;
                var currentPartId = partOwner.PartIds[categoryIndex];

                var options = new string[partCount + 1];
                options[0] = "<none>";
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    var part = registry.Categories[categoryIndex].Parts[partIndex];
                    var partId = partIndex + 1;

                    options[partId] = part.Name;
                }

                var newPartId = EditorGUILayout.Popup(registry.Categories[categoryIndex].Name, currentPartId, options);

                partOwner.PartIds[categoryIndex] = newPartId;
            }
        }

        var change = EditorGUI.EndChangeCheck();

        if(change)
            EditorUtility.SetDirty(partOwner);
    }
}
