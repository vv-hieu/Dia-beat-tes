using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temp : MonoBehaviour
{
    public GameObject cakePiece;

    private void Start()
    {
        for (float x = 2.0f; x <= 12.0f; x += 1.0f)
        {
            for (float y = 2.0f; y <= 12.0f; y += 1.0f)
            {
                Instantiate(cakePiece, new Vector3(x, y, 0.0f), Quaternion.identity, transform);
            }
        }
    }
}
