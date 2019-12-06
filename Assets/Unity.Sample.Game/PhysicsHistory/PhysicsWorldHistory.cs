using System;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

// TODO (mogensh) Improve this when we have C#8
[StructLayout(LayoutKind.Sequential)]
public struct CollisionHistoryBuffer
{
    public const int size = 16;

    private CollisionWorld CollisionWorld00;
    private CollisionWorld CollisionWorld01;
    private CollisionWorld CollisionWorld02;
    private CollisionWorld CollisionWorld03;
    private CollisionWorld CollisionWorld04;
    private CollisionWorld CollisionWorld05;
    private CollisionWorld CollisionWorld06;
    private CollisionWorld CollisionWorld07;
    private CollisionWorld CollisionWorld08;
    private CollisionWorld CollisionWorld09;
    private CollisionWorld CollisionWorld10;
    private CollisionWorld CollisionWorld11;
    private CollisionWorld CollisionWorld12;
    private CollisionWorld CollisionWorld13;
    private CollisionWorld CollisionWorld14;
    private CollisionWorld CollisionWorld15;

    public void GetCollisionWorldFromTick(int tick, out CollisionWorld collWorld)
    {
        var index = tick % size;
        GetCollisionWorldFromIndex(index, out collWorld);
    }

    public void DisposeIndex(int index)
    {
        switch (index)
        {
            case 0 : CollisionWorld00.Dispose(); return;
            case 1 : CollisionWorld01.Dispose(); return;
            case 2 : CollisionWorld02.Dispose(); return;
            case 3 : CollisionWorld03.Dispose(); return;
            case 4 : CollisionWorld04.Dispose(); return;
            case 5 : CollisionWorld05.Dispose(); return;
            case 6 : CollisionWorld06.Dispose(); return;
            case 7 : CollisionWorld07.Dispose(); return;
            case 8 : CollisionWorld08.Dispose(); return;
            case 9 : CollisionWorld09.Dispose(); return;
            case 10 : CollisionWorld10.Dispose(); return;
            case 11 : CollisionWorld11.Dispose(); return;
            case 12 : CollisionWorld12.Dispose(); return;
            case 13 : CollisionWorld13.Dispose(); return;
            case 14 : CollisionWorld14.Dispose(); return;
            case 15 : CollisionWorld15.Dispose(); return;
            default:
                throw new IndexOutOfRangeException();
        }
    }

    void GetCollisionWorldFromIndex(int index, out CollisionWorld collWorld)
    {
        switch (index)
        {
            case 0 : collWorld = CollisionWorld00; break;
            case 1 : collWorld = CollisionWorld01;break;
            case 2 : collWorld = CollisionWorld02;break;
            case 3 : collWorld = CollisionWorld03;break;
            case 4 : collWorld = CollisionWorld04;break;
            case 5 : collWorld = CollisionWorld05;break;
            case 6 : collWorld = CollisionWorld06;break;
            case 7 : collWorld = CollisionWorld07;break;
            case 8 : collWorld = CollisionWorld08;break;
            case 9 : collWorld = CollisionWorld09;break;
            case 10 : collWorld = CollisionWorld10;break;
            case 11 : collWorld = CollisionWorld11;break;
            case 12 : collWorld = CollisionWorld12;break;
            case 13 : collWorld = CollisionWorld13;break;
            case 14 : collWorld = CollisionWorld14;break;
            case 15 : collWorld = CollisionWorld15;break;
            default:
                throw new IndexOutOfRangeException();
        }
    }

    public void CloneCollisionWorld(int index, in CollisionWorld collWorld)
    {
        switch (index)
        {
            case 0 : CollisionWorld00 = (CollisionWorld)collWorld.Clone(); break;
            case 1 : CollisionWorld01 = (CollisionWorld)collWorld.Clone();break;
            case 2 : CollisionWorld02 = (CollisionWorld)collWorld.Clone();break;
            case 3 : CollisionWorld03 = (CollisionWorld)collWorld.Clone();break;
            case 4 : CollisionWorld04 = (CollisionWorld)collWorld.Clone();break;
            case 5 : CollisionWorld05 = (CollisionWorld)collWorld.Clone();break;
            case 6 : CollisionWorld06 = (CollisionWorld)collWorld.Clone();break;
            case 7 : CollisionWorld07 = (CollisionWorld)collWorld.Clone();break;
            case 8 : CollisionWorld08 = (CollisionWorld)collWorld.Clone();break;
            case 9 : CollisionWorld09 = (CollisionWorld)collWorld.Clone();break;
            case 10 : CollisionWorld10 = (CollisionWorld)collWorld.Clone();break;
            case 11 : CollisionWorld11 = (CollisionWorld)collWorld.Clone();break;
            case 12 : CollisionWorld12 = (CollisionWorld)collWorld.Clone();break;
            case 13 : CollisionWorld13 = (CollisionWorld)collWorld.Clone();break;
            case 14 : CollisionWorld14 = (CollisionWorld)collWorld.Clone();break;
            case 15 : CollisionWorld15 = (CollisionWorld)collWorld.Clone();break;
            default:
                throw new IndexOutOfRangeException();
        }
    }

}

