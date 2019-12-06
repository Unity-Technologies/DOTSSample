using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

public struct PlayerStateSnapshotData : ISnapshotData<PlayerStateSnapshotData>
{
    public uint tick;
    private int PlayerStateplayerId;
    private NativeString64 PlayerStateplayerName;
    private int PlayerStateteamIndex;
    private int PlayerStatescore;
    private uint PlayerStategameModeSystemInitialized;
    private uint PlayerStatedisplayCountDown;
    private int PlayerStatecountDown;
    private uint PlayerStatedisplayScoreBoard;
    private uint PlayerStatedisplayGameScore;
    private uint PlayerStatedisplayGameResult;
    private NativeString64 PlayerStategameResult;
    private uint PlayerStatedisplayGoal;
    private int PlayerStategoalPositionX;
    private int PlayerStategoalPositionY;
    private int PlayerStategoalPositionZ;
    private uint PlayerStategoalDefendersColor;
    private uint PlayerStategoalAttackersColor;
    private uint PlayerStategoalAttackers;
    private uint PlayerStategoalDefenders;
    private NativeString64 PlayerStategoalString;
    private NativeString64 PlayerStateactionString;
    private int PlayerStategoalCompletion;
    uint changeMask0;

    public uint Tick => tick;
    public int GetPlayerStateplayerId(GhostDeserializerState deserializerState)
    {
        return (int)PlayerStateplayerId;
    }
    public int GetPlayerStateplayerId()
    {
        return (int)PlayerStateplayerId;
    }
    public void SetPlayerStateplayerId(int val, GhostSerializerState serializerState)
    {
        PlayerStateplayerId = (int)val;
    }
    public void SetPlayerStateplayerId(int val)
    {
        PlayerStateplayerId = (int)val;
    }
    public NativeString64 GetPlayerStateplayerName(GhostDeserializerState deserializerState)
    {
        return PlayerStateplayerName;
    }
    public NativeString64 GetPlayerStateplayerName()
    {
        return PlayerStateplayerName;
    }
    public void SetPlayerStateplayerName(NativeString64 val, GhostSerializerState serializerState)
    {
        PlayerStateplayerName = val;
    }
    public void SetPlayerStateplayerName(NativeString64 val)
    {
        PlayerStateplayerName = val;
    }
    public int GetPlayerStateteamIndex(GhostDeserializerState deserializerState)
    {
        return (int)PlayerStateteamIndex;
    }
    public int GetPlayerStateteamIndex()
    {
        return (int)PlayerStateteamIndex;
    }
    public void SetPlayerStateteamIndex(int val, GhostSerializerState serializerState)
    {
        PlayerStateteamIndex = (int)val;
    }
    public void SetPlayerStateteamIndex(int val)
    {
        PlayerStateteamIndex = (int)val;
    }
    public int GetPlayerStatescore(GhostDeserializerState deserializerState)
    {
        return (int)PlayerStatescore;
    }
    public int GetPlayerStatescore()
    {
        return (int)PlayerStatescore;
    }
    public void SetPlayerStatescore(int val, GhostSerializerState serializerState)
    {
        PlayerStatescore = (int)val;
    }
    public void SetPlayerStatescore(int val)
    {
        PlayerStatescore = (int)val;
    }
    public bool GetPlayerStategameModeSystemInitialized(GhostDeserializerState deserializerState)
    {
        return PlayerStategameModeSystemInitialized!=0;
    }
    public bool GetPlayerStategameModeSystemInitialized()
    {
        return PlayerStategameModeSystemInitialized!=0;
    }
    public void SetPlayerStategameModeSystemInitialized(bool val, GhostSerializerState serializerState)
    {
        PlayerStategameModeSystemInitialized = val?1u:0;
    }
    public void SetPlayerStategameModeSystemInitialized(bool val)
    {
        PlayerStategameModeSystemInitialized = val?1u:0;
    }
    public bool GetPlayerStatedisplayCountDown(GhostDeserializerState deserializerState)
    {
        return PlayerStatedisplayCountDown!=0;
    }
    public bool GetPlayerStatedisplayCountDown()
    {
        return PlayerStatedisplayCountDown!=0;
    }
    public void SetPlayerStatedisplayCountDown(bool val, GhostSerializerState serializerState)
    {
        PlayerStatedisplayCountDown = val?1u:0;
    }
    public void SetPlayerStatedisplayCountDown(bool val)
    {
        PlayerStatedisplayCountDown = val?1u:0;
    }
    public int GetPlayerStatecountDown(GhostDeserializerState deserializerState)
    {
        return (int)PlayerStatecountDown;
    }
    public int GetPlayerStatecountDown()
    {
        return (int)PlayerStatecountDown;
    }
    public void SetPlayerStatecountDown(int val, GhostSerializerState serializerState)
    {
        PlayerStatecountDown = (int)val;
    }
    public void SetPlayerStatecountDown(int val)
    {
        PlayerStatecountDown = (int)val;
    }
    public bool GetPlayerStatedisplayScoreBoard(GhostDeserializerState deserializerState)
    {
        return PlayerStatedisplayScoreBoard!=0;
    }
    public bool GetPlayerStatedisplayScoreBoard()
    {
        return PlayerStatedisplayScoreBoard!=0;
    }
    public void SetPlayerStatedisplayScoreBoard(bool val, GhostSerializerState serializerState)
    {
        PlayerStatedisplayScoreBoard = val?1u:0;
    }
    public void SetPlayerStatedisplayScoreBoard(bool val)
    {
        PlayerStatedisplayScoreBoard = val?1u:0;
    }
    public bool GetPlayerStatedisplayGameScore(GhostDeserializerState deserializerState)
    {
        return PlayerStatedisplayGameScore!=0;
    }
    public bool GetPlayerStatedisplayGameScore()
    {
        return PlayerStatedisplayGameScore!=0;
    }
    public void SetPlayerStatedisplayGameScore(bool val, GhostSerializerState serializerState)
    {
        PlayerStatedisplayGameScore = val?1u:0;
    }
    public void SetPlayerStatedisplayGameScore(bool val)
    {
        PlayerStatedisplayGameScore = val?1u:0;
    }
    public bool GetPlayerStatedisplayGameResult(GhostDeserializerState deserializerState)
    {
        return PlayerStatedisplayGameResult!=0;
    }
    public bool GetPlayerStatedisplayGameResult()
    {
        return PlayerStatedisplayGameResult!=0;
    }
    public void SetPlayerStatedisplayGameResult(bool val, GhostSerializerState serializerState)
    {
        PlayerStatedisplayGameResult = val?1u:0;
    }
    public void SetPlayerStatedisplayGameResult(bool val)
    {
        PlayerStatedisplayGameResult = val?1u:0;
    }
    public NativeString64 GetPlayerStategameResult(GhostDeserializerState deserializerState)
    {
        return PlayerStategameResult;
    }
    public NativeString64 GetPlayerStategameResult()
    {
        return PlayerStategameResult;
    }
    public void SetPlayerStategameResult(NativeString64 val, GhostSerializerState serializerState)
    {
        PlayerStategameResult = val;
    }
    public void SetPlayerStategameResult(NativeString64 val)
    {
        PlayerStategameResult = val;
    }
    public bool GetPlayerStatedisplayGoal(GhostDeserializerState deserializerState)
    {
        return PlayerStatedisplayGoal!=0;
    }
    public bool GetPlayerStatedisplayGoal()
    {
        return PlayerStatedisplayGoal!=0;
    }
    public void SetPlayerStatedisplayGoal(bool val, GhostSerializerState serializerState)
    {
        PlayerStatedisplayGoal = val?1u:0;
    }
    public void SetPlayerStatedisplayGoal(bool val)
    {
        PlayerStatedisplayGoal = val?1u:0;
    }
    public float3 GetPlayerStategoalPosition(GhostDeserializerState deserializerState)
    {
        return GetPlayerStategoalPosition();
    }
    public float3 GetPlayerStategoalPosition()
    {
        return new float3(PlayerStategoalPositionX * 0.01f, PlayerStategoalPositionY * 0.01f, PlayerStategoalPositionZ * 0.01f);
    }
    public void SetPlayerStategoalPosition(float3 val, GhostSerializerState serializerState)
    {
        SetPlayerStategoalPosition(val);
    }
    public void SetPlayerStategoalPosition(float3 val)
    {
        PlayerStategoalPositionX = (int)(val.x * 100);
        PlayerStategoalPositionY = (int)(val.y * 100);
        PlayerStategoalPositionZ = (int)(val.z * 100);
    }
    public uint GetPlayerStategoalDefendersColor(GhostDeserializerState deserializerState)
    {
        return (uint)PlayerStategoalDefendersColor;
    }
    public uint GetPlayerStategoalDefendersColor()
    {
        return (uint)PlayerStategoalDefendersColor;
    }
    public void SetPlayerStategoalDefendersColor(uint val, GhostSerializerState serializerState)
    {
        PlayerStategoalDefendersColor = (uint)val;
    }
    public void SetPlayerStategoalDefendersColor(uint val)
    {
        PlayerStategoalDefendersColor = (uint)val;
    }
    public uint GetPlayerStategoalAttackersColor(GhostDeserializerState deserializerState)
    {
        return (uint)PlayerStategoalAttackersColor;
    }
    public uint GetPlayerStategoalAttackersColor()
    {
        return (uint)PlayerStategoalAttackersColor;
    }
    public void SetPlayerStategoalAttackersColor(uint val, GhostSerializerState serializerState)
    {
        PlayerStategoalAttackersColor = (uint)val;
    }
    public void SetPlayerStategoalAttackersColor(uint val)
    {
        PlayerStategoalAttackersColor = (uint)val;
    }
    public uint GetPlayerStategoalAttackers(GhostDeserializerState deserializerState)
    {
        return (uint)PlayerStategoalAttackers;
    }
    public uint GetPlayerStategoalAttackers()
    {
        return (uint)PlayerStategoalAttackers;
    }
    public void SetPlayerStategoalAttackers(uint val, GhostSerializerState serializerState)
    {
        PlayerStategoalAttackers = (uint)val;
    }
    public void SetPlayerStategoalAttackers(uint val)
    {
        PlayerStategoalAttackers = (uint)val;
    }
    public uint GetPlayerStategoalDefenders(GhostDeserializerState deserializerState)
    {
        return (uint)PlayerStategoalDefenders;
    }
    public uint GetPlayerStategoalDefenders()
    {
        return (uint)PlayerStategoalDefenders;
    }
    public void SetPlayerStategoalDefenders(uint val, GhostSerializerState serializerState)
    {
        PlayerStategoalDefenders = (uint)val;
    }
    public void SetPlayerStategoalDefenders(uint val)
    {
        PlayerStategoalDefenders = (uint)val;
    }
    public NativeString64 GetPlayerStategoalString(GhostDeserializerState deserializerState)
    {
        return PlayerStategoalString;
    }
    public NativeString64 GetPlayerStategoalString()
    {
        return PlayerStategoalString;
    }
    public void SetPlayerStategoalString(NativeString64 val, GhostSerializerState serializerState)
    {
        PlayerStategoalString = val;
    }
    public void SetPlayerStategoalString(NativeString64 val)
    {
        PlayerStategoalString = val;
    }
    public NativeString64 GetPlayerStateactionString(GhostDeserializerState deserializerState)
    {
        return PlayerStateactionString;
    }
    public NativeString64 GetPlayerStateactionString()
    {
        return PlayerStateactionString;
    }
    public void SetPlayerStateactionString(NativeString64 val, GhostSerializerState serializerState)
    {
        PlayerStateactionString = val;
    }
    public void SetPlayerStateactionString(NativeString64 val)
    {
        PlayerStateactionString = val;
    }
    public float GetPlayerStategoalCompletion(GhostDeserializerState deserializerState)
    {
        return PlayerStategoalCompletion * 0.01f;
    }
    public float GetPlayerStategoalCompletion()
    {
        return PlayerStategoalCompletion * 0.01f;
    }
    public void SetPlayerStategoalCompletion(float val, GhostSerializerState serializerState)
    {
        PlayerStategoalCompletion = (int)(val * 100);
    }
    public void SetPlayerStategoalCompletion(float val)
    {
        PlayerStategoalCompletion = (int)(val * 100);
    }

