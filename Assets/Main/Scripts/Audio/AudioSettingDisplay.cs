using UnityEngine;
using UnityEngine.UI;

public class AudioSettingDisplay : MonoBehaviour
{
    [SerializeField] private Slider musicVolume;
    [SerializeField] private Slider soundFxVolume;

    public void Start()
    {
        SoundManager.ObtainSettings();

        if (musicVolume != null)
        {
            musicVolume.value = SoundManager.musicVolume;
        }
        if (soundFxVolume != null)
        {
            soundFxVolume.value = SoundManager.soundEffectVolume;
        }
    }

    public void Update()
    {
        bool changed = false;
        if (musicVolume != null)
        {
            if (SoundManager.musicVolume != musicVolume.value)
            {
                SoundManager.musicVolume = musicVolume.value;
                changed = true;
            }
        }
        if (soundFxVolume != null)
        {
            if (SoundManager.soundEffectVolume != soundFxVolume.value)
            {
                SoundManager.soundEffectVolume = soundFxVolume.value;
                changed = true;
            }
        }
        if (changed)
        {
            SoundManager.SaveSettings();
        }
    }
}
