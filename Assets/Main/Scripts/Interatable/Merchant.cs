using UnityEngine;

public class Merchant : MonoBehaviour
{
    private NPC npc;

    public void PostSelect()
    {
        npc.SetLines(new string[] {
            "Looking for something?"
        });
    }

    private void Awake()
    {
        npc = GetComponent<NPC>();
    }
}
