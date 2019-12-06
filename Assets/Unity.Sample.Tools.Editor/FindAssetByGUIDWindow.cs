using UnityEditor;
using UnityEngine;

class FindAssetByGUIDWindow : EditorWindow
{
    [MenuItem("A2/Windows/Find assets by GUID")]
    static void OpenWindow()
    {
        GetWindow<FindAssetByGUIDWindow>("Find assets by GUID");
    }

    string m_GUID;
    string m_AssetPath;
    Object m_MainAsset;
    Object[] m_AllAssets;

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        m_GUID = EditorGUILayout.TextField("GUID", m_GUID);
        if (EditorGUI.EndChangeCheck())
        {
            m_AssetPath = AssetDatabase.GUIDToAssetPath(m_GUID);
            if (string.IsNullOrEmpty(m_AssetPath))
            {
                m_MainAsset = null;
                m_AllAssets = null;
            }
            else
            {
                m_MainAsset = AssetDatabase.LoadMainAssetAtPath(m_AssetPath);
                m_AllAssets = AssetDatabase.LoadAllAssetsAtPath(m_AssetPath);
            }
        }

        if (!string.IsNullOrEmpty(m_AssetPath))
        {
            GUILayout.Label("Asset path:");
            GUILayout.Label(m_AssetPath);
        }


        if (m_MainAsset != null)
        {
            GUILayout.Label("Main asset:");
            if (GUILayout.Button(m_MainAsset.name))
                EditorGUIUtility.PingObject(m_MainAsset);
        }

        if (m_AllAssets != null)
        {
            GUILayout.Label("Sub assets:");
            foreach (var asset in m_AllAssets)
            {
                if (AssetDatabase.IsMainAsset(asset))
                    continue;

                if (GUILayout.Button(asset.name))
                    EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
