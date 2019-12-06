using System.Collections;
using System.Collections.Generic;
using Unity.Sample.Core;
using UnityEditor;
using UnityEngine;


public class FindPrefab : EditorWindow
{
    private string componentType = "";
    private List<Object> foundList = new List<Object>();
    private GUIStyle referenceStyle;
    private Vector2 scrollPos;

    [MenuItem("A2/Windows/Find Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<FindPrefab>(false, "Find Prefab", true);
    }


    void OnGUI()
    {
        if (referenceStyle == null)
        {
            referenceStyle = SelectionHistoryWindow.CreateObjectReferenceStyle();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Required component");
        componentType = GUILayout.TextField(componentType);
        GUILayout.EndHorizontal();

        GUILayout.Label("!Currently only searches in project folder!");

        if (GUILayout.Button("Find"))
        {
            foundList.Clear();
            var guids = AssetDatabase.FindAssets("t:GameObject");
            var i = 1;
            foreach (var guid in guids)
            {
                EditorUtility.DisplayProgressBar("Scanning", "Prefab:" + i + "/" + guids.Length, (float)i / (float)guids.Length);

                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                foreach (var component in go.GetComponentsInChildren<Component>())
                {
                    if (component == null)
                    {
                        GameDebug.LogError("Prefab " + path + " has null component");
                        continue;
                    }

                    var type = component.GetType();
                    if (type.Name == componentType)
                    {
                        foundList.Add(go);
                        break;
                    }
                }

                i++;
            }
            EditorUtility.ClearProgressBar();
        }

        GUILayout.Label("Found:" + foundList.Count);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var found in foundList)
        {
            SelectionHistoryWindow.DrawObjectReference(found, found.name,referenceStyle);
        }
        EditorGUILayout.EndScrollView();

    }

}
