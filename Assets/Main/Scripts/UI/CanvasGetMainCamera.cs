using UnityEngine;
using UnityEngine.UI;

public class CanvasGetMainCamera : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;        
    }
}
