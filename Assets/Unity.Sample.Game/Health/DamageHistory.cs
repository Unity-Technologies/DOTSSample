using System;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

[Serializable]
public struct DamageHistoryData : IComponentData
{
    [Serializable]
    public struct InflictedDamage
    {
        [GhostDefaultField]
        public int tick;
        [GhostDefaultField]
        public int lethal;
    }
    [NonSerialized] public InflictedDamage inflictedDamage;
}

public class DamageHistory : ComponentDataProxy<DamageHistoryData>
{
}
