using UnityEngine;

[ExecuteInEditMode]
public class Anchor : MonoBehaviour
{
    [SerializeField] private Transform anchorTo;
    [SerializeField] private Vector2   anchorOffset;
    
    private void Update()
    {
        if (anchorTo != null)
        {
            transform.position = new Vector3(anchorTo.position.x + anchorOffset.x, anchorTo.position.y + anchorOffset.y, 0.0f);
        }
    }
}
