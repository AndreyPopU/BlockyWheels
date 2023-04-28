using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public Transform target;
    public CarMovement car;
    public Vector3 offset;
    public int spectateIndex;
    public int spectateTargetCount;

    public List<CarMovement> unfinishedCars;
    [HideInInspector]
    public Camera cam;
    public Vector3 centerPoint;
    private CarMovement targetCar;
    private Rigidbody targetRb;

    private float zoom;
    private float zDistance;

    void Awake()
    {
        instance = this;
        centerPoint = transform.position;
        cam = GetComponent<Camera>();
        unfinishedCars = new List<CarMovement>();
        targetCar = target.GetComponent<CarMovement>();
        targetRb = target.GetComponent<Rigidbody>();
    }

    public IEnumerator ChangeFOV(int desire)
    {
        if (cam.fieldOfView < desire)
        {
            while (cam.fieldOfView < desire)
            {
                cam.fieldOfView += .5f;
                yield return null;
            }
        }
        else
        {
            while (cam.fieldOfView > desire)
            {
                cam.fieldOfView -= .5f;
                yield return null;
            }
        }
    }

    private void Update()
    {
        if (car != null)
        {
            // After finishing control spectate
            if (car.finished)
            {
                if (targetCar == null || targetCar.finished) ChangeSpectate(1);
            }

            // Shake camera when going fast
            if (car.speed > car.minMaxSpeed.y - 300 && !car.finished && !GameManager.instance.pause)
            {
                float randomX = Random.Range(transform.position.x - .05f, transform.position.x + .05f);
                float randomY = Random.Range(transform.position.y - .05f, transform.position.y + .05f);
                transform.position = new Vector3(randomX, randomY, transform.position.z);
            }
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        GameManager.instance.transform.position = new Vector3(target.position.x - 140, GameManager.instance.transform.position.y, GameManager.instance.transform.position.z);

        if (car == null)
            transform.position = Vector3.Lerp(transform.position, target.position + offset, .125f);
        else
        {
            if (target.name == "LocalCar")
            {
                transform.position = Vector3.Lerp(transform.position, target.position + offset, .125f);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 50 + targetRb.velocity.magnitude / 1.5f, .25f);
            }
        }
    }

    public void ChangeSpectate(int value)
    {
        if (!car.hasAuthority) return;

        // When a car finishes, call ChangeSpectate for every camera spectating that player, if it is the last car to finish, set target to null
        if (!car.finished) return;

        if (target == null) return;

        CarMovement[] cars = FindObjectsOfType<CarMovement>();

        for (int i = 0; i < cars.Length; i++)
        {
            if (!cars[i].finished) unfinishedCars.Add(cars[i]);
        }
            
        if (unfinishedCars.Count == 0)
        {
            GameManager.instance.spectatePanel.gameObject.SetActive(false);
            target = null;
            targetCar = null;
            StartCoroutine(ChangeFOV(60));
        }
        else GameManager.instance.spectatePanel.gameObject.SetActive(true);

        if (unfinishedCars.Count <= 1) return;

        spectateIndex += value;

        if (spectateIndex < 0) spectateIndex = unfinishedCars.Count - 1;
        else if (spectateIndex >= unfinishedCars.Count) spectateIndex = 0;

        print("Changing target to index: " + unfinishedCars[spectateIndex].transform);
        target = unfinishedCars[spectateIndex].transform;
        targetCar = target.GetComponent<CarMovement>();
        GameManager.instance.spectateName.text = targetCar.playerName;
        targetRb = target.GetComponent<Rigidbody>();

        unfinishedCars.Clear();
    }
}
