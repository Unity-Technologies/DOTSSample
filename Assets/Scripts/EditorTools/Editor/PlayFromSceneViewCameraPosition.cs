using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public class PlayFromSceneViewCameraPosition
{
    static PlayFromSceneViewCameraPosition()
    {
        EditorApplication.playModeStateChanged += OnStateChange;        
    }

    [MenuItem("A2/Hotkeys/Play From Scene View Camera Position _F8")]
    static void EnterPlayMode()
    {
        if (EditorApplication.isPlaying) //Also use F8 to leave play mode
        {
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }


        if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || SceneView.lastActiveSceneView.camera.transform.position == Vector3.zero)
        {
            return;
        }
        var camPosition = SceneView.lastActiveSceneView.camera.transform.position;



        Vector3 spawnPosition = camPosition+new Vector3(0,1,0);
        Vector3 spawnRotation = SceneView.lastActiveSceneView.camera.transform.eulerAngles;



        Debug.Log("Set Spawn Position: " + spawnPosition);
        Debug.Log("Set Spawn Rotation: " + spawnRotation);
        PlayerPrefs.SetString("spawnPosition", spawnPosition.ToString());
        PlayerPrefs.SetString("spawnRotation", spawnRotation.ToString());
        PlayerPrefs.Save();
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }


    //Hook Enter-Exit playmode.
    static void OnStateChange(PlayModeStateChange state)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) //Only execute when player is leaving Play mode from the editor.
        {
            //Make sure to erase SpawnPosition & Rotation keys if for some reason they were not deleted.
            PlayerPrefs.DeleteKey("spawnPosition");
            PlayerPrefs.DeleteKey("spawnRotation");
        }
    }
}
