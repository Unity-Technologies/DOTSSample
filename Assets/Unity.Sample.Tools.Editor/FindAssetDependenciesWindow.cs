using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class FindAssetDependenciesWindow : EditorWindow
{
    private GUIStyle m_ReferenceStyle;

    private enum Mode
    {
        FindAssetDependencies,
        FindAssetsThatDependOn
    }
    private Mode m_Mode = Mode.FindAssetDependencies;
    private Vector2 m_ScrollViewPos;

    [MenuItem("A2/Windows/Find Asset Dependencies")]
    static void OpenWindow()
    {
        GetWindow<FindAssetDependenciesWindow>("Find Asset Dependencies");
    }


    class DependencyData
    {
        public string AssetPath;
        public List<string> Dependencies = new List<string>();
    }

    private List<DependencyData> m_DependencyData = new List<DependencyData>();


    private void OnEnable()
    {
        m_ReferenceStyle = SelectionHistoryWindow.CreateObjectReferenceStyle();
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();

        var newMode = (Mode)EditorGUILayout.EnumPopup("Mode",m_Mode);
        if (newMode != m_Mode)
        {
            m_Mode = newMode;
            m_DependencyData.Clear(); // Clear data as it is no longer valid for current mode
        }


        if (GUILayout.Button("Find"))
        {
            if (m_Mode == Mode.FindAssetDependencies)
            {
                FindAssetDependencies();
            }
            else
            {
                FindAssetThatDependsOn();
            }
        }
        GUILayout.EndHorizontal();

        m_ScrollViewPos = GUILayout.BeginScrollView(m_ScrollViewPos);
        foreach (var data in m_DependencyData)
        {
            var o = AssetDatabase.LoadAssetAtPath<Object>(data.AssetPath);
            SelectionHistoryWindow.DrawObjectReference(o, data.AssetPath,m_ReferenceStyle);
            foreach (var dependency in data.Dependencies)
            {
                GUILayout.BeginHorizontal();
                var arrow = m_Mode == Mode.FindAssetDependencies ? "  <-" : "  ->";
                GUILayout.Label(arrow, GUILayout.Width(40));
                o = AssetDatabase.LoadAssetAtPath<Object>(dependency);
                SelectionHistoryWindow.DrawObjectReference(o, dependency,m_ReferenceStyle);
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select assets WITH dependencies"))
        {
            var select = new List<Object>();
            foreach (var data in m_DependencyData)
            {
                if (data.Dependencies.Count == 0)
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(data.AssetPath);
                select.Add(asset);
            }
            Selection.objects = select.ToArray();
            Repaint();
        }

        if (GUILayout.Button("Select assets with NO dependencies"))
        {
            var select = new List<Object>();
            foreach (var data in m_DependencyData)
            {
                if (data.Dependencies.Count > 0)
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(data.AssetPath);
                select.Add(asset);
            }
            Selection.objects = select.ToArray();
            Repaint();
        }
        GUILayout.EndHorizontal();






    }

    void FindAssetDependencies()
    {
        m_DependencyData.Clear();
        foreach (var selected in Selection.objects)
        {
            if (!AssetDatabase.IsMainAsset(selected))
                continue;

            var path = AssetDatabase.GetAssetPath(selected);
            var dependencies = new List<string>(AssetDatabase.GetDependencies(path));
            dependencies.Remove(path); // GetDependencies return asset itself

            var data = new DependencyData()
            {
                AssetPath = path,
                Dependencies = dependencies,
            };
            m_DependencyData.Add(data);
        }

        // Sort dependency lists
        foreach (var data in m_DependencyData)
        {
            data.Dependencies.Sort();
        }
    }

    void FindAssetThatDependsOn()
    {
        // Build dependency list
        m_DependencyData.Clear();
        var selectedPaths = new List<string>();
        foreach (var selected in Selection.objects)
        {
            if (!AssetDatabase.IsMainAsset(selected))
                continue;

            var path = AssetDatabase.GetAssetPath(selected);
            if (AssetDatabase.IsValidFolder(path))
                continue;

            var data = new DependencyData()
            {
                AssetPath = path,
            };
            m_DependencyData.Add(data);

            selectedPaths.Add(path);
        }

        // Iterate all assets
        var guids = AssetDatabase.FindAssets("t:Object");
        for(int i=0;i<guids.Length;i++)
        {
            var guid = guids[i];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Searching", "Asset:" + path + " " + i + "/" + guids.Length, (float)i / (float)guids.Length);

            if (selectedPaths.Contains(path))
                continue;

            var dependencies = AssetDatabase.GetDependencies(path);
            foreach (var dependency in dependencies)
            {
                if (dependency.Equals(path))
                    continue;

                var index = selectedPaths.IndexOf(dependency);
                if (index != -1)
                {
                    m_DependencyData[index].Dependencies.Add(path);
                }
            }
        }


        // Sort dependency lists
        foreach (var data in m_DependencyData)
        {
            data.Dependencies.Sort();
        }

        EditorUtility.ClearProgressBar();
    }
}
