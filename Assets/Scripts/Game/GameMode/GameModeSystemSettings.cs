using UnityEngine;

[CreateAssetMenu(fileName = "GameModeSystemSettings", menuName = "A2/GameMode/GameModeSystemSettings")]
public class GameModeSystemSettings : ScriptableObject
{
    public WeakAssetReference gameModePrefab;
    public WeakAssetReference teamObjectStatePrefab;
}
