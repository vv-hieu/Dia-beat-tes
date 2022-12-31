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

        GameContext res = new GameContext(player);

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
        public Player player;
        // Timer, world, ... sum shid like dat

        public GameContext(Player player)
        {
            this.player = player;
        }
    }
}
