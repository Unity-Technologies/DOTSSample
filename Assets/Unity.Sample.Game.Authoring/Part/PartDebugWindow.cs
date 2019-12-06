using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class PartDebugWindow : EditorWindow
{
    static EntityQuery m_partOwnerQuery;
    Entity m_selectedEntity;

    [MenuItem("A2/Windows/Part Debug")]
    public static void ShowWindow()
    {
        GetWindow<PartDebugWindow>(false, "Part Debug", true);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
    }
    private static void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange obj)
    {
        if(obj == PlayModeStateChange.EnteredPlayMode)
            m_partOwnerQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PartOwner.InputState), typeof(PartOwner.RegistryAsset));
        if (obj == PlayModeStateChange.ExitingPlayMode)
        {
            m_partOwnerQuery.Dispose();
            m_partOwnerQuery = null;
        }

    }

    void OnGUI()
    {

        if (m_partOwnerQuery == null)
        {
            GUILayout.Label("Only works in play mode");
            return;
        }

        var partOwnerEntities = m_partOwnerQuery.ToEntityArray(Allocator.TempJob);

        if(partOwnerEntities.Length > 0)
        {
            var options = new string[partOwnerEntities.Length];
            var selected = 0;
            for (int i = 0; i < partOwnerEntities.Length; i++)
            {
                var name = World.DefaultGameObjectInjectionWorld.EntityManager.GetName(partOwnerEntities[i]);
                options[i] = name;

                if (partOwnerEntities[i] == m_selectedEntity)
                    selected = i;
            }

            var newSelected = EditorGUILayout.Popup("Label", selected, options);
            m_selectedEntity = partOwnerEntities[newSelected];
        }
        partOwnerEntities.Dispose();

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (!entityManager.Exists(m_selectedEntity) || !entityManager.HasComponent<PartOwner.RegistryAsset>(m_selectedEntity))
            return;


        var registryData = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PartOwner.RegistryAsset>(m_selectedEntity);
        var path = AssetDatabase.GUIDToAssetPath(registryData.Value.ToGuidStr());
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var registryAsset = go.GetComponent<PartRegistryAuthoring>();

 //       var runtimeRegistry = PartRegistry.GetPartRegistry(registryData.Value);

        var partOwnerInput = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<PartOwner.InputState>(m_selectedEntity);
        var partIds = new int[registryAsset.Categories.Count];
        registryAsset.UnpackPartsList(partOwnerInput.PackedPartIds, partIds);

        EditorGUI.BeginChangeCheck();



        for (int categoryIndex = 0; categoryIndex < registryAsset.Categories.Count; categoryIndex++)
        {
            var partCount = registryAsset.Categories[categoryIndex].Parts.Count;
            var currentPartId = partIds[categoryIndex];

            var options = new string[partCount + 1];
            options[0] = "<none>";
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                var part = registryAsset.Categories[categoryIndex].Parts[partIndex];
                var partId = partIndex + 1;

                options[partId] = part.Name;
            }

            partIds[categoryIndex] = EditorGUILayout.Popup(registryAsset.Categories[categoryIndex].Name, currentPartId, options);
        }



//        for (int i = 0; i < registryAsset.Categories.Count; i++)
//        {
//            int max = registryAsset.Categories[i].Parts.Count;
//            partIds[i] = EditorGUILayout.IntSlider(registryAsset.Categories[i].Name, partIds[i], 0, max);
//        }

        var change = EditorGUI.EndChangeCheck();

        if (change)
        {
            partOwnerInput.PackedPartIds = registryAsset.PackPartsList(partIds);
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(m_selectedEntity,partOwnerInput);
        }
    }
}
