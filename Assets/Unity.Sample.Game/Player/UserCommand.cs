using System;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Networking;
using Unity.NetCode;

// TODO (mogensh) we should find an generic and extendable way to define Command so everything does not need to depend on this type
[System.Serializable]
public struct UserCommand :  ICommandData<UserCommand>
{
    public uint Tick => tick;

    public enum Button : uint
    {
        None = 0,
        Jump = 1 << 0,
        Boost = 1 << 1,
        PrimaryFire = 1 << 2,
        SecondaryFire = 1 << 3,
        Reload = 1 << 4,
        Melee = 1 << 5,
        Use = 1 << 6,
        Ability1 = 1 << 7,
        Ability2 = 1 << 8,
        Ability3 = 1 << 9,
        Crouch = 1 << 10,

        CameraSideSwitch = 1 << 15,

        Item1 = 1 << 27,
        Item2 = 1 << 28,
        Item3 = 1 << 29,
        Item4 = 1 << 30,
    }

    public struct ButtonBitField
    {
        public uint flags;

        public bool IsSet(Button button)
        {
            return (flags & (uint)button) > 0;
        }

        public void Or(Button button, bool val)
        {
            if (val)
                flags = flags | (uint)button;
        }

        public void Set(Button button, bool val)
        {
            if (val)
                flags = flags | (uint)button;
            else
            {
                flags = flags & ~(uint)button;
            }
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            var names = Enum.GetNames(typeof(Button));
            var values = Enum.GetValues(typeof(Button));
            stringBuilder.Append("<");
            for (int i = 0; i < names.Length; i++)
            {
                var value = (uint) values.GetValue(i);
                if ((flags & value) == 0)
                    continue;

                stringBuilder.Append("," + names[i]);
            }
            stringBuilder.Append(">");
            return stringBuilder.ToString();
        }
    }

    public uint tick;
    public int checkTick;        // For debug purposes
    public int renderTick;
    public float moveYaw;
    public float moveMagnitude;
    public float lookYaw;
    public float lookPitch;
    public ButtonBitField buttons;

    public static readonly UserCommand defaultCommand = new UserCommand(0);

    private UserCommand(int i)
    {
        tick = 0;
        checkTick = 0;
        renderTick = 0;
        moveYaw = 0;
        moveMagnitude = 0;
        lookYaw = 0;
        lookPitch = 90;
        buttons.flags = 0;
    }

    public void ClearCommand()
    {
        buttons.flags = 0;
        moveMagnitude = 0;
    }

    public float3 LookDir
    {
        get { return math.mul(quaternion.Euler(new float3( math.radians(-lookPitch) , math.radians(lookYaw), 0)) , new float3(0, -1, 0));  }
    }
    public quaternion LookRotation
    {
        get { return quaternion.Euler(new float3(math.radians(90 - lookPitch), math.radians(lookYaw), 0)); }
    }

    public void Serialize(DataStreamWriter writer)
    {
        writer.Write(checkTick);
        writer.Write(renderTick);
        writer.Write((int)(moveYaw*10));
        writer.Write((int)(moveMagnitude*100));
        writer.Write((uint)buttons.flags);
        writer.Write(lookYaw);
        writer.Write(lookPitch);
    }

    public void Deserialize(uint tick, DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
        this.tick = tick;
        checkTick = reader.ReadInt(ref ctx);
        renderTick = reader.ReadInt(ref ctx);
        moveYaw = 0.1f * reader.ReadInt(ref ctx);
        moveMagnitude = 0.01f * reader.ReadInt(ref ctx);
        buttons.flags = reader.ReadUInt(ref ctx);
        lookYaw = reader.ReadFloat(ref ctx);
        lookPitch = reader.ReadFloat(ref ctx);
    }
    public void Serialize(DataStreamWriter writer, UserCommand baseline, NetworkCompressionModel compressionModel)
    {
        writer.WritePackedIntDelta(checkTick, baseline.checkTick, compressionModel);
        writer.WritePackedIntDelta(renderTick, baseline.renderTick, compressionModel);
        writer.WritePackedIntDelta((int)(moveYaw*10), (int)(baseline.moveYaw*10), compressionModel);
        writer.WritePackedIntDelta((int)(moveMagnitude*100), (int)(baseline.moveMagnitude*100), compressionModel);
        writer.WritePackedUIntDelta((uint)buttons.flags, baseline.buttons.flags, compressionModel);
        writer.WritePackedFloatDelta(lookYaw, baseline.lookYaw, compressionModel);
        writer.WritePackedFloatDelta(lookPitch, baseline.lookPitch, compressionModel);
    }

    public void Deserialize(uint tick, DataStreamReader reader, ref DataStreamReader.Context ctx, UserCommand baseline, NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        checkTick = reader.ReadPackedIntDelta(ref ctx, baseline.checkTick, compressionModel);
        renderTick = reader.ReadPackedIntDelta(ref ctx, baseline.renderTick, compressionModel);
        moveYaw = 0.1f * reader.ReadPackedIntDelta(ref ctx, (int)(baseline.moveYaw*10), compressionModel);
        moveMagnitude = 0.01f * reader.ReadPackedIntDelta(ref ctx, (int)(baseline.moveMagnitude*100), compressionModel);
        buttons.flags = reader.ReadPackedUIntDelta(ref ctx, baseline.buttons.flags, compressionModel);
        lookYaw = reader.ReadPackedFloatDelta(ref ctx, baseline.lookYaw, compressionModel);
        lookPitch = reader.ReadPackedFloatDelta(ref ctx, baseline.lookPitch, compressionModel);
    }

    public override string ToString()
    {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
        strBuilder.AppendLine("tick:" + checkTick);
        strBuilder.AppendLine("moveYaw:" + moveYaw);
        strBuilder.AppendLine("moveMagnitude:" + moveMagnitude);
        strBuilder.AppendLine("lookYaw:" + lookYaw);
        strBuilder.AppendLine("lookPitch:" + lookPitch);
        strBuilder.AppendLine("buttons:" + buttons);
        return strBuilder.ToString();
    }
}
