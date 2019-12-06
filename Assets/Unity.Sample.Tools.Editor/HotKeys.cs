using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

//
// A bunch of useful functions mapped to hotkeys by using the MenuItem attribute
//

public class HotKeys
{
    [MenuItem("A2/Hotkeys/Deselect All &d")]
    static void Deselect()
    {
        Selection.activeGameObject = null;
    }

    [MenuItem("A2/Hotkeys/Toggle Gizmos _%G")]
    static void ToggleGizmos()
    {
        var etype = typeof(Editor);

        var annotation = etype.Assembly.GetType("UnityEditor.Annotation");
        var scriptClass = annotation.GetField("scriptClass");
        var classID = annotation.GetField("classID");

        var annotation_util = etype.Assembly.GetType("UnityEditor.AnnotationUtility");
        var getAnnotations = annotation_util.GetMethod("GetAnnotations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var setGizmoEnable = annotation_util.GetMethod("SetGizmoEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var setIconEnabled = annotation_util.GetMethod("SetIconEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var iconSize = annotation_util.GetProperty("iconSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showGrid = annotation_util.GetProperty("showGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showSelectionOutline = annotation_util.GetProperty("showSelectionOutline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showSelectionWire = annotation_util.GetProperty("showSelectionWire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var annotations = getAnnotations.Invoke(null, null) as System.Array;
        foreach (var a in annotations)
        {
            int cid = (int)classID.GetValue(a);
            string cls = (string)scriptClass.GetValue(a);
            setGizmoEnable.Invoke(null, new object[] { cid, cls, s_GizmoEnabled ? 1 : 0 });
            setIconEnabled.Invoke(null, new object[] { cid, cls, s_GizmoEnabled ? 1 : 0 });
        }
        s_GizmoEnabled = !s_GizmoEnabled;
        return;
//        Commenting unreachable code to reduce log spam
//        if (s_GizmoEnabled)
//        {
//            s_PreviewIconSize = (float)iconSize.GetValue(null, null);
//            s_PreviewShowGrid = (bool)showGrid.GetValue(null, null);
//            s_PreviewShowSelectionOutline = (bool)showSelectionOutline.GetValue(null, null);
//            s_PreviewShowSelectionWire = (bool)showSelectionWire.GetValue(null, null);
//
//            iconSize.SetValue(null, 0.0f, null);
//            showGrid.SetValue(null, false, null);
//            showSelectionOutline.SetValue(null, false, null);
//            showSelectionWire.SetValue(null, false, null);
//        }
//        else
//        {
//            iconSize.SetValue(null, s_PreviewIconSize, null);
//            showGrid.SetValue(null, s_PreviewShowGrid, null);
//            showSelectionOutline.SetValue(null, s_PreviewShowSelectionOutline, null);
//            showSelectionWire.SetValue(null, s_PreviewShowSelectionWire, null);
//        }
//        s_GizmoEnabled = !s_GizmoEnabled;
    }

    private static string k_EditorPrefScreenshotPath = "ScreenshotPath";
    [MenuItem("A2/Take screenshot")]
    public static void CaptureScreenshot()
    {
        var path = UnityEditor.EditorPrefs.GetString(k_EditorPrefScreenshotPath, Application.dataPath.BeforeLast("Assets"));
        var filename = EditorUtility.SaveFilePanel("Save screenshot", path, "sample_shot.png", "png");

        // Check if user cancelled
        if (filename == "")
            return;

        UnityEditor.EditorPrefs.SetString(k_EditorPrefScreenshotPath, System.IO.Path.GetDirectoryName(filename));
        ScreenCapture.CaptureScreenshot(filename, 1);
    }

    static bool s_GizmoEnabled = true;
//    static float s_PreviewIconSize = 0.0f;
//    static bool s_PreviewShowGrid = false;
//    static bool s_PreviewShowSelectionOutline = false;
//    static bool s_PreviewShowSelectionWire = false;



    [MenuItem("A2/Hotkeys/Group Under New Parent &G", false)]
    static void GroupUnderNewParent()
    {
        List<GameObject> newSelectionList = new List<GameObject>();
        Transform[] selectionTransforms = Selection.gameObjects.Select(f => f.transform).ToArray();
        //Find closest shared parent.

        Transform closestSharedParentTransform = FindClosestSharedParent(selectionTransforms);

        Vector3 centerPosition = GetCenterPosition(selectionTransforms);
        bool isSingleParent = IsSingleParent(closestSharedParentTransform, selectionTransforms);
        var newParent = new GameObject("Group_");
        Undo.RegisterCreatedObjectUndo(newParent, "Undo GroupUnderNewParent");
        newParent.transform.parent = closestSharedParentTransform;
        if (isSingleParent)
        {
//            Debug.Log(selectionTransforms[selectionTransforms.Length-1].name+" - sibling index="+selectionTransforms[selectionTransforms.Length-1].GetSiblingIndex());
            newParent.transform.SetSiblingIndex(selectionTransforms[selectionTransforms.Length-1].GetSiblingIndex()+1);
//            Debug.Log(newParent.name+" - sibling index="+newParent.transform.GetSiblingIndex());
        }
        newParent.transform.position = centerPosition;
        newParent.transform.eulerAngles = Vector3.zero;
        newParent.transform.localScale = Vector3.one;
        Undo.RecordObjects(Selection.gameObjects, "Undo GroupUnderNewParent");
        foreach (var go in Selection.gameObjects)
        {
            Undo.SetTransformParent(go.transform, newParent.transform, "Undo GroupUnderNewParent");
        }
        var newParentArray = new GameObject[] { newParent.gameObject };
        Selection.objects = newParentArray;
        RenameGameObject();

    }

    static Transform FindClosestSharedParent(Transform[] transformArray)
    {
        List<Transform> transformList = new List<Transform>(transformArray);
        //transformList.AddRange(transformArray);
        if (transformList.Count() == 1)
            return transformList.First().parent;

        Transform firstParent = transformList.First().parent;
        if (firstParent == null)
            return null;

        transformList.Remove(transformList.First());
        var transformListCopy = new List<Transform>(transformList);

        bool allAreChildren = true;
        foreach (var transform in transformList)
        {
            if (!transform.IsChildOf(firstParent))
            {
                allAreChildren = false;
                break;
            }
            else
            {
                transformListCopy.Remove(transform);
            }
        }
        if (allAreChildren)
            return firstParent;
        if (firstParent.parent == null)
            return null;
        transformListCopy.Insert(0, firstParent.parent);
        return (FindClosestSharedParent(transformListCopy.ToArray()));
    }

    static Vector3 GetCenterPosition(Transform[] transformArray)
    {
        var centerPosition=Vector3.zero;
        foreach (var transform in transformArray)
        {
            if (centerPosition == Vector3.zero)
                centerPosition = transform.position;
            else
            {
                centerPosition += transform.position;
            }
        }

        centerPosition = centerPosition / transformArray.Length;
        return centerPosition;
    }

    static bool IsSingleParent(Transform parent, Transform[] transformArray)
    {
        foreach (var transform in transformArray)
        {
            if (transform.parent != parent)
                return false;

        }
        return true;
    }


    static double renameTime;

    public static void RenameGameObject()
    {
        renameTime = EditorApplication.timeSinceStartup + 0.2f;
        EditorApplication.update += EngageRenameModeA;// enter EngageRenameModeA with a 0,2s delay to allow editor to finish creating gameObject

    }
    private static void EngageRenameModeA()
    {
        if (EditorApplication.timeSinceStartup >= renameTime)
        {
            EditorApplication.update -= EngageRenameModeA;
            renameTime = EditorApplication.timeSinceStartup + 0.25f;

            var hierarchyWindow=GetHierarchyWindow();



            EditorApplication.update += EngageRenameModeB;// enter EngageRenameModeA with a 0,2s delay to allow editor to finish focusing on the hierarchy window
        }
    }

    private static void EngageRenameModeB()
    {
        if (EditorApplication.timeSinceStartup >= renameTime)
        {
            EditorApplication.update -= EngageRenameModeB;

            var hierarchyWindow=GetHierarchyWindow();

            Event renameEvent = new Event() { keyCode = KeyCode.F2, type = EventType.KeyDown };
            hierarchyWindow.SendEvent(renameEvent);
        }
    }

    private static EditorWindow GetHierarchyWindow()
    {
        var hierarchyWindow=EditorWindow.focusedWindow;

        if (!hierarchyWindow.ToString().Contains("SceneHierarchyWindow"))
        {
            Event switchToHierarchyEvent = new Event() { keyCode = KeyCode.Alpha4, control=true,type = EventType.KeyDown };
            hierarchyWindow.SendEvent(switchToHierarchyEvent);
            hierarchyWindow=EditorWindow.focusedWindow;
        }

        hierarchyWindow.Focus();
        return hierarchyWindow;
    }




}
