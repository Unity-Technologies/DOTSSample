using System;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

[System.Serializable]
public struct TeleporterPresentationData : IComponentData
{
    [NonSerialized]
    [GhostDefaultField]
    public int effectTick;
}

[DisallowMultipleComponent]
public class TeleporterPresentation : ComponentDataProxy<TeleporterPresentationData>
{
}
