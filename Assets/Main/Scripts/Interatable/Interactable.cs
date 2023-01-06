using UnityEngine;

public class Interactable : MonoBehaviour
{
    public virtual void OnApproach(Player player)
    {
    }

    public virtual void OnInteract(Player player)
    {
    }

    public virtual void OnLeave(Player player)
    {
    }
}
