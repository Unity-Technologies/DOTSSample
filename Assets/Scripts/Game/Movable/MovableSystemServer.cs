using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class MovableSystemServer
{
    int spawnNum;

    public MovableSystemServer(World world)
    {
        m_GameWorld = world;
        Console.AddCommand("spawnbox", CmdSpawnBox, "Spawn <n> boxes", GetHashCode());
        Console.AddCommand("despawnboxes", CmdDespawnBoxes, "Despawn all boxes", GetHashCode());
    }

    private void CmdDespawnBoxes(string[] args)
    {
        foreach (var box in m_Movables)
        {
            PrefabAssetManager.DestroyEntity(m_GameWorld.EntityManager, box);
        }
        m_Movables.Clear();
    }

    private void CmdSpawnBox(string[] args)
    {
        if (args.Length > 0)
            int.TryParse(args[0], out spawnNum);
        else
            spawnNum = 1;
        spawnNum = Mathf.Clamp(spawnNum, 1, 100);
    }

    public void Shutdown()
    {
        Console.RemoveCommandsWithTag(GetHashCode());
    }

    public void Update()
    {
        if (spawnNum <= 0)
            return;
        spawnNum--;

        int x = spawnNum % 10 - 5;
        int z = spawnNum / 10 - 5;

        var movable = PrefabAssetManager.CreateEntity(m_GameWorld.EntityManager,Game.game.movableBoxPrototype);

        var transform = m_GameWorld.EntityManager.GetComponentObject<Transform>(movable);
        transform.position = new Vector3(40 + x * 3, 30, 30 + z * 3);// level_00: new Vector3(-20+x*3,10,-10+z*3)
        transform.rotation =  UnityEngine.Random.rotation;

        m_Movables.Add(movable);
    }

    private List<Entity> m_Movables = new List<Entity>();

    private World m_GameWorld;
}
