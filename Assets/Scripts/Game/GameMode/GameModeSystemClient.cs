using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine.UI;

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class GameModeSystemClient : JobComponentSystem
{
    EntityQuery PlayersGroup;
    EntityQuery GameModesGroup;

    int m_PlayerId;
    Entity m_Player;

    public GameModeSystemClient()
    {
        if (Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.scoreboardPanel.uiBinding.Clear(); ;
            Game.game.clientFrontend.gameScorePanel.Clear();
        }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        PlayersGroup = GetEntityQuery(typeof(Player.State));
        GameModesGroup = GetEntityQuery(typeof(GameModeData));
    }

    public void Shutdown()
    {
        if (Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.scoreboardPanel.uiBinding.Clear(); ;
            Game.game.clientFrontend.gameScorePanel.Clear();
        }
    }

    // TODO : We need to fix up these dependencies
    public void SetLocalPlayerId(int playerId)
    {
        m_PlayerId = playerId;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (Game.game.clientFrontend == null)
            return default;

        var scoreboardUI = Game.game.clientFrontend.scoreboardPanel.uiBinding;
        var overlayUI = Game.game.clientFrontend.gameScorePanel;

        using (var playerEntityArray = PlayersGroup.ToEntityArray(Allocator.Persistent))
        using (var playerStateArray = PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent))
        using (var gameModeArray = GameModesGroup.ToComponentDataArray<GameModeData>(Allocator.Persistent))
        {
            // Update individual player stats
            // Use these indexes to fill up each of the team lists
            var scoreBoardPlayerIndexes = new int[scoreboardUI.teams.Length];

            for (int i = 0, c = playerStateArray.Length; i < c; ++i)
            {
                var playerEntity = playerEntityArray[i];
                var playerState = playerStateArray[i];
                var teamIndex = playerState.teamIndex;

                // TODO (petera) this feels kind of hacky
                if (playerState.playerId == m_PlayerId)
                    m_Player = playerEntity;

                var teamColor = Color.white;
                int scoreBoardColumn = 0;
                if (m_Player != Entity.Null)
                {
                    var ps = EntityManager.GetComponentData<Player.State>(m_Player);

                    bool friendly = teamIndex == ps.teamIndex;
                    teamColor = friendly ? Game.game.gameColors[(int)Game.GameColor.Friend] : Game.game.gameColors[(int)Game.GameColor.Enemy];
                    scoreBoardColumn = friendly ? 0 : 1;
                }

                var idx = scoreBoardPlayerIndexes[scoreBoardColumn]++;

                var column = scoreboardUI.teams[scoreBoardColumn];

                // If too few,
                while (idx >= column.playerScores.Count)
                {
                    var entry = GameObject.Instantiate<TMPro.TextMeshProUGUI>(column.playerScoreTemplate, column.playerScoreTemplate.transform.parent);
                    entry.gameObject.SetActive(true);
                    var trans = (RectTransform)entry.transform;
                    var tempTrans = ((RectTransform)column.playerScoreTemplate.transform);
                    trans.localPosition = tempTrans.localPosition - new Vector3(0, tempTrans.rect.height * idx, 0);
                    column.playerScores.Add(entry);
                }

                column.playerScores[idx].Format("{0} : {1}", playerState.playerName.ToString(), playerState.score);

                column.playerScores[idx].color = teamColor;
            }

            // Clear all member text fields that was not used
            for (var teamIndex = 0; teamIndex < scoreboardUI.teams.Length; teamIndex++)
            {
                var numPlayers = scoreBoardPlayerIndexes[teamIndex];
                var column = scoreboardUI.teams[teamIndex];
                for (var i = column.playerScores.Count - 1; i >= numPlayers; --i)
                {
                    GameObject.Destroy(column.playerScores[i].gameObject);
                    column.playerScores.RemoveAt(i);
                }
            }

            if (m_Player == Entity.Null)
                return default;

            var localPlayerState = EntityManager.GetComponentData<Player.State>(m_Player);

            // Update gamemode overlay
            GameDebug.Assert(gameModeArray.Length < 2);
            if (gameModeArray.Length > 0)
            {
                GameModeData gameMode = gameModeArray[0];
                if (localPlayerState.displayGameResult)
                {
                    overlayUI.message.text = localPlayerState.gameResult.ToString();
                }
                else
                    overlayUI.message.text = "";

                var timeLeft = System.TimeSpan.FromSeconds(gameMode.gameTimerSeconds);

                overlayUI.timer.Format("{0}:{1:00}", timeLeft.Minutes, timeLeft.Seconds);
                overlayUI.timerMessage.Set(ref gameMode.gameTimerMessage);
                overlayUI.objective.Set(ref localPlayerState.goalString);
                overlayUI.SetObjectiveProgress(localPlayerState.goalCompletion, (int)localPlayerState.goalAttackers, (int)localPlayerState.goalDefenders, Game.game.gameColors[localPlayerState.goalDefendersColor], Game.game.gameColors[localPlayerState.goalAttackersColor]);

                // Update team scores on hud
                var friendColor = Game.game.gameColors[(int)Game.GameColor.Friend];
                var enemyColor = Game.game.gameColors[(int)Game.GameColor.Enemy];
                overlayUI.team1Score.Format("{0}", localPlayerState.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
                overlayUI.team1Score.color = friendColor;
                overlayUI.team2Score.Format("{0}", localPlayerState.teamIndex == 0 ? gameMode.teamScore1 : gameMode.teamScore0);
                overlayUI.team2Score.color = enemyColor;

                // Update team score and name on scoreboard
                scoreboardUI.teams[0].score.Format("{0}", localPlayerState.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
                scoreboardUI.teams[1].score.Format("{0}", localPlayerState.teamIndex == 0 ? gameMode.teamScore1 : gameMode.teamScore0);
                scoreboardUI.teams[0].name.Set(ref (localPlayerState.teamIndex == 0 ? ref gameMode.teamName0 : ref gameMode.teamName1));
                scoreboardUI.teams[1].name.Set(ref (localPlayerState.teamIndex == 0 ? ref gameMode.teamName1 : ref gameMode.teamName0));
            }

            overlayUI.action.text = localPlayerState.actionString.ToString();
        }

        return default;
    }
}
