using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public struct LocalPlayer : IComponentData
{
    public int playerId;
    public UserCommand command;

    // Previous UserCommands are stored in a Buffer on the entity.
    // Commands are stored at (tick % bufferSize) and the
    // below two values are used to keep track of the history
    public int m_LastTickStored;    // The last tick for which a command was stored
    public int m_NumConsecutives;   // Number of consecutive commands stored

    public bool HasCommand(int tick) { return tick <= m_LastTickStored && tick > m_LastTickStored - m_NumConsecutives; }
    public int FirstTick() { return m_NumConsecutives > 0 ? m_LastTickStored - m_NumConsecutives + 1 : -1; }
    public int LastTick() { return m_NumConsecutives > 0 ? m_LastTickStored : -1; }
    public void ClearCommandHistory() { m_LastTickStored = 0; m_NumConsecutives = 0; }

    public Entity controlledEntity;
    public Entity playerEntity;
    public Entity hudEntity;

    public float m_debugMoveDuration;
    public float m_debugMovePhaseDuration;
    public float m_debugMoveTurnSpeed;
    public float m_debugMoveMag;

    static public LocalPlayer Default()
    {
        var d = new LocalPlayer();
        d.command = UserCommand.defaultCommand;
        d.playerId = -1;
        d.controlledEntity = Entity.Null;
        d.playerEntity = Entity.Null;
        return d;
    }
}
