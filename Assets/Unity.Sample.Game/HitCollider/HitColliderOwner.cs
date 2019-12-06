using System;
using Unity.Entities;



public static class HitColliderOwner
{
    [Flags]
    public enum TeamFlagsEnum
    {
        TeamA = 1 << 0,
        TeamB = 1 << 1,
    }


    [Serializable]
    public struct State : IComponentData
    {
        [EnumBitField(typeof(TeamFlagsEnum))]
        public uint colliderFlags;

        public int collisionEnabled;
    }
}



