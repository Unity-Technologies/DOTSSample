using UnityEditor;

public class RevertSelectedPrefabs
{
    [MenuItem("A2/Revert Selected Prefabs")]
    static void Execute()
    {
        foreach (var gameObject in Selection.gameObjects)
            PrefabUtility.RevertPrefabInstance(gameObject,InteractionMode.AutomatedAction);
    }
}
