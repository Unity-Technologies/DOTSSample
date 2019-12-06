using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Animation;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;

public class AnimationGraphHelper
{
    [ConfigVar(Name = "animgraphnode.show.lifetime", DefaultValue = "0", Description = "Show animation graph node lifetime")]
    public static ConfigVar ShowLifetime;

    [ConfigVar(Name = "animgraphnode.show.shutdown", DefaultValue = "0", Description = "Show animation graph node status")]
    public static ConfigVar ShowShutdown;

    class NodeData
    {
        public string name;
    }

    class NodeSetData
    {
        public Dictionary<int, NodeData> Nodes = new Dictionary<int, NodeData>();
    }

    static Dictionary<AnimationGraphSystem,NodeSetData> g_AnimationGraphSystems = new Dictionary<AnimationGraphSystem, NodeSetData>();

    public static NodeHandle<T> CreateNode<T>(AnimationGraphSystem animGraphSys, string name) where T :INodeDefinition, new()
    {
        var handle = animGraphSys.Set.Create<T>();
        GameDebug.Log(animGraphSys.World,ShowLifetime, "Create node. hash:{0} name:{1} ", handle.GetHashCode(), name);

        var data = new NodeData
        {
            name = name,
        };
        GetData(animGraphSys).Nodes.Add(handle.GetHashCode(),data);

        return handle;
    }

    public static void DestroyNode<T>(AnimationGraphSystem animGraphSys, NodeHandle<T> handle)where T :INodeDefinition, new()
    {
        animGraphSys.Set.Destroy(handle);
        GameDebug.Log(animGraphSys.World,ShowLifetime, "Destroy node. hash:{0}", handle.GetHashCode());

        if (!GetData(animGraphSys).Nodes.ContainsKey(handle.GetHashCode()))
        {
            GameDebug.LogError("Node is not registered");
            return;
        }

        GetData(animGraphSys).Nodes.Remove(handle.GetHashCode());
    }

    static public void DumpState(World world)
    {
#if UNITY_EDITOR
        var strBuilder = new StringBuilder();
        strBuilder.AppendLine("AnimationGraphSystems dump state. World:" + world.Name);

        foreach (var pair in g_AnimationGraphSystems)
        {
            if (pair.Key.World != world)
                continue;

            strBuilder.AppendLine(" AnimationGraphSystem:" + pair.Key.GetType());
            foreach (var value in pair.Value.Nodes.Values)
            {
                strBuilder.AppendLine("   " + value.name);
            }
        }

        strBuilder.AppendLine(" AnimationGraphSystems left:" + g_AnimationGraphSystems.Count);
        GameDebug.Log(world,ShowShutdown, strBuilder.ToString());
#endif
    }

    static public void Shutdown(World world)
    {
        GameDebug.Log(world,ShowShutdown, "AnimationGraph shutdown. World:" + world.Name);

        var outgoing = new List<AnimationGraphSystem>();
        foreach (var pair in g_AnimationGraphSystems)
        {
            if (pair.Key.World != world)
                continue;

            outgoing.Add(pair.Key);

        }

        foreach (var system in outgoing)
        {
            g_AnimationGraphSystems.Remove(system);
        }

        DumpState(world);
    }


    static NodeSetData GetData(AnimationGraphSystem animGraphSys)
    {
        NodeSetData data;
        if (g_AnimationGraphSystems.TryGetValue(animGraphSys, out data))
            return data;

        data = new NodeSetData();
        g_AnimationGraphSystems.Add(animGraphSys,data);
        return data;
    }

}
