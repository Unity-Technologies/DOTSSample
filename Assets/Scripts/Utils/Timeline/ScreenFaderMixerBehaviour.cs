using System;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ScreenFaderMixerBehaviour : PlayableBehaviour
{
    bool m_FirstFrameHappened;

    Exposure m_Exposure;
    Volume m_FadeVolume;

    public override void OnPlayableCreate(Playable playable)
    {
        base.OnPlayableCreate(playable);

        var layer = LayerMask.NameToLayer("PostProcess Volumes");
        if (layer == -1)
            GameDebug.LogWarning("Unable to find layer mask for camera fader");

        var gameObject = new GameObject()
        {
            name = "ScreenFaderMixerBehaviour Quick Volume",
            layer = layer,
            hideFlags = HideFlags.HideAndDontSave
        };

        m_FadeVolume = gameObject.AddComponent<Volume>();
        m_FadeVolume.priority = 100.0f;
        m_FadeVolume.isGlobal = true;
        var profile = m_FadeVolume.profile;

        m_Exposure = profile.Add<Exposure>();
        m_Exposure.mode.Override(ExposureMode.Automatic);
        m_Exposure.compensation.Override(0);
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!m_FirstFrameHappened)
        {
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount();

        float blendedExposure = 0.0f;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<ScreenFaderBehaviour> inputPlayable = (ScriptPlayable<ScreenFaderBehaviour>)playable.GetInput(i);
            ScreenFaderBehaviour input = inputPlayable.GetBehaviour();

            blendedExposure += input.exposure * inputWeight;
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately(inputWeight, 0f))
                currentInputs++;
        }

        m_Exposure.compensation.Override(blendedExposure + 0.5f * (1.0f - totalWeight));
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        m_FirstFrameHappened = false;

        m_FadeVolume.enabled = false;
        GameObject.DestroyImmediate(m_FadeVolume.gameObject);
        m_FadeVolume = null;

        GameObject.DestroyImmediate(m_Exposure);
        m_Exposure = null;
    }
}
