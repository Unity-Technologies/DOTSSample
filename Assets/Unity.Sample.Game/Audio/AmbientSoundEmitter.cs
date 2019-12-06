using UnityEngine;

public class AmbientSoundEmitter : MonoBehaviour
{
    #pragma warning disable 649
    public SoundDef sound;

    SoundSystem.SoundHandle handle;
    #pragma warning restore 649

    void Start()
    {
        SoundSystem.Instance.Play(sound);
    }

    void OnDisable()
    {
        SoundSystem.Instance.Stop(handle, 4.0f);
    }
}
