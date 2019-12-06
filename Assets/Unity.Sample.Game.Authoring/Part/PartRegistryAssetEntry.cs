using System;
using System.Collections.Generic;
using UnityEngine;

public class PartRegistryAssetEntry : MonoBehaviour
{
    public WeakAssetReference Asset;

    public int CategoryIndex;
    public int PartIndex;

    public int BuildTypeFlags = (int)BuildType.Client;
    public int LODFlags;
    public int RigFlags;
}