//public class CollisionHistory
//{
//    const int m_bufferCapacity = 16;
//
//    private CollisionWorld[] m_CollisionWorlds = new CollisionWorld[m_bufferCapacity];
//    private bool[] m_Stored = new bool[m_bufferCapacity];
//
//    public CollisionHistory()
//    {
//        for (int i = 0; i < m_bufferCapacity; i++)
//            m_CollisionWorlds[i] = new CollisionWorld(0);
//    }
//
//    public void Dispose()
//    {
//        // TODO (mogensh) wait for all jobs that might be querying this collisionworld
//        for (int i = 0; i < m_bufferCapacity; i++)
//            m_CollisionWorlds[i].Dispose();
//    }
//
//    public bool GetCollisionWorld(int tick, out CollisionWorld world)
//    {
//        if (tick < 0)
//        {
//            GameDebug.LogError("Invalid tick. tick:" + tick);
//            world = new CollisionWorld(0);
//            return false;
//        }
//
//
//        var index = tick % m_bufferCapacity;
////        GameDebug.Assert(index >= 0 && index < m_CollisionWorlds.Length, "Invalid index:{0}. Length:{1} tick:{2}", index, m_CollisionWorlds.Length, tick);
//
//        if (!m_Stored[index])
//        {
//           // GameDebug.LogError("No data stored for index:" + index + " tick:" + tick);
//            world = new CollisionWorld(0);
//            return false;
//        }
//        // TODO (mogensh) add check for world not stored yet
////        GameDebug.Assert(m_CollisionWorlds[index].Bodies != null,"CollisionWorld has not been stored for tick:" + tick + " index:" + index);
//
//        world = m_CollisionWorlds[index];
//        return true;
//    }
//
//    public void Store(int tick, ref CollisionWorld sourceCollWorld)
//    {
//        if (tick <= m_LastStoredTick)
//            return;
//
//        // Store world for each tick that has not been stored yet (framerate might be lower than tickrate)
//        var startStoreTick = m_LastStoredTick == -1 ? m_LastStoredTick + 1 : tick;
//        for (int storeTick = startStoreTick; storeTick <= tick; storeTick++)
//        {
//            var index = storeTick % m_bufferCapacity;
//
//            m_CollisionWorlds[index].Dispose();
//
//            m_CollisionWorlds[index] = (CollisionWorld)sourceCollWorld.Clone();
//            m_Stored[index] = true;
//        }
//
//        m_LastStoredTick = tick;
//    }
//
//    int m_LastStoredTick = -1;
//}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class PhysicsWorldHistory : JobComponentSystem
{
    private bool m_initialized;
    private int m_lastStoredTick;

    public CollisionHistoryBuffer CollisionHistory
    {
        get { return m_CollisionHistory;  }
    }

    CollisionHistoryBuffer m_CollisionHistory;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (m_initialized)
        {
            for(int i=0;i<CollisionHistoryBuffer.size;i++)
                m_CollisionHistory.DisposeIndex(i);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var timeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        if (timeQuery.CalculateEntityCount() == 0)
            return default;
        var globalTime = timeQuery.GetSingleton<GlobalGameTime>();

        var tick = globalTime.gameTime.tick;

        var buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
        buildPhysicsWorld.FinalJobHandle.Complete();

        if (!m_initialized)
        {
            for (int i = 0; i < CollisionHistoryBuffer.size; i++)
            {
                m_CollisionHistory.CloneCollisionWorld(i, in buildPhysicsWorld.PhysicsWorld.CollisionWorld);
            }

            m_lastStoredTick = tick;
            m_initialized = true;
        }
        else
        {
            if (tick <= m_lastStoredTick)
                return default;

            // Store world for each tick that has not been stored yet (framerate might be lower than tickrate)
            var startStoreTick = m_lastStoredTick != -1 ? m_lastStoredTick + 1 : tick;
//            GameDebug.Log(World,null,"Store:{0}->{1}",startStoreTick,tick);
            for (int storeTick = startStoreTick; storeTick <= tick; storeTick++)
            {
                var index = storeTick % CollisionHistoryBuffer.size;

                m_CollisionHistory.DisposeIndex(index);
                m_CollisionHistory.CloneCollisionWorld(index, in buildPhysicsWorld.PhysicsWorld.CollisionWorld);
            }

            m_lastStoredTick = tick;
        }

        return default;
    }
}
