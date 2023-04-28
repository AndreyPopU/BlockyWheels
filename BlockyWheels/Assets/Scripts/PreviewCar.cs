using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewCar : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Animator>().SetFloat("idleMultiplier", .65f);
    }

    void FixedUpdate()
    {
        transform.Rotate(Vector3.up, 150 * Time.fixedDeltaTime);
    }
}
