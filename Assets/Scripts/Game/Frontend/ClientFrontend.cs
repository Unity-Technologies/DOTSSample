using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;

public class ClientFrontend : MonoBehaviour
{
    public ScoreBoard scoreboardPanel;
    public GameScore gameScorePanel;
    public ChatPanel chatPanel;
    public ServerPanel serverPanel;
    public bool m_ShowScorePanel;

#pragma warning disable 649
    [SerializeField] CountDown countDownPanel;
    [SerializeField] MainMenu mainMenu;
    [SerializeField] SoundDef uiHighlightSound;
    [SerializeField] SoundDef uiSelectSound;
    [SerializeField] SoundDef uiSelectLightSound;
    [SerializeField] SoundDef uiCloseSound;
#pragma warning restore 649

    Canvas m_ScoreboardPanelCanvas;
    Canvas m_GameScorePanelCanvas;
    Canvas m_CountDownPanelCanvas;
    Canvas m_ChatPanelCanvas;

    public enum MenuShowing
    {
        None,
        Main,
        Ingame
    }

    Interpolator m_MenuFader = new Interpolator(0.0f, Interpolator.CurveType.SmoothStep);

    public MenuShowing menuShowing { get; private set; } = MenuShowing.None;

    public int ActiveMainMenuNumber
    {
        get { return mainMenu.gameObject.activeSelf ? mainMenu.activeSubmenuNumber : -1; }
    }


    // Audio for menus. Called from events on the ui elements
    public void OnHighlight() { SoundSystem.Instance.Play(uiHighlightSound); }
    public void OnSelect() { SoundSystem.Instance.Play(uiSelectSound); }
    public void OnClose() { SoundSystem.Instance.Play(uiCloseSound); }

    void Awake()
    {
        m_ScoreboardPanelCanvas = scoreboardPanel.GetComponent<Canvas>();
        m_GameScorePanelCanvas = gameScorePanel.GetComponent<Canvas>();
        m_CountDownPanelCanvas = countDownPanel.GetComponent<Canvas>();
        m_ChatPanelCanvas = chatPanel.GetComponent<Canvas>();
        Clear();
    }

    public void Clear()
    {
        scoreboardPanel.SetPanelActive(false);
        gameScorePanel.SetPanelActive(false);
        countDownPanel.SetPanelActive(false);
        mainMenu.SetPanelActive(MenuShowing.None);
        chatPanel.SetPanelActive(true); // active always as it has its own display/hide logic
        chatPanel.ClearMessages();
        serverPanel.SetPanelActive(false);
    }

    public void ShowMenu(MenuShowing show, float fadeTime = 0.0f)
    {
        if (menuShowing == show)
            return;
        menuShowing = show;
        m_MenuFader.MoveTo(show != MenuShowing.None ? 1.0f : 0.0f, fadeTime);
        if (menuShowing != MenuShowing.None)
            SoundSystem.Instance.Play(uiSelectLightSound);
        else
            SoundSystem.Instance.Play(uiCloseSound);
    }

    public void UpdateGame()
    {
        mainMenu.UpdateMenus();

        // Show/Hide fully for debug purposes
        var show = IngameHUD.showHud.IntValue > 0;
        if (m_ChatPanelCanvas.enabled != show)
        {
            m_ScoreboardPanelCanvas.enabled = show;
            m_GameScorePanelCanvas.enabled = show;
            m_CountDownPanelCanvas.enabled = show;
            m_ChatPanelCanvas.enabled = show;
        }

        // Toggle menu if not in editor
        if (!Application.isEditor && Input.GetKeyUp(KeyCode.Escape))
        {
            if (menuShowing == MenuShowing.None)
            {
                // What menu should we show?
                // Show main menu if no level loaded or menu level loaded
                if (Game.game.levelManager.currentLevel == null || Game.game.levelManager.currentLevel.name == "level_menu")
                    Console.EnqueueCommandNoHistory("menu 1 0.2");
                else
                    Console.EnqueueCommandNoHistory("menu 2 0.2");
            }
            else
            {
                Console.EnqueueCommandNoHistory("menu 0 0.2");
                InputSystem.RequestMousePointerLock();
            }
        }

        // Fade main menu
        var fade = m_MenuFader.GetValue();
        var active = fade > 0.0f;
        if (mainMenu.GetPanelActive() != active)
            mainMenu.SetPanelActive(menuShowing);
        if (active)
            mainMenu.SetAlpha(fade);
    }

    public void UpdateChat(ChatSystemClient chatSystem)
    {
        chatPanel.Tick(chatSystem);
    }

    // Force showing of score board e.g. when dead
    public void SetShowScorePanel(bool showScorePanel)
    {
        m_ShowScorePanel = showScorePanel;
    }

    public void UpdateIngame(ref Player.State playerState)
    {
        // Countdown
        countDownPanel.SetPanelActive(playerState.displayCountDown);
        if (playerState.displayCountDown)
            countDownPanel.levelInfoCounter.Format("{0}", playerState.countDown);

        // Scoreboard
        scoreboardPanel.SetPanelActive(!playerState.displayCountDown && (playerState.displayScoreBoard || GatedInput.GetKey(KeyCode.Tab) || m_ShowScorePanel));

        // Game score panel
        gameScorePanel.SetPanelActive(playerState.displayGameScore);
    }
}

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
class ClientFrontendUpdate : JobComponentSystem
{
    EntityQuery m_gameModeGroup;
    EntityQuery m_localPlayerGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int count = 0;
        var PlayerStateFromEntity = GetComponentDataFromEntity<Player.State>(true);
        Entities
            .WithReadOnly(PlayerStateFromEntity)
            .WithoutBurst() // Captures managed data
            .ForEach((Entity entity, ref LocalPlayer localPlayer) =>
        {
            count++;
            if(PlayerStateFromEntity.HasComponent(localPlayer.playerEntity))
            {
                var playerState = PlayerStateFromEntity[localPlayer.playerEntity];
                Game.game.clientFrontend.UpdateIngame(ref playerState);
            }
        }).Run();

        GameDebug.Assert(count == 1, "There should only be one localplayer. Found:{0}", count);
        return default;
    }
}
