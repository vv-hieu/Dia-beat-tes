using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameContext GetGameContext()
    {
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

        GameContext res = new GameContext(player, map);

        return res;
    }

    public static GameManager instance { get; private set; }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("There can only be one instance of GameManager.");
        }
    }

    public struct GameContext
    {
        public Player player { get; private set; }
        public Map    map    { get; private set; }

        public GameContext(Player player, Map map)
        {
            this.player = player;
            this.map    = map;
        }
    }
}
