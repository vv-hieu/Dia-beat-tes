using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundManager
{
    public static void PlaySound(AudioClip audio)
    {
        if (audio != null)
        {
            GameObject go = new GameObject("SFX");
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.PlayOneShot(audio);
            go.AddComponent<DestroyAudioWhenDone>();
        }
    }
}
