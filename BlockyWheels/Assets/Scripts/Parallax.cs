using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float length;
    private float startPos;

    void Start()
    {
        startPos = transform.position.x;
        if (length == 0) length = GetComponent<Collider>().bounds.size.x;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CarMovement>())
        {
            startPos -= length;
            transform.position = new Vector3(startPos, transform.position.y, transform.position.z);
        }
    }
}
