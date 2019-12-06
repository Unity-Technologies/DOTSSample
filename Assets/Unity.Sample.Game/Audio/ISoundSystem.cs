using UnityEngine;
using UnityEngine.Audio;

public interface ISoundSystem
{
    void Init(AudioMixer mixer);
    void SetRegistry(SoundRegistry registry);
    void SetCurrentListener(AudioListener audioListener);
    SoundSystem.SoundHandle Play(SoundDef soundDef);
    SoundSystem.SoundHandle Play(SoundDef soundDef, Transform parent);
    SoundSystem.SoundHandle Play(SoundDef soundDef, Vector3 position);
    SoundSystem.SoundHandle Play(WeakAssetReference weakSoundDef, Vector3 position);
    void UpdatePosition(ref SoundSystem.SoundHandle handle, Vector3 position);
    bool IsValid(ref SoundSystem.SoundHandle handle);
    void Stop(SoundSystem.SoundHandle sh, float fadeOutTime = 0);
    void Update();
}
