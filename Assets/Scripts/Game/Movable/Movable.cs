using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public struct MovableData : IComponentData
{
    [GhostDefaultField(1000, true)]
    Vector3 position;
    [GhostDefaultField(1000, true)]
    Quaternion rotation;
}

[RequireComponent(typeof(Rigidbody))]
public class Movable : ComponentDataProxy<MovableData>
{
    public void Start()
    {
        if (Game.GetGameLoop<ServerGameLoop>() == null)
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