    public void PredictDelta(uint tick, ref PlayerStateSnapshotData baseline1, ref PlayerStateSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
        PlayerStateplayerId = predictor.PredictInt(PlayerStateplayerId, baseline1.PlayerStateplayerId, baseline2.PlayerStateplayerId);
        PlayerStateteamIndex = predictor.PredictInt(PlayerStateteamIndex, baseline1.PlayerStateteamIndex, baseline2.PlayerStateteamIndex);
        PlayerStatescore = predictor.PredictInt(PlayerStatescore, baseline1.PlayerStatescore, baseline2.PlayerStatescore);
        PlayerStategameModeSystemInitialized = (uint)predictor.PredictInt((int)PlayerStategameModeSystemInitialized, (int)baseline1.PlayerStategameModeSystemInitialized, (int)baseline2.PlayerStategameModeSystemInitialized);
        PlayerStatedisplayCountDown = (uint)predictor.PredictInt((int)PlayerStatedisplayCountDown, (int)baseline1.PlayerStatedisplayCountDown, (int)baseline2.PlayerStatedisplayCountDown);
        PlayerStatecountDown = predictor.PredictInt(PlayerStatecountDown, baseline1.PlayerStatecountDown, baseline2.PlayerStatecountDown);
        PlayerStatedisplayScoreBoard = (uint)predictor.PredictInt((int)PlayerStatedisplayScoreBoard, (int)baseline1.PlayerStatedisplayScoreBoard, (int)baseline2.PlayerStatedisplayScoreBoard);
        PlayerStatedisplayGameScore = (uint)predictor.PredictInt((int)PlayerStatedisplayGameScore, (int)baseline1.PlayerStatedisplayGameScore, (int)baseline2.PlayerStatedisplayGameScore);
        PlayerStatedisplayGameResult = (uint)predictor.PredictInt((int)PlayerStatedisplayGameResult, (int)baseline1.PlayerStatedisplayGameResult, (int)baseline2.PlayerStatedisplayGameResult);
        PlayerStatedisplayGoal = (uint)predictor.PredictInt((int)PlayerStatedisplayGoal, (int)baseline1.PlayerStatedisplayGoal, (int)baseline2.PlayerStatedisplayGoal);
        PlayerStategoalPositionX = predictor.PredictInt(PlayerStategoalPositionX, baseline1.PlayerStategoalPositionX, baseline2.PlayerStategoalPositionX);
        PlayerStategoalPositionY = predictor.PredictInt(PlayerStategoalPositionY, baseline1.PlayerStategoalPositionY, baseline2.PlayerStategoalPositionY);
        PlayerStategoalPositionZ = predictor.PredictInt(PlayerStategoalPositionZ, baseline1.PlayerStategoalPositionZ, baseline2.PlayerStategoalPositionZ);
        PlayerStategoalDefendersColor = (uint)predictor.PredictInt((int)PlayerStategoalDefendersColor, (int)baseline1.PlayerStategoalDefendersColor, (int)baseline2.PlayerStategoalDefendersColor);
        PlayerStategoalAttackersColor = (uint)predictor.PredictInt((int)PlayerStategoalAttackersColor, (int)baseline1.PlayerStategoalAttackersColor, (int)baseline2.PlayerStategoalAttackersColor);
        PlayerStategoalAttackers = (uint)predictor.PredictInt((int)PlayerStategoalAttackers, (int)baseline1.PlayerStategoalAttackers, (int)baseline2.PlayerStategoalAttackers);
        PlayerStategoalDefenders = (uint)predictor.PredictInt((int)PlayerStategoalDefenders, (int)baseline1.PlayerStategoalDefenders, (int)baseline2.PlayerStategoalDefenders);
        PlayerStategoalCompletion = predictor.PredictInt(PlayerStategoalCompletion, baseline1.PlayerStategoalCompletion, baseline2.PlayerStategoalCompletion);
    }

