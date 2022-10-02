using UnityEngine;
using System.Collections;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;

public class HoverObject : MonoBehaviour
{
    [SerializeField] private bool invertRotation;
    void Update()
    {
        transform.position = Vector3.up * Mathf.Cos(Time.time);
        transform.Rotate(invertRotation ? Vector3.down : Vector3.up, 0.05f);
    }
}