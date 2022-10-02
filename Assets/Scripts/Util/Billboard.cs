using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        transform.position = WorldManager.Instance.worldCenter.position - 2 * (Camera.main.transform.position - WorldManager.Instance.worldCenter.position);
        transform.LookAt(Camera.main.transform.position);
    }
}