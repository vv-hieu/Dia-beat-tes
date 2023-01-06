using UnityEngine;

public class WeaponSmith : MonoBehaviour
{
    private NPC npc;

    public void PostSelect()
    {
        npc.SetLines(new string[] {
            "Changed your mind?"
        });
    }

    private void Awake()
    {
        npc = GetComponent<NPC>();
    }
}
