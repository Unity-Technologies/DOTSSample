using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class Player
{
    public struct OwnerPlayerId : IComponentData
    {
        public static OwnerPlayerId Default => new OwnerPlayerId {Value = -1};
        
        [GhostDefaultField]
        public int Value;
    }
    
    public struct State : IComponentData
    {
        [GhostDefaultField]
        public int playerId;
        [GhostDefaultField]
        public NativeString64 playerName;
        [GhostDefaultField]
        public int teamIndex;
        [GhostDefaultField]
        public int score;
        //[ReplicatedField]
        public Entity controlledEntity;
        [GhostDefaultField]
        public bool gameModeSystemInitialized;

        // These are only sync'hed to owning client
        [GhostDefaultField]
        public bool displayCountDown;
        [GhostDefaultField]
        public int countDown;
        [GhostDefaultField]
        public bool displayScoreBoard;
        [GhostDefaultField]
        public bool displayGameScore;
        [GhostDefaultField]
        public bool displayGameResult;
        [GhostDefaultField]
        public NativeString64 gameResult;

        [GhostDefaultField]
        public bool displayGoal;
        [GhostDefaultField(100)]
        public float3 goalPosition;
        [GhostDefaultField]
        public uint goalDefendersColor;
        [GhostDefaultField]
        public uint goalAttackersColor;
        [GhostDefaultField]
        public uint goalAttackers;
        [GhostDefaultField]
        public uint goalDefenders;
        [GhostDefaultField]
        public NativeString64 goalString;
        [GhostDefaultField]
        public NativeString64 actionString;
        [GhostDefaultField(100)]
        public float goalCompletion;

        // Non synchronized
        public bool enableCharacterSwitch;
    }

}
