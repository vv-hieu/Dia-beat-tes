using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foo : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<DialogBox>().Display(new string[] {
                "Hello there!",
                "I'm the lorax.",
                "I speak for the trees.",
            });
        }
    }
}
