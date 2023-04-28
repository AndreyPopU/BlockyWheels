using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct InputState : NetworkMessage
{
    public int tick;
    public Vector3 inputVector;
}

public struct SimulationState : NetworkMessage
{
    public int tick;
    public Vector3 position;
    public Vector3 velocity;
}

public class Client : MonoBehaviour
{
    public bool serverAuthority;
    public bool playerControlled;
    public float speed = 5;

    // Sync server/client time
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;

    // Store inputs and positions
    private const int BUFFER_SIZE = 1024;
    private SimulationState[] simulationStateBuffer = new SimulationState[BUFFER_SIZE];
    private InputState[] inputStateBuffer = new InputState[BUFFER_SIZE];
    private SimulationState serverSimulationState;
    private InputState inputState;
    private int lastCorrectedTick;

    private Rigidbody rb;

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
        NetworkClient.ReplaceHandler<SimulationState>(OnSimulationStateReceived);
    }

    void Update()
    {
        if (!NetworkClient.active) return;

        if (playerControlled)
        {
            // Update input
            inputState = new InputState
            {
                inputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"))
            };
        }

        // Calculate Server ticks
        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }
    }

    public void HandleTick()
    {
        if (playerControlled)
        {
            inputState.tick = currentTick;

            // Update client position
            ProcessInputs(inputState);

            // Send to server
            //NetworkClient.Send(inputState);
        }

        // Check if reconciliation is needed
        if (serverSimulationState.position != null) Reconciliate();

        SimulationState simulationState = CurrentSimulationState(inputState);

        // Add state and input to cache
        int bufferIndex = currentTick % BUFFER_SIZE;

        simulationStateBuffer[bufferIndex] = simulationState;

        if (playerControlled) inputStateBuffer[bufferIndex] = inputState;
    }

    private void OnSimulationStateReceived(NetworkConnection conn, SimulationState state)
    {
        // If client receives a new server SimulationState update, update current one
        if (serverSimulationState.tick < state.tick)
        {
            serverSimulationState = state;
        }
    }

    public SimulationState CurrentSimulationState(InputState input) // Converts InputState to SimulationState
    {
        return new SimulationState()
        {
            tick = input.tick,
            position = transform.position,
            velocity = rb.velocity,
        };
    }

    public void ProcessInputs(InputState state)
    {
        // If state is null, no movement happened
        if (state.inputVector == null)
        {
            state.inputVector = Vector3.zero;
        }

        rb.velocity = state.inputVector * speed;
    }

    public void Reconciliate()
    {
        // Don't reconciliate for old states.
        if (serverSimulationState.tick <= lastCorrectedTick) return;

        int bufferIndex = serverSimulationState.tick % BUFFER_SIZE;

        // Obtain the cached input and simulation states.
        InputState cachedInputState = inputStateBuffer[bufferIndex];
        SimulationState cachedSimulationState = simulationStateBuffer[bufferIndex];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedInputState.inputVector == null || cachedSimulationState.position == null)
        {
            transform.position = serverSimulationState.position;
            rb.velocity = serverSimulationState.velocity;

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedTick = serverSimulationState.tick;

            return;
        }

        // Find the difference between the vector's values. 
        float difference = Vector3.Distance(cachedSimulationState.position, serverSimulationState.position);

        //  Maximum distance before correction
        float tolerance = .2f;

        if (serverAuthority) tolerance = 0;

        // A correction is necessary.
        if (difference > tolerance)
        {
            Debug.Log("We need to reconcile bro!");

            // Set the player's position to match the server's state. 
            transform.position = serverSimulationState.position;
            rb.velocity = serverSimulationState.velocity;

            // Declare the rewindFrame as we're about to resimulate our cached inputs. 
            int rewindFrame = serverSimulationState.tick;

            // Loop through and apply cached inputs until we're 
            // caught up to our current simulation frame. 
            while (rewindFrame < currentTick)
            {
                // Determine the cache index 
                int rewindCacheIndex = rewindFrame % BUFFER_SIZE;

                // Obtain the cached input and simulation states.
                InputState rewindCachedInputState = inputStateBuffer[rewindCacheIndex];
                SimulationState rewindCachedSimulationState = simulationStateBuffer[rewindCacheIndex];

                // If there's no state to simulate, for whatever reason, 
                // increment the rewindFrame and continue.
                if (rewindCachedInputState.inputVector == null || rewindCachedSimulationState.position == null)
                {
                    ++rewindFrame;
                    continue;
                }

                // Process the cached inputs. 
                ProcessInputs(rewindCachedInputState);

                // Replace the simulationStateCache index with the new value.
                SimulationState rewoundSimulationState = CurrentSimulationState(inputState);
                rewoundSimulationState.tick = rewindFrame;
                simulationStateBuffer[rewindCacheIndex] = rewoundSimulationState;

                // Increase the amount of frames that we've rewound.
                ++rewindFrame;
            }
        }

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedTick = serverSimulationState.tick;
    }
}