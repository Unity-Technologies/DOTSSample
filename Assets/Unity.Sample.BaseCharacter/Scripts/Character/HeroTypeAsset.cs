using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "HeroType", menuName = "A2/Hero/HeroType")]
public class HeroTypeAsset : ScriptableObject
{
    [Serializable]
    public class ItemEntry
    {
        public WeakAssetReference asset;
        public byte slot;
    }

    [Serializable]
    public struct SprintCameraSettings
    {
        public float FOVFactor;
        public float FOVInceraetSpeed;
        public float FOVDecreaseSpeed;
    }

    public ItemEntry[] items = new ItemEntry[0];

    public WeakAssetReference characterPrefab;
    public float health = 100;
    public SprintCameraSettings sprintCameraSettings = new SprintCameraSettings();
    public float eyeHeight = 1.8f;
}
