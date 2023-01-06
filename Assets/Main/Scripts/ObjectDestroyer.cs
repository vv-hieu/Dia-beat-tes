using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDestroyer : MonoBehaviour
{
    public void DestroyObject(GameObject obj)
    {
        Destroy(obj);
    }

    public void DestroyObjectWithTag(string tag) 
    {
        Destroy(GameObject.FindGameObjectWithTag(tag));
    }

    public void DestroyObjectsWithTag(string tag)
    {
        GameObject[] go = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in go)
        {
            Destroy(obj);
        }
    }
}
