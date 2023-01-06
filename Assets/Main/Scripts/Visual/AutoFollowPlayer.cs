using UnityEngine;
using Cinemachine;

public class AutoFollowPlayer : MonoBehaviour
{
    private void Start()
    {
        GetComponent<CinemachineVirtualCamera>().Follow = GameObject.FindGameObjectWithTag("Player").transform;
    }
}
