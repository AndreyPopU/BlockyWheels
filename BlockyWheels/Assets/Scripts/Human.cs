using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Human : MonoBehaviour
{
    public float speed;
    public bool crossRoad;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        Vector3 desPos;
        Vector3 desRot;

        if (transform.position.z > 0)
        {
            desPos = new Vector3(transform.position.x, transform.position.y, -19);
            desRot = new Vector3(0, 180, 0);
        }
        else
        {
            desPos = new Vector3(transform.position.x, transform.position.y, 11.25f);
            desRot = Vector3.zero;
        }

        if (crossRoad) StartCoroutine(Walk(desPos, desRot));
    }

    private void Update()
    {
        if (!crossRoad) transform.Translate(-Vector3.forward * speed * Time.deltaTime);
    }

    IEnumerator Walk(Vector3 desiredPos, Vector3 desiredRot) // -19 11.25
    {
        // Jump and rotate
        animator.SetTrigger("Jump");
        while (Quaternion.Angle(transform.rotation, Quaternion.Euler(desiredRot)) > 5)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(desiredRot), .5f);
            yield return null;
        }

        // Wait
        yield return new WaitForSeconds(.5f);

        // Wait till reaches
        while (Vector3.Distance(transform.position, desiredPos) > .25f)
        {
            // Walk
            transform.position = Vector3.MoveTowards(transform.position, desiredPos, speed * Time.deltaTime);
            //transform.position = Vector3.Lerp(transform.position, desiredPos, .01f);
            yield return null;
        }

        // When reaches wait again
        yield return new WaitForSeconds(.5f);

        Vector3 desPos;
        Vector3 desRot;

        if (transform.position.z > 0)
        {
            desPos = new Vector3(transform.position.x, transform.position.y, -19);
            desRot = new Vector3(0, 180, 0);
        }
        else
        {
            desPos = new Vector3(transform.position.x, transform.position.y, 11.25f);
            desRot = Vector3.zero;
        }

        // Repeat
        StartCoroutine(Walk(desPos, desRot));
    }
}
