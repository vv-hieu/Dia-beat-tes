using UnityEngine;

public class CurseMaster : MonoBehaviour
{
    private NPC npc;

    public void PostSelect()
    {
        npc.SetLines(new string[] {
            "Cursed are powerful, ...",
            "but they come with a cost.",
            "Use them at your own risk"
        });

        npc.preDialog  = new NPC.NPCEvent();
        npc.postDialog = new NPC.NPCEvent();
    }

    private void Awake()
    {
        npc = GetComponent<NPC>();    
    }
}
