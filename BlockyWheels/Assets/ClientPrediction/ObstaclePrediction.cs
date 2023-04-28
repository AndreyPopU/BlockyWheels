using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct ObstacleState : NetworkMessage
{
    public int tick;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Quaternion rotation;
}

public struct ObstacleLaunch : NetworkMessage
{
    public int tick;
    public uint netID;
    public float speed;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Quaternion rotation;
}

public class ObstaclePrediction : MonoBehaviour
{
    public CarMovement owner;
    public Obstacle obstacle;

    // Sync server/client time
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    // Store inputs and positions
    private ObstacleState[] obstacleStateBuffer = new ObstacleState[BUFFER_SIZE];
    private ObstacleLaunch[] obstacleInputBuffer = new ObstacleLaunch[BUFFER_SIZE];
    private ObstacleState serverObstacleState;
    private ObstacleLaunch obstacleInputState;
    private int lastCorrectedTick;

    private Rigidbody rb;
    public float hitSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        if (!NetworkClient.active) return;

        // NOTE: RegisterHandler doesn't work, but this does, so there's that
        // Create a Handler to handle received SimulationStates from server
        NetworkClient.ReplaceHandler<ObstacleState>(OnObstacleStateReceived);
    }

    void Update()
    {
        if (!NetworkClient.active) return;

        // Calculate Server ticks
        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }

        if (!obstacle.launched) return;

        obstacleInputState = new ObstacleLaunch
        {
            netID = GetComponent<NetworkIdentity>().netId,
            speed = hitSpeed,
            rotation = transform.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
        };
    }

    public void HandleTick()
    {
        if (!obstacle.launched) return;

        obstacleInputState.tick = currentTick;

        ProcessState(obstacleInputState);

        if (owner != null && StateIsValid(obstacleInputState)) owner.SendPrediction(obstacleInputState);

        ////////////////////////////////////////////////////////////////////////////
        // Check if reconciliation is needed
        //if (serverObstacleState.position != null) Reconciliate();

        ObstacleState obstacleState = CurrentObstacleState(obstacleInputState);

        // Add state and input to cache
        int bufferIndex = currentTick % BUFFER_SIZE;

        obstacleStateBuffer[bufferIndex] = obstacleState;

        obstacleInputBuffer[bufferIndex] = obstacleInputState;
    }

    public void ProcessState(ObstacleLaunch state)
    {
        // If state is null, no movement happened

        transform.rotation = state.rotation;
        rb.velocity = state.velocity;
        rb.angularVelocity = state.angularVelocity;
    }

    public bool StateIsValid(ObstacleLaunch state)
    {
        if (state.velocity.magnitude > 0 ||
            state.angularVelocity.magnitude > 0 ||
            state.tick > -1 || state.rotation.eulerAngles.magnitude > 0)
            return true;

        return false;
    }

    public ObstacleState CurrentObstacleState(ObstacleLaunch state) // Converts InputState to SimulationState
    {
        return new ObstacleState()
        {
            tick = state.tick,
            position = transform.position,
            rotation = transform.rotation,
            velocity = rb.velocity,
            angularVelocity = rb.angularVelocity,
        };
    }

    private void OnObstacleStateReceived(NetworkConnection conn, ObstacleState state)
    {
        // If client receives a new server SimulationState update, update current one
        if (serverObstacleState.tick < state.tick)
        {
            serverObstacleState = state;
        }
    }

    public void Reconciliate()
    {
        // Don't reconciliate for old states.
        if (serverObstacleState.tick <= lastCorrectedTick) return;

        int bufferIndex = serverObstacleState.tick % BUFFER_SIZE;

        // Obtain the cached input and simulation states.
        ObstacleLaunch cachedObstacleInput = obstacleInputBuffer[bufferIndex];
        ObstacleState cachedSimulationState = obstacleStateBuffer[bufferIndex];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedSimulationState.position == null || cachedObstacleInput.velocity.magnitude > 0 || cachedObstacleInput.rotation.eulerAngles.magnitude > 0)
        {
            transform.position = serverObstacleState.position;
            transform.rotation = serverObstacleState.rotation;
            rb.velocity = serverObstacleState.velocity;
            rb.angularVelocity = serverObstacleState.angularVelocity;

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedTick = serverObstacleState.tick;

            return;
        }

        // Find the difference between the vector's values. 
        float difference = Vector3.Distance(cachedSimulationState.position, serverObstacleState.position);

        //  Maximum distance before correction
        float tolerance = .2f;

        // A correction is necessary.
        if (difference > tolerance)
        {
            Debug.Log("We need to reconcile bro!");

            // Set the player's position to match the server's state. 
            transform.position = serverObstacleState.position;
            transform.rotation = serverObstacleState.rotation;
            rb.velocity = serverObstacleState.velocity;
            rb.angularVelocity = serverObstacleState.angularVelocity;

            // Declare the rewindFrame as we're about to resimulate our cached inputs. 
            int rewindFrame = serverObstacleState.tick;

            // Loop through and apply cached inputs until we're 
            // caught up to our current simulation frame. 
            while (rewindFrame < currentTick)
            {
                // Determine the cache index 
                int rewindCacheIndex = rewindFrame % BUFFER_SIZE;

                // Obtain the cached input and simulation states.
                ObstacleLaunch rewindCachedInputState = obstacleInputBuffer[rewindCacheIndex];
                ObstacleState rewindCachedSimulationState = obstacleStateBuffer[rewindCacheIndex];

                // If there's no state to simulate, for whatever reason, 
                // increment the rewindFrame and continue.
                if (rewindCachedSimulationState.position == null || rewindCachedInputState.velocity.magnitude > 0 || rewindCachedInputState.rotation.eulerAngles.magnitude > 0)
                {
                    ++rewindFrame;
                    continue;
                }

                ProcessState(rewindCachedInputState);

                // Replace the simulationStateCache index with the new value.
                ObstacleState rewoundObstacleState = CurrentObstacleState(obstacleInputState);
                rewoundObstacleState.tick = rewindFrame;
                obstacleStateBuffer[rewindCacheIndex] = rewoundObstacleState;

                // Increase the amount of frames that we've rewound.
                ++rewindFrame;
            }
        }

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedTick =  serverObstacleState.tick;
    }
}