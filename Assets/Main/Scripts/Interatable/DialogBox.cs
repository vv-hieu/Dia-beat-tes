using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogBox : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private Vector2 boxSize             = Vector2.one;
    [SerializeField] private float   appearTime          = 0.8f;
    [SerializeField] private float   charactersPerSecond = 5.0f;
    [SerializeField] private float   timeBetweenLines    = 2.0f;
    [SerializeField] private float   padding             = 0.02f;
    [SerializeField] private int     fontSize            = 10;
    [SerializeField] private Color   fontColor;

    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private TextMeshPro    text;

    private bool             m_init = false;
    private List<string>     m_lines;
    private float            m_timeBetweenCharacters;
    private Vector2          m_boxSize;
    private OnDialogFinished m_callback;

    private void Start()
    {
        m_boxSize = Vector2.zero;
        {
            sprite.size = m_boxSize;
        }
        if (text != null)
        {
            text.GetComponent<RectTransform>().sizeDelta = m_boxSize - Vector2.one * padding;
        }
        p_SetString("");
    }

    private void OnValidate()
    {
        if (sprite != null)
        {
            sprite.size = boxSize;
        }
        if (text != null)
        {
            text.GetComponent<RectTransform>().sizeDelta = boxSize - Vector2.one * padding;

            text.fontSize = fontSize;
            text.color    = fontColor;
        }
    }

    public void Display(IEnumerable<string> lines)
    {
        if (!m_init)
        {
            m_init                  = true;
            m_lines                 = new List<string>(lines);
            m_timeBetweenCharacters = 1.0f / Mathf.Max(0.0001f, charactersPerSecond);

            if (m_lines.Count == 0)
            {
                Destroy(gameObject);
                return;
            }
            StartCoroutine(p_AppearAsync());
        }
    }

    public void Display(IEnumerable<string> lines, Vector2 boxSize)
    {
        if (!m_init)
        {
            m_init = true;
            m_lines = new List<string>(lines);
            m_timeBetweenCharacters = 1.0f / Mathf.Max(0.0001f, charactersPerSecond);

            this.boxSize = boxSize;

            if (m_lines.Count == 0)
            {
                Destroy(gameObject);
                return;
            }
            StartCoroutine(p_AppearAsync());
        }
    }

    public void Display(IEnumerable<string> lines, OnDialogFinished callback)
    {
        if (!m_init)
        {
            m_init = true;
            m_lines = new List<string>(lines);
            m_timeBetweenCharacters = 1.0f / Mathf.Max(0.0001f, charactersPerSecond);
            m_callback = callback;

            if (m_lines.Count == 0)
            {
                if (m_callback != null)
                {
                    m_callback();
                }
                Destroy(gameObject);
                return;
            }
            StartCoroutine(p_AppearAsync());
        }
    }

    public void Display(IEnumerable<string> lines, Vector2 boxSize, OnDialogFinished callback)
    {
        if (!m_init)
        {
            m_init = true;
            m_lines = new List<string>(lines);
            m_timeBetweenCharacters = 1.0f / Mathf.Max(0.0001f, charactersPerSecond);
            m_callback = callback;

            this.boxSize = boxSize;

            if (m_lines.Count == 0)
            {
                if (m_callback != null)
                {
                    m_callback();
                }
                Destroy(gameObject);
                return;
            }
            StartCoroutine(p_AppearAsync());
        }
    }

    private void p_SetString(string str)
    {
        if (text != null)
        {
            text.text = str;
        }
    }

    private IEnumerator p_AppearAsync()
    {
        float time = 0.0f;
        m_boxSize = Vector2.zero;

        while (time < appearTime)
        {
            m_boxSize = (time / appearTime) * boxSize;

            if (sprite != null)
            {
                sprite.size = m_boxSize;
            }
            if (text != null)
            {
                text.GetComponent<RectTransform>().sizeDelta = m_boxSize - Vector2.one * padding;

                text.fontSize = fontSize;
                text.color   = fontColor;
            }

            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        if (sprite != null)
        {
            sprite.size = boxSize;
        }
        if (text != null)
        {
            text.GetComponent<RectTransform>().sizeDelta = boxSize - Vector2.one * padding;

            text.fontSize = fontSize;
            text.color   = fontColor;
        }

        yield return StartCoroutine(p_DisplayAsync(0));
    }

    private IEnumerator p_DisplayAsync(int index)
    {
        if (index < m_lines.Count)
        {
            string str  = "";
            float  time = 0.0f;
            int    idx  = 0;

            p_SetString(str);
            while (str.Length < m_lines[index].Length)
            {
                if (time >= m_timeBetweenCharacters)
                {
                    time = 0.0f;
                    str += m_lines[index][idx];
                    p_SetString(str);
                    ++idx;
                }
                time += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(timeBetweenLines);
            yield return StartCoroutine(p_DisplayAsync(index + 1));
        }
        else
        {
            if (m_callback != null)
            {
                m_callback();
            }
            Destroy(gameObject);
        }
    }

    public delegate void OnDialogFinished();
}
