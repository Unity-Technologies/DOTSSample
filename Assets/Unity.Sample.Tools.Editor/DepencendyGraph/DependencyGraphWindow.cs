using System.Collections;
using System.Collections.Generic;
using Unity.Sample.Core;
using UnityEditor;
using UnityEngine;

public class DependencyGraphWindow : EditorWindow
{
    [MenuItem("A2/Windows/DependencyGraph")]
    static void Open()
    {
        GetWindow<DependencyGraphWindow>(false, "Dependency Graph", true);
    }

    void OnGUI()
    {
        if (GUILayout.Button("DUWIT!"))
        {
            var active = Selection.activeObject;


            if (active == null)
            {
                GameDebug.LogWarning("Nothing selected");
                return;
            }

            if (!AssetDatabase.IsMainAsset(active))
            {
                GameDebug.LogWarning("Object:" + active + " is not main asset");
                return;
            }



            {
                var guid = AssetDatabase.FindAssets("t:GameObject");
                for(int i=0;i<guid.Length;i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid[i]);
                    EditorUtility.DisplayProgressBar("Search gameobjects", "GameObject:" + path + " " + i + "/" + guid.Length, (float)i / guid.Length);

                    var o = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var oa = new Object[] {o};
                    var dependencies = EditorUtility.CollectDependencies(oa);
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == active)
                        {
                            GameDebug.Log("FOUND GAMEOBJECT:" + path);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }

            {
                var guid = AssetDatabase.FindAssets("t:Scene");
                for(int i=0;i<guid.Length;i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid[i]);
                    EditorUtility.DisplayProgressBar("Search scenes", "Scene:" + path + " " + i + "/" + guid.Length, (float)i / guid.Length);

                    var o = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    var oa = new Object[] {o};
                    var dependencies = EditorUtility.CollectDependencies(oa);
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == active)
                        {
                            GameDebug.Log("FOUND SCENE:" + path);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }

            {
                var guid = AssetDatabase.FindAssets("t:ScriptableObject");
                for(int i=0;i<guid.Length;i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid[i]);
                    EditorUtility.DisplayProgressBar("Search scriptable objects", "ScriptableObject:" + path + " " + i + "/" + guid.Length, (float)i / guid.Length);

                    var o = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    var oa = new Object[] {o};
                    var dependencies = EditorUtility.CollectDependencies(oa);
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == active)
                        {
                            GameDebug.Log("FOUND SCRIPTABLEOBJECT:" + path);
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }

        }
    }
}
