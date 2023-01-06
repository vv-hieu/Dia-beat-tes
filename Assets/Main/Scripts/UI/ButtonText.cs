using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonText : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Transform text;
    [SerializeField] private float     normalOffset;
    [SerializeField] private float     pressedOffset;

    public void OnPointerDown(PointerEventData eventData)
    {
        text.position = transform.position + Vector3.up * pressedOffset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        text.position = transform.position + Vector3.up * normalOffset;
    }
}
