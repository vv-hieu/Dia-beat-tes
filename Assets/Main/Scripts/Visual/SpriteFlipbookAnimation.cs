using System.Collections.Generic;
using UnityEngine;

public class SpriteFlipbookAnimation : MonoBehaviour
{
    [SerializeField] private List<Sprite> sprites     = new List<Sprite>();
    [SerializeField] private bool         loop        = false;
    [SerializeField] private bool         playOnAwake = false;
    [SerializeField] private float        duration;

    public int  currentFrame { get; private set; } = 0;
    public bool isPlaying    { get; private set; } = false;

    private SpriteRenderer m_sprite;
    private float          m_time = 0.0f;

    public void Play()
    {
        m_time    = 0.0f;
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void SetDuration(float duration)
    {
        this.duration = duration;
    }

    private void Awake()
    {
        m_sprite = GetComponent<SpriteRenderer>();

        isPlaying = playOnAwake;
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
    }

    private void Update()
    {
        if (isPlaying)
        {
            float t = m_time / duration;

            if (loop)
            {
                t = t - Mathf.Floor(t);
                p_SetFrame((int)Mathf.Clamp(t * sprites.Count, 0.0f, sprites.Count - 0.01f));
            }
            else
            {
                if (t >= 0.0f && t <= 1.0f)
                {
                    p_SetFrame((int)Mathf.Clamp(t * sprites.Count, 0.0f, sprites.Count - 0.01f));
                }
                else
                {
                    p_SetFrame(-1);
                    m_time = 0.0f;
                    isPlaying = false;
                }
            }

            m_time += Time.deltaTime;
        }
    }

    private void p_SetFrame(int index)
    {
        if (index < 0 || index >= sprites.Count)
        {
            m_sprite.sprite = null;
            currentFrame    = -1;
        }
        else
        {
            m_sprite.sprite = sprites[index];
            currentFrame    = index;
        }
    }
}
