using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private SerializableDictionary<string, GameObject> collectibles = new SerializableDictionary<string, GameObject>();
    [SerializeField] private SerializableDictionary<string, GameObject> relics       = new SerializableDictionary<string, GameObject>();

    public GameObject GetCollectible(string name)
    {
        if (collectibles.TryGetValue(name, out GameObject collectible))
        {
            return collectible;
        }
        return null;
    }

    public GameObject GetRelic(string name)
    {
        if (relics.TryGetValue(name, out GameObject relic))
        {
            return relic;
        }
        return null;
    }

    public static ItemManager instance { get; private set; }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("There can only be one instance of CollectibleManager.");
        }
    }
}
