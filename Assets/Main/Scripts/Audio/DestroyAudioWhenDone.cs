using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAudioWhenDone : MonoBehaviour
{
    private void Start()
    {
        if (TryGetComponent(out AudioSource audio))
        {
            StartCoroutine(p_DestroyAudioSourceWhenDone(audio));
        }
    }

    private IEnumerator p_DestroyAudioSourceWhenDone(AudioSource audio)
    {
        while (audio.isPlaying)
        {
            yield return new WaitForEndOfFrame();
        }
        Destroy(audio.gameObject);
    }
}
