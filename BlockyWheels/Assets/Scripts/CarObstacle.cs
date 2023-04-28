using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarObstacle : Obstacle
{
    public float speed;
    public float turnSpeed;
    public bool changingLane;

    [Header("Lanes")]
    public float currentLaneX;
    public int laneIndex;
    public bool canChangeLane = true;
    public bool caresAboutLaw = true;

    private float baseSpeed;
    private float baseRotationY;


    public override void Start()
    {
        baseRotationY = transform.rotation.eulerAngles.y;
        base.Start();
        rb = GetComponent<Rigidbody>();

        baseSpeed = speed;
        currentLaneX = transform.position.z;

        laneIndex = 0;

        for (int i = 0; i < GameManager.instance.carLanes.Length; i++)
        {
            if (currentLaneX == GameManager.instance.carLanes[i]) laneIndex = i;
        }

        if (canChangeLane) Invoke("DelayChangeLane", Random.Range(5, 10));
    }

    void FixedUpdate()
    {
        if (launched || !GameManager.instance.started) return;

        //speed = CarMovement.instance.speed - CarMovement.instance.speed / 1.65f;
        //speed = Mathf.Clamp(speed, 600, 1000);

        rb.velocity = transform.right * speed * Time.fixedDeltaTime;

        RaycastHit hit;
        Ray ray = new Ray(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, transform.right);

        if (!caresAboutLaw) return;

        if (!changingLane)
        {
            if (Physics.Raycast(ray, out hit, 5))
            {
                if (hit.collider.GetComponent<CarObstacle>() || hit.collider.gameObject.tag == "RedLight") // If raycast hits anything - slow down
                {
                    if (speed > 0) speed -= 20;
                    else speed = 0;
                }
            }
            else
            {
                if (speed < 600) speed += 20; // else speed up
            }
        }

        Debug.DrawRay(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, transform.right * 5);
        Debug.DrawRay(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, transform.right * 100);
    }

    public override void OnTriggerEnter(Collider other)
    {
        //if (!collided)
        //{
        //    if (other.GetComponent<CarObstacle>()) (gameObject);
        //}

        StopAllCoroutines();

        base.OnTriggerEnter(other);
    }

    void DelayChangeLane() // Invoked
    {
        StartCoroutine(ChangeLane());
    }

    public IEnumerator ChangeLane()
    {
        if (!isServer) yield break;

        if (speed < 590) yield break;

        speed /= 1.25f;

        // Chose Lane to change to and check if it's a valid lane
        int yRotation = 0;
        int chance = Random.Range(0, 2);
        if (chance == 0) yRotation = -1;
        else yRotation = 1;

        if (laneIndex == 0) yRotation = -1;
        else if (laneIndex == 3) yRotation = 1;

        laneIndex -= yRotation;

        Vector3 direction = transform.forward;
        if (yRotation == 1) direction *= -1;

        // Raycast to check if there is a car in the merging lane
        RaycastHit hit;
        Ray rayfront = new Ray(transform.position + Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
        Ray raymid = new Ray(transform.position, direction * 5);
        Ray rayback = new Ray(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
        Ray trafficLight = new Ray(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, transform.right * 100);

        // If Raycast hits anything return
        if (Physics.Raycast(rayfront, out hit, 8f)) yield break;
        if (Physics.Raycast(raymid, out hit, 8f)) yield break;
        if (Physics.Raycast(rayback, out hit, 8f)) yield break;
        if (Physics.Raycast(trafficLight, out hit, 100))
        {
            if (hit.collider.gameObject.CompareTag("RedLight")) yield break;
        }

        changingLane = true;

        if (yRotation == 1) // Change lane Right
        {
            while (transform.rotation.eulerAngles.y < baseRotationY + 30)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, yRotation * turnSpeed, 0)), .125f);
                yield return new WaitForSecondsRealtime(.02f);

                // Old way of changing lane
                //transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, yRotation * turnSpeed * rb.velocity.normalized.magnitude, 0)), .125f);

                Debug.DrawRay(transform.position + Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
                Debug.DrawRay(transform.position, direction * 5);
                Debug.DrawRay(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
            }
        }
        else // Change lane Left
        {
            while (transform.rotation.eulerAngles.y > baseRotationY - 30)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, yRotation * turnSpeed, 0)), .125f);
                yield return new WaitForSecondsRealtime(.02f);

                // Old way of changing lane
                //transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, yRotation * turnSpeed * rb.velocity.normalized.magnitude, 0)), .125f);

                Debug.DrawRay(transform.position + Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
                Debug.DrawRay(transform.position, direction * 5);
                Debug.DrawRay(transform.position - Vector3.right * coreCollider.bounds.size.x / 2, direction * 5);
            }
        }

        while (true) // While car isn't in new lane
        {
            if (yRotation == 1)
            {
                if (transform.position.z > GameManager.instance.carLanes[laneIndex] - 1.75f) // + 3
                    break;
            }
            else
            {
                if (transform.position.z < GameManager.instance.carLanes[laneIndex] + 1.75f) // - 3
                    break;
            }

            yield return new WaitForSecondsRealtime(.02f);
        }

        print("Changing lane8");
        // Go back to going forward
        while (transform.rotation.eulerAngles.y < baseRotationY - 3 || transform.rotation.eulerAngles.y > baseRotationY + 3)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, -yRotation * turnSpeed * rb.velocity.normalized.magnitude, 0)), .125f);
            yield return new WaitForSecondsRealtime(.02f);
        }

        transform.rotation = Quaternion.Euler(0, baseRotationY, 0);
        speed = baseSpeed;
        changingLane = false;

        Invoke("DelayChangeLane", Random.Range(5, 10));
    }
}
