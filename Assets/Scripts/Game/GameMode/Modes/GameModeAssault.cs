using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using System;
using Unity.Collections;
using Unity.Sample.Core;

public class GameModeAssault : IGameMode
{
    static readonly int s_AttackTeam = 0;
    static readonly int s_DefendTeam = 1;

    [ConfigVar(Name = "game.assault.minplayers", DefaultValue = "2", Description = "Minimum players before match starts")]
    public static ConfigVar minPlayers;
    [ConfigVar(Name = "game.assault.roundlength", DefaultValue = "420", Description = "Round length (seconds)")]
    public static ConfigVar roundLength;
    [ConfigVar(Name = "game.assault.prematchtime", DefaultValue = "10", Description = "Time before match starts")]
    public static ConfigVar preMatchTime;
    [ConfigVar(Name = "game.assault.postmatchtime", DefaultValue = "10", Description = "Time after match ends before new will begin")]
    public static ConfigVar postMatchTime;

    EntityQuery m_PlayersGroup;
    EntityQuery m_CapturePointGroup;

    public void Initialize(World world, GameModeSystemServer gameModeSystemServer)
    {
        m_World = world;
        m_GameModeSystemServer = gameModeSystemServer;

        m_PlayersGroup = m_GameModeSystemServer.GetEntityQuery(typeof(Player.State));
        m_CapturePointGroup = m_GameModeSystemServer.GetEntityQuery(typeof(CapturePoint));

        // Create teams
        m_GameModeSystemServer.CreateTeam("Attackers");
        m_GameModeSystemServer.CreateTeam("Defenders");

        Console.Write("Assault game mode initialized");
    }

    public void Restart()
    {
        GameDebug.Log("Restarting gamemode...");
        var captures = m_CapturePointGroup.ToComponentArray<CapturePoint>();
        for (var i = 0; i < captures.Length; i++)
        {
            var c = captures[i];
            GameDebug.Log("Capture " + c.objectiveName + " reset");
            c.status = CapturePoint.Status.Locked;
            c.captured = 0;
        }
        m_Phase = Phase.PreGame;
        m_GameModeSystemServer.StartGameTimer(preMatchTime, "PreMatch");
        m_ActiveCapturePoint = null;
        SelectNextCapturePoint();
    }

    public void Shutdown()
    {
    }

