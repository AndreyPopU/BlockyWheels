using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaignMovement : MonoBehaviour
{
    public float speed;
    public Transform[] waypoints;
    public int currentWaypoint;
    public bool reversed = false;
    public BoxCollider coreCollider;
    public GameObject GFX;
    PlayerControls controls;

    [HideInInspector]
    public Rigidbody rb;
    [HideInInspector]
    public bool canMove = true;
    public bool coroutineFinished;
    public int input;
    public int lastInput;

    private Vector2 movement;

    private void Awake()
    {
        controls = new PlayerControls();

        if (SaveLoadManager.IntToBool(PlayerPrefs.GetInt(SaveLoadManager.invertedControlsString)))
            controls.Gameplay.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
        else controls.Gameplay.Move.performed += ctx => movement = new Vector3(ctx.ReadValue<Vector2>().y, -ctx.ReadValue<Vector2>().x);

        controls.Gameplay.Reset.performed += ctx => GameManager.instance.ResetLevel();
        if (PlayerPrefs.HasKey(SaveLoadManager.lastCheckpointString)) currentWaypoint = PlayerPrefs.GetInt(SaveLoadManager.lastCheckpointString);
        else currentWaypoint = 1;

        if (PlayerPrefs.HasKey(SaveLoadManager.lastRotationString)) 
            transform.rotation = Quaternion.Euler(new Vector3(0, PlayerPrefs.GetInt(SaveLoadManager.lastRotationString),0));
        else transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 20;
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
    private void FixedUpdate()
    {
        if (!canMove) return;

        if (coroutineFinished)
        {
            if (movement.x > 0)
            {
                if (Mathf.Abs(speed) < 1200) speed += 100 * lastInput;
                else speed = 1200 * lastInput;
            }
            else
            {
                if (speed > 0) speed -= 40 * lastInput;
                else speed = 0;
            }
        }

        // Movement
        rb.velocity = transform.right * speed * Time.fixedDeltaTime;

        // Rotation
        if (!reversed) input = Mathf.RoundToInt(-movement.y);
        else input = Mathf.RoundToInt(movement.y);

        if (input != 0 && coroutineFinished && input != lastInput)
            StartCoroutine(FlipRotation(input));

        // Waypoint shit
        if (coroutineFinished)
        {
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), 
                new Vector2(waypoints[currentWaypoint].position.x, waypoints[currentWaypoint].position.z)) < 5) currentWaypoint++;

            Quaternion rot = Quaternion.LookRotation(waypoints[currentWaypoint].position - transform.position);
            Quaternion rotation = Quaternion.Euler(0, rot.eulerAngles.y - 90 * lastInput, transform.eulerAngles.z + 1.5f * -lastInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, .001f * Mathf.Abs(speed) / 4);
        }
    }

    IEnumerator FlipRotation(int _input)
    {
        //Rotate GFX and reverse speed
        // Reverse waypoints
        System.Array.Reverse(waypoints);
        currentWaypoint = waypoints.Length - currentWaypoint;
        coroutineFinished = false;
        lastInput = _input;
        float rotation = -180;
        if (_input == 1) rotation = 0;
        int loops = 0;

        float currentSpeed = Mathf.Abs(speed);

        if (GFX.transform.localRotation.eulerAngles.y - rotation > 3) // Mathf.Abs(transform.rotation.eulerAngles.y - rotation) > 3
        {
            while (loops < 20)
            {
                GFX.transform.localRotation = Quaternion.Lerp(GFX.transform.localRotation, 
                    Quaternion.Euler(new Vector3(0, rotation, 0)), .25f); // 0.075
                loops++;
                speed += (currentSpeed / 10) * lastInput;
                yield return null;
            }

            speed = currentSpeed * _input;
            GFX.transform.localRotation = Quaternion.Euler(new Vector3(0, rotation, 0));
        }

        coroutineFinished = true;
    }
}
