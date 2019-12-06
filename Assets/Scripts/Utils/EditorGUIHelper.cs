#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorGUIHelper
{
#if UNITY_EDITOR
    public static bool EditorPrefToggle(string editorPrefName, string label)
    {
        var value = EditorPrefs.GetBool(editorPrefName);
        var newValue = EditorGUILayout.Toggle(label, value);
        if(newValue != value)
            EditorPrefs.SetBool(editorPrefName,newValue);
        return newValue;
    }


    public static string EditorPrefTextField(string label, string editorPrefKey)
    {
        var str = EditorPrefs.GetString(editorPrefKey);
        str = EditorGUILayout.TextField(label, str);
        str = str.Trim();
        EditorPrefs.SetString(editorPrefKey, str);
        return str;
    }
#endif
}