    public void Serialize(int networkId, ref PlayerStateSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
        changeMask0 = (PlayerStateplayerId != baseline.PlayerStateplayerId) ? 1u : 0;
        changeMask0 |= PlayerStateplayerName.Equals(baseline.PlayerStateplayerName) ? 0 : (1u<<1);
        changeMask0 |= (PlayerStateteamIndex != baseline.PlayerStateteamIndex) ? (1u<<2) : 0;
        changeMask0 |= (PlayerStatescore != baseline.PlayerStatescore) ? (1u<<3) : 0;
        changeMask0 |= (PlayerStategameModeSystemInitialized != baseline.PlayerStategameModeSystemInitialized) ? (1u<<4) : 0;
        changeMask0 |= (PlayerStatedisplayCountDown != baseline.PlayerStatedisplayCountDown) ? (1u<<5) : 0;
        changeMask0 |= (PlayerStatecountDown != baseline.PlayerStatecountDown) ? (1u<<6) : 0;
        changeMask0 |= (PlayerStatedisplayScoreBoard != baseline.PlayerStatedisplayScoreBoard) ? (1u<<7) : 0;
        changeMask0 |= (PlayerStatedisplayGameScore != baseline.PlayerStatedisplayGameScore) ? (1u<<8) : 0;
        changeMask0 |= (PlayerStatedisplayGameResult != baseline.PlayerStatedisplayGameResult) ? (1u<<9) : 0;
        changeMask0 |= PlayerStategameResult.Equals(baseline.PlayerStategameResult) ? 0 : (1u<<10);
        changeMask0 |= (PlayerStatedisplayGoal != baseline.PlayerStatedisplayGoal) ? (1u<<11) : 0;
        changeMask0 |= (PlayerStategoalPositionX != baseline.PlayerStategoalPositionX ||
                                           PlayerStategoalPositionY != baseline.PlayerStategoalPositionY ||
                                           PlayerStategoalPositionZ != baseline.PlayerStategoalPositionZ) ? (1u<<12) : 0;
        changeMask0 |= (PlayerStategoalDefendersColor != baseline.PlayerStategoalDefendersColor) ? (1u<<13) : 0;
        changeMask0 |= (PlayerStategoalAttackersColor != baseline.PlayerStategoalAttackersColor) ? (1u<<14) : 0;
        changeMask0 |= (PlayerStategoalAttackers != baseline.PlayerStategoalAttackers) ? (1u<<15) : 0;
        changeMask0 |= (PlayerStategoalDefenders != baseline.PlayerStategoalDefenders) ? (1u<<16) : 0;
        changeMask0 |= PlayerStategoalString.Equals(baseline.PlayerStategoalString) ? 0 : (1u<<17);
        changeMask0 |= PlayerStateactionString.Equals(baseline.PlayerStateactionString) ? 0 : (1u<<18);
        changeMask0 |= (PlayerStategoalCompletion != baseline.PlayerStategoalCompletion) ? (1u<<19) : 0;
        writer.WritePackedUIntDelta(changeMask0, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            writer.WritePackedIntDelta(PlayerStateplayerId, baseline.PlayerStateplayerId, compressionModel);
        if ((changeMask0 & (1 << 1)) != 0)
        {
            writer.WritePackedUIntDelta(PlayerStateplayerName.LengthInBytes, baseline.PlayerStateplayerName.LengthInBytes, compressionModel);
            var PlayerStateplayerNameBaselineLength = (ushort)math.min((uint)PlayerStateplayerName.LengthInBytes, baseline.PlayerStateplayerName.LengthInBytes);
            for (int sb = 0; sb < PlayerStateplayerNameBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStateplayerName.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStateplayerName.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b1[sb], b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStateplayerNameBaselineLength; sb < PlayerStateplayerName.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStateplayerName.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b[sb], 0, compressionModel);
                    }
                }
            }
        }
        if ((changeMask0 & (1 << 2)) != 0)
            writer.WritePackedIntDelta(PlayerStateteamIndex, baseline.PlayerStateteamIndex, compressionModel);
        if ((changeMask0 & (1 << 3)) != 0)
            writer.WritePackedIntDelta(PlayerStatescore, baseline.PlayerStatescore, compressionModel);
        if ((changeMask0 & (1 << 4)) != 0)
            writer.WritePackedUIntDelta(PlayerStategameModeSystemInitialized, baseline.PlayerStategameModeSystemInitialized, compressionModel);
        if ((changeMask0 & (1 << 5)) != 0)
            writer.WritePackedUIntDelta(PlayerStatedisplayCountDown, baseline.PlayerStatedisplayCountDown, compressionModel);
        if ((changeMask0 & (1 << 6)) != 0)
            writer.WritePackedIntDelta(PlayerStatecountDown, baseline.PlayerStatecountDown, compressionModel);
        if ((changeMask0 & (1 << 7)) != 0)
            writer.WritePackedUIntDelta(PlayerStatedisplayScoreBoard, baseline.PlayerStatedisplayScoreBoard, compressionModel);
        if ((changeMask0 & (1 << 8)) != 0)
            writer.WritePackedUIntDelta(PlayerStatedisplayGameScore, baseline.PlayerStatedisplayGameScore, compressionModel);
        if ((changeMask0 & (1 << 9)) != 0)
            writer.WritePackedUIntDelta(PlayerStatedisplayGameResult, baseline.PlayerStatedisplayGameResult, compressionModel);
        if ((changeMask0 & (1 << 10)) != 0)
        {
            writer.WritePackedUIntDelta(PlayerStategameResult.LengthInBytes, baseline.PlayerStategameResult.LengthInBytes, compressionModel);
            var PlayerStategameResultBaselineLength = (ushort)math.min((uint)PlayerStategameResult.LengthInBytes, baseline.PlayerStategameResult.LengthInBytes);
            for (int sb = 0; sb < PlayerStategameResultBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStategameResult.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStategameResult.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b1[sb], b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStategameResultBaselineLength; sb < PlayerStategameResult.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStategameResult.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b[sb], 0, compressionModel);
                    }
                }
            }
        }
        if ((changeMask0 & (1 << 11)) != 0)
            writer.WritePackedUIntDelta(PlayerStatedisplayGoal, baseline.PlayerStatedisplayGoal, compressionModel);
        if ((changeMask0 & (1 << 12)) != 0)
        {
            writer.WritePackedIntDelta(PlayerStategoalPositionX, baseline.PlayerStategoalPositionX, compressionModel);
            writer.WritePackedIntDelta(PlayerStategoalPositionY, baseline.PlayerStategoalPositionY, compressionModel);
            writer.WritePackedIntDelta(PlayerStategoalPositionZ, baseline.PlayerStategoalPositionZ, compressionModel);
        }
        if ((changeMask0 & (1 << 13)) != 0)
            writer.WritePackedUIntDelta(PlayerStategoalDefendersColor, baseline.PlayerStategoalDefendersColor, compressionModel);
        if ((changeMask0 & (1 << 14)) != 0)
            writer.WritePackedUIntDelta(PlayerStategoalAttackersColor, baseline.PlayerStategoalAttackersColor, compressionModel);
        if ((changeMask0 & (1 << 15)) != 0)
            writer.WritePackedUIntDelta(PlayerStategoalAttackers, baseline.PlayerStategoalAttackers, compressionModel);
        if ((changeMask0 & (1 << 16)) != 0)
            writer.WritePackedUIntDelta(PlayerStategoalDefenders, baseline.PlayerStategoalDefenders, compressionModel);
        if ((changeMask0 & (1 << 17)) != 0)
        {
            writer.WritePackedUIntDelta(PlayerStategoalString.LengthInBytes, baseline.PlayerStategoalString.LengthInBytes, compressionModel);
            var PlayerStategoalStringBaselineLength = (ushort)math.min((uint)PlayerStategoalString.LengthInBytes, baseline.PlayerStategoalString.LengthInBytes);
            for (int sb = 0; sb < PlayerStategoalStringBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStategoalString.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStategoalString.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b1[sb], b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStategoalStringBaselineLength; sb < PlayerStategoalString.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStategoalString.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b[sb], 0, compressionModel);
                    }
                }
            }
        }
        if ((changeMask0 & (1 << 18)) != 0)
        {
            writer.WritePackedUIntDelta(PlayerStateactionString.LengthInBytes, baseline.PlayerStateactionString.LengthInBytes, compressionModel);
            var PlayerStateactionStringBaselineLength = (ushort)math.min((uint)PlayerStateactionString.LengthInBytes, baseline.PlayerStateactionString.LengthInBytes);
            for (int sb = 0; sb < PlayerStateactionStringBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStateactionString.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStateactionString.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b1[sb], b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStateactionStringBaselineLength; sb < PlayerStateactionString.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStateactionString.buffer.byte0000)
                    {
                        writer.WritePackedUIntDelta(b[sb], 0, compressionModel);
                    }
                }
            }
        }
        if ((changeMask0 & (1 << 19)) != 0)
            writer.WritePackedIntDelta(PlayerStategoalCompletion, baseline.PlayerStategoalCompletion, compressionModel);
    }

    public void Deserialize(uint tick, ref PlayerStateSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        changeMask0 = reader.ReadPackedUIntDelta(ref ctx, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            PlayerStateplayerId = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStateplayerId, compressionModel);
        else
            PlayerStateplayerId = baseline.PlayerStateplayerId;
        if ((changeMask0 & (1 << 1)) != 0)
        {
            PlayerStateplayerName.LengthInBytes = (ushort)reader.ReadPackedUIntDelta(ref ctx, (uint)baseline.PlayerStateplayerName.LengthInBytes, compressionModel);
            var PlayerStateplayerNameBaselineLength = (ushort)math.min((uint)PlayerStateplayerName.LengthInBytes, baseline.PlayerStateplayerName.LengthInBytes);
            for (int sb = 0; sb < PlayerStateplayerNameBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStateplayerName.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStateplayerName.buffer.byte0000)
                    {
                        b1[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStateplayerNameBaselineLength; sb < PlayerStateplayerName.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStateplayerName.buffer.byte0000)
                    {
                        b[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, 0, compressionModel);
                    }
                }
            }
        }
        else
            PlayerStateplayerName = baseline.PlayerStateplayerName;
        if ((changeMask0 & (1 << 2)) != 0)
            PlayerStateteamIndex = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStateteamIndex, compressionModel);
        else
            PlayerStateteamIndex = baseline.PlayerStateteamIndex;
        if ((changeMask0 & (1 << 3)) != 0)
            PlayerStatescore = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStatescore, compressionModel);
        else
            PlayerStatescore = baseline.PlayerStatescore;
        if ((changeMask0 & (1 << 4)) != 0)
            PlayerStategameModeSystemInitialized = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStategameModeSystemInitialized, compressionModel);
        else
            PlayerStategameModeSystemInitialized = baseline.PlayerStategameModeSystemInitialized;
        if ((changeMask0 & (1 << 5)) != 0)
            PlayerStatedisplayCountDown = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStatedisplayCountDown, compressionModel);
        else
            PlayerStatedisplayCountDown = baseline.PlayerStatedisplayCountDown;
        if ((changeMask0 & (1 << 6)) != 0)
            PlayerStatecountDown = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStatecountDown, compressionModel);
        else
            PlayerStatecountDown = baseline.PlayerStatecountDown;
        if ((changeMask0 & (1 << 7)) != 0)
            PlayerStatedisplayScoreBoard = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStatedisplayScoreBoard, compressionModel);
        else
            PlayerStatedisplayScoreBoard = baseline.PlayerStatedisplayScoreBoard;
        if ((changeMask0 & (1 << 8)) != 0)
            PlayerStatedisplayGameScore = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStatedisplayGameScore, compressionModel);
        else
            PlayerStatedisplayGameScore = baseline.PlayerStatedisplayGameScore;
        if ((changeMask0 & (1 << 9)) != 0)
            PlayerStatedisplayGameResult = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStatedisplayGameResult, compressionModel);
        else
            PlayerStatedisplayGameResult = baseline.PlayerStatedisplayGameResult;
        if ((changeMask0 & (1 << 10)) != 0)
        {
            PlayerStategameResult.LengthInBytes = (ushort)reader.ReadPackedUIntDelta(ref ctx, (uint)baseline.PlayerStategameResult.LengthInBytes, compressionModel);
            var PlayerStategameResultBaselineLength = (ushort)math.min((uint)PlayerStategameResult.LengthInBytes, baseline.PlayerStategameResult.LengthInBytes);
            for (int sb = 0; sb < PlayerStategameResultBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStategameResult.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStategameResult.buffer.byte0000)
                    {
                        b1[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStategameResultBaselineLength; sb < PlayerStategameResult.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStategameResult.buffer.byte0000)
                    {
                        b[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, 0, compressionModel);
                    }
                }
            }
        }
        else
            PlayerStategameResult = baseline.PlayerStategameResult;
        if ((changeMask0 & (1 << 11)) != 0)
            PlayerStatedisplayGoal = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStatedisplayGoal, compressionModel);
        else
            PlayerStatedisplayGoal = baseline.PlayerStatedisplayGoal;
        if ((changeMask0 & (1 << 12)) != 0)
        {
            PlayerStategoalPositionX = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStategoalPositionX, compressionModel);
            PlayerStategoalPositionY = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStategoalPositionY, compressionModel);
            PlayerStategoalPositionZ = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStategoalPositionZ, compressionModel);
        }
        else
        {
            PlayerStategoalPositionX = baseline.PlayerStategoalPositionX;
            PlayerStategoalPositionY = baseline.PlayerStategoalPositionY;
            PlayerStategoalPositionZ = baseline.PlayerStategoalPositionZ;
        }
        if ((changeMask0 & (1 << 13)) != 0)
            PlayerStategoalDefendersColor = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStategoalDefendersColor, compressionModel);
        else
            PlayerStategoalDefendersColor = baseline.PlayerStategoalDefendersColor;
        if ((changeMask0 & (1 << 14)) != 0)
            PlayerStategoalAttackersColor = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStategoalAttackersColor, compressionModel);
        else
            PlayerStategoalAttackersColor = baseline.PlayerStategoalAttackersColor;
        if ((changeMask0 & (1 << 15)) != 0)
            PlayerStategoalAttackers = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStategoalAttackers, compressionModel);
        else
            PlayerStategoalAttackers = baseline.PlayerStategoalAttackers;
        if ((changeMask0 & (1 << 16)) != 0)
            PlayerStategoalDefenders = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerStategoalDefenders, compressionModel);
        else
            PlayerStategoalDefenders = baseline.PlayerStategoalDefenders;
        if ((changeMask0 & (1 << 17)) != 0)
        {
            PlayerStategoalString.LengthInBytes = (ushort)reader.ReadPackedUIntDelta(ref ctx, (uint)baseline.PlayerStategoalString.LengthInBytes, compressionModel);
            var PlayerStategoalStringBaselineLength = (ushort)math.min((uint)PlayerStategoalString.LengthInBytes, baseline.PlayerStategoalString.LengthInBytes);
            for (int sb = 0; sb < PlayerStategoalStringBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStategoalString.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStategoalString.buffer.byte0000)
                    {
                        b1[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStategoalStringBaselineLength; sb < PlayerStategoalString.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStategoalString.buffer.byte0000)
                    {
                        b[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, 0, compressionModel);
                    }
                }
            }
        }
        else
            PlayerStategoalString = baseline.PlayerStategoalString;
        if ((changeMask0 & (1 << 18)) != 0)
        {
            PlayerStateactionString.LengthInBytes = (ushort)reader.ReadPackedUIntDelta(ref ctx, (uint)baseline.PlayerStateactionString.LengthInBytes, compressionModel);
            var PlayerStateactionStringBaselineLength = (ushort)math.min((uint)PlayerStateactionString.LengthInBytes, baseline.PlayerStateactionString.LengthInBytes);
            for (int sb = 0; sb < PlayerStateactionStringBaselineLength; ++sb)
            {
                unsafe
                {
                    fixed (byte* b1 = &PlayerStateactionString.buffer.byte0000)
                    fixed (byte* b2 = &baseline.PlayerStateactionString.buffer.byte0000)
                    {
                        b1[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, b2[sb], compressionModel);
                    }
                }
            }
            for (int sb = PlayerStateactionStringBaselineLength; sb < PlayerStateactionString.LengthInBytes; ++sb)
            {
                unsafe
                {
                    fixed (byte* b = &PlayerStateactionString.buffer.byte0000)
                    {
                        b[sb] = (byte)reader.ReadPackedUIntDelta(ref ctx, 0, compressionModel);
                    }
                }
            }
        }
        else
            PlayerStateactionString = baseline.PlayerStateactionString;
        if ((changeMask0 & (1 << 19)) != 0)
            PlayerStategoalCompletion = reader.ReadPackedIntDelta(ref ctx, baseline.PlayerStategoalCompletion, compressionModel);
        else
            PlayerStategoalCompletion = baseline.PlayerStategoalCompletion;
    }
    public void Interpolate(ref PlayerStateSnapshotData target, float factor)
    {
    }
}
