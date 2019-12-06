using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Sample.Core;

[DisableAutoCreation]
public class ManualComponentSystemGroup : ComponentSystemGroup
{
    List<ComponentSystemBase> childSystems = new List<ComponentSystemBase>();
    static Dictionary<System.Type, object[]> s_AttributeCache = new Dictionary<System.Type, object[]>();
    protected override void OnCreate()
    {
        base.OnCreate();

        // Verify we have disableautiocreateion
        var hasDisableAutoCreation = this.GetType().GetCustomAttributes(typeof(DisableAutoCreationAttribute), false).Length > 0;
        if (!hasDisableAutoCreation)
            GameDebug.LogError($"Trying to create a system {this.GetType()} derived from ManualComponentSystemGroup but there is no [DisablAutoCreation]");

        // Find all systems that execute in this group and create them
        foreach (var s in GameBootStrap.Systems)
        {
            object[] groupsAttributes;
            if (s_AttributeCache.ContainsKey(s))
                groupsAttributes = s_AttributeCache[s];
            else
            {
                groupsAttributes = s.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                s_AttributeCache[s] = groupsAttributes;
            }
            foreach (var g in groupsAttributes)
            {
                var uig = g as UpdateInGroupAttribute;
                if (uig.GroupType == this.GetType())
                {
                    var sys = World.CreateSystem(s);
                    childSystems.Add(sys);
                    AddSystemToUpdateList(sys);
                    break;
                }
            }
        }
    }

    public void DestroyGroup()
    {
        foreach (var s in childSystems)
        {
            var sGroup = s as ManualComponentSystemGroup;
            if (sGroup != null)
                sGroup.DestroyGroup();
            else
                World.DestroySystem(s);
        }
        childSystems.Clear();
        World.DestroySystem(this);
    }

    protected override void OnDestroy()
    {
        if (World.AllWorlds.Contains(World))
            GameDebug.Assert(childSystems.Count == 0);

        base.OnDestroy();
    }
}
