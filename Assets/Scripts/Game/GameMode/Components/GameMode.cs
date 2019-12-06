using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

// TODO (petera) Rename this to GameModeState or something even better

// This is data is replicated to the clients about the 'global' state of
// the game mode, scores etc.

public class GameMode : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // no reason to pass over any variables as this is only
        // used from code. but we make a prefab and conversion
        // code to have it picked up by netcode
        dstManager.AddComponentData(entity, new GameModeData());
    }
}


[Serializable]
public struct GameModeData : IComponentData
{
    [GhostDefaultField]
    public int gameTimerSeconds;
    [GhostDefaultField]
    public NativeString64 gameTimerMessage;
    [GhostDefaultField]
    public NativeString64 teamName0;
    [GhostDefaultField]
    public NativeString64 teamName1;
    [GhostDefaultField]
    public int teamScore0;
    [GhostDefaultField]
    public int teamScore1;
}
