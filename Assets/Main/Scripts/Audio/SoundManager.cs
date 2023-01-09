using UnityEngine;

public static class SoundManager
{
    public static float musicVolume       = 1.0f;
    public static float soundEffectVolume = 1.0f;

    public static void PlaySound(AudioClip audio)
    {
        if (audio != null)
        {
            GameObject go = new GameObject("SFX");
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.PlayOneShot(audio);
            audioSource.volume = soundEffectVolume;
            go.AddComponent<DestroyAudioWhenDone>();
        }
    }

    public static void ObtainSettings()
    {
        GameManager.GetVolumeSettings(out musicVolume, out soundEffectVolume);
    }

    public static void SaveSettings()
    {
        GameManager.SetVolumeSettings(musicVolume, soundEffectVolume);
    }
}
