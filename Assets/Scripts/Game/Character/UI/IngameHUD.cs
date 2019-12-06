using Unity.Sample.Core;
using UnityEngine;

public class IngameHUD : MonoBehaviour
{
    [ConfigVar(Name = "show.hud", DefaultValue = "1", Description = "Show HUD")]
    public static ConfigVar showHud;

    public HUDCrosshair m_Crosshair;
    public HUDGoal m_Goal;
    public CharacterHealthUI m_Health;

    Canvas m_Canvas;

    public void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
    }

    public void SetPanelActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void FrameUpdate()
    {
        var show = showHud.IntValue > 0;
        if (m_Canvas.enabled != show)
            m_Canvas.enabled = show;

        // TODO reenable these
        //m_Crosshair.FrameUpdate(cameraSettings);
        //m_Goal.FrameUpdate(ref playerState);
    }

    public void ShowHitMarker(bool lethal)
    {
        SoundSystem.Instance.Play(lethal ? m_LethalHitSound : m_HitMarkerSound);
        m_Crosshair.ShowHitMarker(lethal);
    }

#pragma warning disable 649
    [SerializeField] SoundDef m_HitMarkerSound;
    [SerializeField] SoundDef m_LethalHitSound;
#pragma warning restore 649
}
