using System.IO;
using UnityEngine;

public class GameManager
{
    private float m_timer          = 0.0f;
    private int   m_completedLevel = 0;
    private int   m_totalCoinCount = 0;

    private static string PERSISTENT_DATA_PATH = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "data.json";

    public static GameContext GetGameContext()
    {
        if (File.Exists(PERSISTENT_DATA_PATH))
        {
            using StreamReader reader = new StreamReader(PERSISTENT_DATA_PATH);
            string json = reader.ReadToEnd();

            PersistentData data = JsonUtility.FromJson<PersistentData>(json);
            instance.m_totalCoinCount = data.coinCount;
        }
        else
        {
            instance.m_totalCoinCount = 0;
        }

        Player player = null;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<Player>();
        }

        Map map = null;
        GameObject mapObj = GameObject.FindGameObjectWithTag("Map");
        if (mapObj != null)
        {
            map = mapObj.GetComponent<Map>();
        }

        GameContext res = new GameContext(player, map, instance.m_timer, instance.m_completedLevel, instance.m_totalCoinCount);

        return res;
    }

    public static void GetVolumeSettings(out float music, out float sfx)
    {
        if (File.Exists(PERSISTENT_DATA_PATH))
        {
            using StreamReader reader = new StreamReader(PERSISTENT_DATA_PATH);
            string json = reader.ReadToEnd();

            PersistentData data = JsonUtility.FromJson<PersistentData>(json);
            music = data.musicVolume;
            sfx   = data.soundFxVolume;
        }
        else
        {
            music = 1.0f;
            sfx   = 1.0f;
        }
    }

    public static void SetVolumeSettings(float music, float sfx)
    {
        PersistentData data = new PersistentData();
        data.coinCount     = 0;
        data.musicVolume   = 1.0f;
        data.soundFxVolume = 1.0f;
        if (File.Exists(PERSISTENT_DATA_PATH))
        {
            using StreamReader reader = new StreamReader(PERSISTENT_DATA_PATH);
            string json1 = reader.ReadToEnd();

            data = JsonUtility.FromJson<PersistentData>(json1);
        }
        data.musicVolume   = music;
        data.soundFxVolume = sfx;

        string json2 = JsonUtility.ToJson(data);

        using StreamWriter writer = new StreamWriter(PERSISTENT_DATA_PATH);
        writer.Write(json2);
    }

    public static void ResetTimer()
    {
        instance.m_timer = 0.0f;
    }

    public static void AddTimer(float dt)
    {
        instance.m_timer += dt;
    }

    public static void ResetLevel()
    {
        instance.m_completedLevel = 0;
    }

    public static void CompleteLevel()
    {
        ++instance.m_completedLevel;
    }

    public static void SetTotalCoinCount(int amount)
    {
        instance.m_totalCoinCount = amount;

        PersistentData data = new PersistentData();
        data.coinCount     = 0;
        data.musicVolume   = 1.0f;
        data.soundFxVolume = 1.0f;
        if (File.Exists(PERSISTENT_DATA_PATH))
        {
            using StreamReader reader = new StreamReader(PERSISTENT_DATA_PATH);
            string json1 = reader.ReadToEnd();

            data = JsonUtility.FromJson<PersistentData>(json1);
        }
        data.coinCount = amount;

        string json2 = JsonUtility.ToJson(data);

        using StreamWriter writer = new StreamWriter(PERSISTENT_DATA_PATH);
        writer.Write(json2);
    }

    public static bool SpendCoin(int amount)
    {
        if (instance.m_totalCoinCount >= amount)
        {
            SetTotalCoinCount(instance.m_totalCoinCount - amount);
            return true;
        }
        return false;
    }

    private GameManager()
    {
    }

    private static GameManager _instance;

    public static GameManager instance 
    { 
        get
        {
            if (_instance == null)
            {
                _instance = new GameManager();
            }
            return _instance;
        }
    }

    public struct GameContext
    {
        public Player player          { get; private set; }
        public Map    map             { get; private set; }
        public float  timeSinceStart  { get; private set; }
        public int    completedLevels { get; private set; }
        public int    totalCoinCount  { get; private set; }

        public GameContext(Player player, Map map, float timeSinceStart, int completedLevels, int totalCoinCount)
        {
            this.player          = player;
            this.map             = map;
            this.timeSinceStart  = timeSinceStart;
            this.completedLevels = completedLevels;
            this.totalCoinCount  = totalCoinCount;
        }
    }

    public struct PersistentData
    {
        public int   coinCount;
        public float musicVolume;
        public float soundFxVolume;
    }
}
