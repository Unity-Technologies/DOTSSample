using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Unity.Build;
using Unity.Build.Common;
using Unity.Sample.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public class OpenScene 
{

    
    public class OpenSceneFromRecent
    {
        // open the window from the menu item Example -> GUI Color
        [MenuItem("File/Open Recent Scene...", false,0)]
        static void Init()
        {
            ShowMenu();
        }

        static void ShowMenu()
        {
            GenericMenu menu = new GenericMenu();            

            var sceneGuids = SceneHistory.GetResentSceneGuids();
            foreach (var sceneGuid in sceneGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
                var filename = Path.GetFileName(path);
                var content = new GUIContent(filename);
                menu.AddItem(content,false,OpenScene,path);
            }
            
            menu.DropDown(new Rect(200,200,1,1));
        }

        static void OpenScene(object o)
        {
            var path = (string) o;
            EditorSceneManager.OpenScene(path);
        }
    }
    
    
    public class OpenSceneFromBuildSettings
    {
        // open the window from the menu item Example -> GUI Color
        [MenuItem("File/Open Scene From BuildSettings...", false,1)]
        static void Init()
        {
            ShowMenu();
        }

        static void ShowMenu()
        {
            GenericMenu menu = new GenericMenu();

            var scenePaths = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Unity.Build.BuildSettings");
            foreach (var guid in guids)
            {
                var buildSettingsPath = AssetDatabase.GUIDToAssetPath(guid);
                var buildSettings = AssetDatabase.LoadAssetAtPath<BuildSettings>(buildSettingsPath);
                var sceneList = buildSettings.GetComponent<SceneList>();

                foreach (var scenePath in sceneList.GetScenePathsForBuild())
                {
                    if (scenePaths.Contains(scenePath))
                        continue;

                    var filename = Path.GetFileName(scenePath);
                    var content = new GUIContent(filename);
                    menu.AddItem(content,false,OpenScene,scenePath);
                    scenePaths.Add(scenePath);
                }
            }
            
            menu.DropDown(new Rect(200,200,1,1));
        }

        static void OpenScene(object o)
        {
            var path = (string) o;
            EditorSceneManager.OpenScene(path);
        }
    }
    
    
    
//    public class OpenSceneFromRecent : EditorWindow
//    {
//        List<string> m_SceneGuids = new List<string>(); 
//        
//        // open the window from the menu item Example -> GUI Color
//        [MenuItem("File/Open Scene/Recent...")]
//        static void Init()
//        {
//            
//            
//            
//           // var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
//            var window = ScriptableObject.CreateInstance(typeof(OpenSceneFromRecent)) as OpenSceneFromRecent;
//            window.FindScenes();
//            window.position = new Rect(50f, 50f, 200f,window.GetWindowHeight());
//            window.ShowPopup();
//        }
//
//        private void OnLostFocus()
//        {
//            Close();
//        }
//
//        public float GetWindowHeight()
//        {
//            return m_SceneGuids.Count * 24f;
//        }
//
//        public void FindScenes()
//        {
//            m_SceneGuids = SceneHistory.GetResentSceneGuids();
//        }
//        
//        void OnGUI()
//        {
//            foreach (var sceneGuid in m_SceneGuids)
//            {
//                var path = AssetDatabase.GUIDToAssetPath(sceneGuid);
//                if (GUILayout.Button(path, EditorStyles.linkLabel))
//                {
//                    EditorSceneManager.OpenScene(path);
//                    Close();
//                }
//            }
//        }
//    }


    [InitializeOnLoad]
    class SceneHistory
    {
        private const string m_PrefKey = "SceneHistory";
        
        static SceneHistory()
        {
            EditorSceneManager.sceneOpened += EditorSceneManagerOnSceneOpened;
        }

        private static void EditorSceneManagerOnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            var guids = GetResentSceneGuids();

            var sceneGuid = AssetDatabase.AssetPathToGUID(scene.path);
            guids.Remove(sceneGuid);
            guids.Insert(0,sceneGuid);

            // trim list to max 8
            while(guids.Count > 8)
                guids.RemoveAt(guids.Count - 1);
            
//            foreach (var guid in guids)
//            {
//                var path = AssetDatabase.GUIDToAssetPath(guid);
//                GameDebug.Log(path);
//            }
            
            StoreResentSceneGuids(guids);
        }

        public static List<string> GetResentSceneGuids()
        {
            var prefString = EditorPrefs.GetString(m_PrefKey, "");
            var sceneGuidArray = prefString.Split(',');
            var sceneGuids = new List<string>(sceneGuidArray);
            sceneGuids.Remove("");
            return sceneGuids;
        }

        static void StoreResentSceneGuids(List<string> guids)
        {
            var prefString = string.Join(",", guids);
            EditorPrefs.SetString(m_PrefKey, prefString);
        }
        
    }
}