    char[] _msgBuf = new char[256];
    public void Update()
    {
        switch (m_Phase)
        {
            case Phase.PreGame:
                if (m_GameModeSystemServer.GetGameTimer() == 0)
                {
                    var playerStateArray = m_PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent);
                    if (playerStateArray.Length < minPlayers.IntValue)
                    {
                        m_GameModeSystemServer.chatSystem.SendChatAnnouncement("Waiting for more players.");
                        m_GameModeSystemServer.StartGameTimer(preMatchTime, "PreMatch");
                    }
                    else
                    {
                        m_GameModeSystemServer.StartGameTimer(roundLength, "");
                        m_Phase = Phase.Active;
                        var l = StringFormatter.Write(ref _msgBuf, 0, "Match started! {0} is attacking!", m_GameModeSystemServer.teams[s_AttackTeam].name);
                        m_GameModeSystemServer.chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                    }
                    playerStateArray.Dispose();
                }
                break;
            case Phase.Active:

                int winTeam = -1;
                if (m_GameModeSystemServer.GetGameTimer() == 0)
                {
                    winTeam = s_DefendTeam;
                }

                if (m_ActiveCapturePoint != null)
                {
                    UpdateCurrentCapturePoint();
                    if (m_ActiveCapturePoint.status == CapturePoint.Status.Completed)
                    {
                        var l = StringFormatter.Write(ref _msgBuf, 0, "{0} captured {1}. {2}/{3}", m_GameModeSystemServer.teams[s_AttackTeam].name, m_ActiveCapturePoint.objectiveName, m_NumCaptured + 1, m_NumCapturePoints);
                        m_GameModeSystemServer.chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                        SelectNextCapturePoint();
                        if (m_ActiveCapturePoint == null)
                        {
                            winTeam = s_AttackTeam;
                        }
                    }
                }
                if (winTeam > -1)
                {
                    var l = StringFormatter.Write(ref _msgBuf, 0, "Match over! {0} won!", m_GameModeSystemServer.teams[winTeam].name);
                    m_GameModeSystemServer.chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                    var players = m_PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent);
                    for (var i = 0; i < players.Length; i++)
                    {
                        var p = players[i];
                        p.displayGameResult = true;
                        p.gameResult = new NativeString64(p.teamIndex == winTeam ? "VICTORY" : "DEFEAT");
                        p.displayGoal = false;
                        p.goalString = new NativeString64();
                        p.goalCompletion = -1.0f;

                        if (p.controlledEntity != Entity.Null)
                        {
                            var healthState = m_World.EntityManager
                                .GetComponentData<HealthStateData>(p.controlledEntity);
                            healthState.health = 0.0f;
                            healthState.deathTick = -1;
                            m_World.EntityManager
                                .SetComponentData(p.controlledEntity, healthState);
                        }
                    }
                    m_Phase = Phase.PostGame;
                    m_GameModeSystemServer.SetRespawnEnabled(false);
                    m_GameModeSystemServer.StartGameTimer(postMatchTime, "PostMatch");
                }
                break;
            case Phase.PostGame:
                if (m_GameModeSystemServer.GetGameTimer() == 0)
                {
                    var players = m_PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent);
                    for (var i = 0; i < players.Length; i++)
                    {
                        var playerState = players[i];
                        playerState.displayGameResult = false;
                    }
                    m_GameModeSystemServer.Restart();
                }
                break;
        }
    }

    public void OnPlayerJoin(ref Player.State playerState)
    {
        playerState.score = 0;
        m_GameModeSystemServer.AssignTeam(ref playerState);
    }

    public void OnPlayerKilled(ref Player.State victim,ref Player.State killer)
    {
        if (killer.teamIndex != victim.teamIndex)
        {
            killer.score++;
        }
    }

    bool InsideCylinder(Vector3 position, Vector3 cylinderBase, float height, float radius)
    {
        if (position.y < cylinderBase.y || position.y > cylinderBase.y + height)
            return false;
        if (new Vector2(position.x - cylinderBase.x, position.z - cylinderBase.z).magnitude > radius)
            return false;
        return true;
    }

    void UpdateCurrentCapturePoint()
    {
        // Count attackers and defenders in capture zone
        var attacking = 0;
        var defending = 0;

        var capturePosition = m_ActiveCapturePoint.transform.position;
        var defendersBasePoint = m_DefendersBasePoint == null ? Vector3.zero : m_DefendersBasePoint.transform.position;
        var attackersBasePoint = m_AttackersBasePoint == null ? Vector3.zero : m_AttackersBasePoint.transform.position;

        var players = m_PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent);

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player.controlledEntity == Entity.Null)
                continue;

            var healthState = m_World.EntityManager.GetComponentData<HealthStateData>(player.controlledEntity);

            // Skip dead
            if (healthState.health <= 0)
                continue;

            var charPredictedState = m_World.EntityManager.GetComponentData<Character.PredictedData>(player.controlledEntity);
            var position = charPredictedState.position;

            bool insideActive = InsideCylinder(position, capturePosition, m_ActiveCapturePoint.height, m_ActiveCapturePoint.radius);

            if (insideActive)
            {
                if (player.teamIndex == s_DefendTeam)
                    ++defending;
                else
                    ++attacking;
            }

            // Is char switch allowed?
            bool switchOk = false;
            if (player.teamIndex == s_DefendTeam && m_DefendersBasePoint != null && InsideCylinder(position, defendersBasePoint, m_DefendersBasePoint.height, m_DefendersBasePoint.radius))
                switchOk = true;

            if (player.teamIndex == s_AttackTeam && m_AttackersBasePoint != null && InsideCylinder(position, attackersBasePoint, m_AttackersBasePoint.height, m_AttackersBasePoint.radius))
                switchOk = true;

            foreach (var b in m_GameModeSystemServer.teamBases)
            {
                if (b.teamIndex == player.teamIndex)
                {
                    var inside = (b.boxCollider.transform.InverseTransformPoint(position) - b.boxCollider.center);
                    if (Mathf.Abs(inside.x) < b.boxCollider.size.x * 0.5f && Mathf.Abs(inside.y) < b.boxCollider.size.y * 0.5f && Mathf.Abs(inside.z) < b.boxCollider.size.z * 0.5f)
                    {
                        switchOk = true;
                    }
                }
            }
            player.enableCharacterSwitch = switchOk;
        }

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];

            // TODO (petera) ok to brute force settings every frame?
            player.displayGoal = true;
            player.goalPosition = capturePosition;
            player.goalString = new NativeString64(player.teamIndex == s_AttackTeam ? m_ActiveCaptureMessage : m_ActiveDefendMessage);
            player.goalCompletion = m_ActiveCapturePoint.captured;
            player.goalDefendersColor = player.teamIndex == s_AttackTeam ? (uint)Game.GameColor.Enemy : (uint)Game.GameColor.Friend;
            player.goalAttackersColor = player.teamIndex == s_AttackTeam ? (uint)Game.GameColor.Friend : (uint)Game.GameColor.Enemy;
            player.goalAttackers = (uint)attacking;
            player.goalDefenders = (uint)defending;
        }

        var status = CapturePoint.Status.Active;
        if (defending > 0 && attacking > 0)
            status = CapturePoint.Status.Contested;
        else
        {
            var gameTimeSystem = m_World.GetExistingSystem<GameTimeSystem>();

            if (attacking > 0)
            {
                float attackMultiplier = Mathf.Sqrt(attacking); // Somewhat random sub-linear scale
                m_ActiveCapturePoint.captured = Mathf.Clamp01(m_ActiveCapturePoint.captured + gameTimeSystem.GetWorldTime().tickInterval * attackMultiplier / m_ActiveCapturePoint.captureTime);
                if (m_ActiveCapturePoint.captured == 1.0f)
                    status = CapturePoint.Status.Completed;
                else
                    status = CapturePoint.Status.Capturing;
            }
            else if (defending > 0)
            {
                float defendMultiplier = Mathf.Sqrt(defending); // Somewhat random sub-linear scale
                m_ActiveCapturePoint.captured = Mathf.Clamp01(m_ActiveCapturePoint.captured - gameTimeSystem.GetWorldTime().tickInterval * defendMultiplier / m_ActiveCapturePoint.captureTime);
                status = CapturePoint.Status.Healing;
            }
            else
                status = CapturePoint.Status.Active;
        }
        if (status != m_ActiveCapturePoint.status)
        {
            GameDebug.Log(string.Format("Capture Point {0} switched from {1} to {2}", m_ActiveCapturePoint.name, m_ActiveCapturePoint.status.ToString(), status.ToString()));
            m_ActiveCapturePoint.status = status;
        }
    }

    List<CapturePoint> sortedCapturePoints = new List<CapturePoint>();
    void SelectNextCapturePoint()
    {
        var capturePoints = m_CapturePointGroup.ToComponentArray<CapturePoint>();
        sortedCapturePoints.Clear();
        for (var i = 0; i < capturePoints.Length; i++)
            sortedCapturePoints.Add(capturePoints[i]);
        sortedCapturePoints.Sort(CapturePointComparer);

        m_NumCapturePoints = capturePoints.Length;

        // Count captured
        m_NumCaptured = 0;
        m_DefendersBasePoint = null;
        m_ActiveCapturePoint = null;
        m_AttackersBasePoint = null;
        foreach (var c in sortedCapturePoints)
        {
            if (c.status == CapturePoint.Status.Completed)
            {
                m_NumCaptured++;
                m_AttackersBasePoint = c;
            }
            else
            {
                if (m_ActiveCapturePoint == null)
                    m_ActiveCapturePoint = c;
                else if (m_DefendersBasePoint == null)
                    m_DefendersBasePoint = c;
            }
        }

        if (m_ActiveCapturePoint != null)
        {
            m_ActiveCapturePoint.status = CapturePoint.Status.Active;
            m_ActiveCaptureMessage = "Attack '" + m_ActiveCapturePoint.objectiveName + "'";
            m_ActiveDefendMessage = "Defend '" + m_ActiveCapturePoint.objectiveName + "'";
        }

        GameDebug.Log("Updated spawnpoint: " + m_ActiveCapturePoint + ":" + m_DefendersBasePoint + m_AttackersBasePoint);
    }

    int CapturePointComparer(CapturePoint x, CapturePoint y)
    {
        return x.captureIndex.CompareTo(y.captureIndex);
    }

    int m_LastSpawnIdx = 0;
    public void OnPlayerRespawn(ref Player.State playerState, ref Vector3 position, ref Quaternion rotation)
    {
        SpawnPoint[] spawns = null;
        if (playerState.teamIndex == s_AttackTeam && m_AttackersBasePoint != null)
            spawns = m_AttackersBasePoint.spawns;
        else if (playerState.teamIndex == s_DefendTeam && m_DefendersBasePoint != null)
            spawns = m_DefendersBasePoint.spawns;

        // Spawn at capture point if possible
        if (spawns != null && spawns.Length > 0)
        {
            int l = spawns.Length;
            m_LastSpawnIdx = (m_LastSpawnIdx + 1) % l;
            var spawn = spawns[m_LastSpawnIdx];
            position = spawn.transform.position;
            rotation = spawn.transform.rotation;
        }
        else
        {
            GameDebug.Log("Spawning at home base");
            // Spawn at home base
            m_GameModeSystemServer.GetRandomSpawnTransform(playerState.teamIndex, ref position, ref rotation);
        }
    }

    enum Phase
    {
        Undefined,
        PreGame,
        Active,
        PostGame,
    }

    Phase m_Phase;
    CapturePoint m_ActiveCapturePoint;
    CapturePoint m_AttackersBasePoint;
    CapturePoint m_DefendersBasePoint;
    string m_ActiveCaptureMessage;
    string m_ActiveDefendMessage;
    int m_NumCaptured;
    int m_NumCapturePoints;
    World m_World;
    GameModeSystemServer m_GameModeSystemServer;
}
