using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameApp
{
    
    public static CameraStack CameraStack
    {
        get { return m_cameraStack; }
    }

    public static bool IsInitialized;    // TODO (mogensh) this is currently owned by Game.cs (set on awake and disable), but should be tied this class
    
    static GameApp()
    {
        Initialize();

#if UNITY_EDITOR
        // In editor we need to make sure GameApp is reset when going from edit mode to playmode
        EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    private static void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange obj)
    {
        if(obj == PlayModeStateChange.ExitingEditMode)
            Initialize();
    }
#endif

    static void Initialize()
    {
        m_cameraStack = new CameraStack();
    }

    private static CameraStack m_cameraStack;

}
