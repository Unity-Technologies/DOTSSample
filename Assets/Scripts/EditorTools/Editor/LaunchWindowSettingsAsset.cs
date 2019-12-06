using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

internal class LaunchWindowSettingsAsset : ScriptableObject
{
    [SerializeField]
    internal LaunchWindow.Data m_Data;

    public static LaunchWindowSettingsAsset Create(LaunchWindow.Data data)
    {
        var result = CreateInstance<LaunchWindowSettingsAsset>();
        var dataCopy = JsonUtility.FromJson<LaunchWindow.Data>(JsonUtility.ToJson(data));
        result.m_Data = dataCopy;
        return result;
    }
}

public class LaunchWindowSettingsAssetHandler
{
    [OnOpenAsset(1)]
    public static bool step1(int instanceID, int line)
    {
        var config = EditorUtility.InstanceIDToObject(instanceID) as LaunchWindowSettingsAsset;
        if (config != null)
        {
            LaunchWindow.OpenAsset(config);
            return true;
        }
        return false;
    }
}
