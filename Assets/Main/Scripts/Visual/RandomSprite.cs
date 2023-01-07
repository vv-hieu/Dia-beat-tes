using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;

    private SpriteRenderer m_sprite;

    public void Randomize()
    {
        if (m_sprite != null)
        {
            if (sprites.Length > 0)
            {
                m_sprite.sprite = sprites[Random.Range(0, sprites.Length)];
            }
            else
            {
                m_sprite.sprite = null;
            }
        }
    }

    private void Awake()
    {
        m_sprite = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Randomize();
    }
}

#if UNITY_EDITOR
namespace MyEditor
{
    [CustomEditor(typeof(RandomSprite))]
    public class RandomSpriteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RandomSprite randomizer = (RandomSprite)target;
            DrawDefaultInspector();
            if (GUILayout.Button("Randomize"))
            {
                randomizer.Randomize();
            }
        }
    }
}
#endif