using UnityEngine;

[CreateAssetMenu(fileName = "PlayerModuleSettings", menuName = "A2/Player/PlayerSystemSettings")]
public class PlayerModuleSettings : ScriptableObject
{
    public WeakAssetReference playerStatePrefab;
    public WeakAssetReference localPlayerPrefab;
}
