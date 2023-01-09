using UnityEngine;

public class AudioVolumeSetting : MonoBehaviour
{
    private AudioSource m_audioSource;

    private void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (m_audioSource != null)
        {
            m_audioSource.volume = Mathf.Clamp01(SoundManager.musicVolume);
        }
    }
}
