using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.DebugDisplay
{

    [CreateAssetMenu]
    public class DebugDisplayResources : ScriptableObject
    {
        public Material textMaterial;
        public Material graphMaterial;
        public Material lineMaterial;
        public TextAsset wide;
    }

}