using System;
using System.Collections.Generic;
using System.IO;
using Unity.Sample.Core;
using UnityEditor;
using UnityEngine;
using Directory = UnityEngine.Windows.Directory;

public class MoveAssetAndSourceFolderWindow : EditorWindow
{
    private string sourceFolderRoot = "SourceAssets";
    private string targetAssetFolder = "Assets";

    struct MoveData
    {
        public string assetFolderStartPath;
        public string assetFolderEndPath;
        public string sourceFolderStartPath;
        public string sourceFolderEndPath;
        public bool sourceExists;
    }

    private List<MoveData> moves = new List<MoveData>();

    [MenuItem("A2/Windows/Move Asset And Source Folder")]
    static void OpenWindow()
    {
        GetWindow<MoveAssetAndSourceFolderWindow>("Move Asset+Source folder");
    }

    MoveAssetAndSourceFolderWindow()
    {
        Selection.selectionChanged += () => Repaint();
    }

    void OnGUI()
    {
        // Sample move data
        moves.Clear();
        foreach (var selected in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(selected);
            if (!AssetDatabase.IsValidFolder(path))
                continue;

            var moveData = new MoveData();
            moveData.assetFolderStartPath = path;
            moveData.assetFolderEndPath = targetAssetFolder + "/" + Path.GetFileName(moveData.assetFolderStartPath);

            moveData.sourceFolderStartPath = path.Replace("Assets", sourceFolderRoot);
            moveData.sourceFolderEndPath = targetAssetFolder + "/" + Path.GetFileName(moveData.sourceFolderStartPath);

            var projectFolder = Application.dataPath.Replace("/Assets", "");
            var sourceFullPath = projectFolder + "/" + moveData.sourceFolderStartPath;

            moveData.sourceExists = Directory.Exists(sourceFullPath);
            moves.Add(moveData);

        }


        GUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Target asset folder:", GUILayout.Width(120));
        if(GUILayout.Button(targetAssetFolder,EditorStyles.textField))
        {
            var newFolder = EditorUtility.OpenFolderPanel("Select target folder", targetAssetFolder, "");
            if (newFolder.Contains(Application.dataPath))
            {
                targetAssetFolder = newFolder.Replace(Application.dataPath, "Assets");
            }
            else
            {
                GameDebug.LogError("Target folder must be below the project Asset folder ");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Move"))
        {
            var result = EditorUtility.DisplayDialog("Move","Are you sure you want to move","Ok");
        }



        var defaultColor = GUI.color;

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(GUILayout.Width(100));
        foreach (var move in moves)
        {
            GUILayout.Label("Asset folder:");
            GUILayout.Label("Source folder:");
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        foreach (var move in moves)
        {
            GUILayout.Label(move.assetFolderStartPath);

            GUI.color = move.sourceExists ? defaultColor : Color.red;
            GUILayout.Label(move.sourceFolderStartPath);
            GUI.color = defaultColor;
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(40));
        foreach (var move in moves)
        {
            GUILayout.Label("=>");
            GUILayout.Label("=>");
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        foreach (var move in moves)
        {
            GUILayout.Label(move.assetFolderEndPath);
            GUILayout.Label(move.sourceFolderEndPath);
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void Move()
    {

        var projectFolder = Application.dataPath.Replace("/Assets", "");

        foreach (var move in moves)
        {
            if (!move.sourceExists)
                continue;

            {
                var from = projectFolder + "/" + move.assetFolderStartPath;
                var to = projectFolder + "/" + move.assetFolderEndPath;
                FileUtil.MoveFileOrDirectory(from, to);
            }

            {
                var from = projectFolder + "/" + move.sourceFolderStartPath;
                var to = projectFolder + "/" + move.sourceFolderEndPath;
                FileUtil.MoveFileOrDirectory(from, to);
            }
        }


    }
}
