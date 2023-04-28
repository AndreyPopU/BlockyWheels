using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;
using System.Linq;

public class Server : MonoBehaviour
{
    // Sync server/client time
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;

    // Dictionary of received inputs from players
    private Dictionary<NetworkConnection, Queue<InputState>> inputQueue = new Dictionary<NetworkConnection, Queue<InputState>>();
    private Dictionary<Obstacle, Queue<ObstacleLaunch>> launchDictionary = new Dictionary<Obstacle, Queue<ObstacleLaunch>>();

    void Start()
    {
        if (!NetworkServer.active) { DestroyImmediate(this); return; }

        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        NetworkServer.RegisterHandler<ObstacleLaunch>(OnObstacleInputReceived);
        //NetworkServer.ReplaceHandler<InputState>(OnClientInputReceived);
        //NetworkServer.ReplaceHandler<ObstacleInput>(OnObstacleInputReceived);
    }

    private void OnClientInputReceived(NetworkConnection conn, InputState state)
    {
        // Ensure the key exists, if it doesn't, create it.
        if (inputQueue.ContainsKey(conn) == false)
        {
            inputQueue.Add(conn, new Queue<InputState>());
        }

        inputQueue[conn].Enqueue(state);
    }

    private void OnObstacleInputReceived(NetworkConnection conn, ObstacleLaunch state)
    {
        uint netID = state.netID;

        if (netID == 0 || NetworkServer.spawned[netID] == null) { print("Spawned object net ID is Invalid!"); return; }
        Obstacle obstacle = NetworkServer.spawned[netID].GetComponent<Obstacle>();

        // If server dictionary doesn't contain obstacle, add it
        if (!launchDictionary.ContainsKey(obstacle)) 
        {
            // Add obstacle and it's state
            launchDictionary.Add(obstacle, new Queue<ObstacleLaunch>());

            obstacle.hitSpeed = state.speed;
        }
        launchDictionary[obstacle].Enqueue(state);
    }

    void Update()
    {
        if (!NetworkServer.active) return;

        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }
    }

    void HandleTick()
    {
        //
        // Launch
        //

        for (int i = 0; i < launchDictionary.Count; i++)
        {
            KeyValuePair<Obstacle, Queue<ObstacleLaunch>> entry = launchDictionary.ElementAt(i);

            Queue<ObstacleLaunch> queue = entry.Value;

            // Checks

            if (entry.Key == null)
            {
                print("Removed non existent obstacle from dictionary");
                launchDictionary.Remove(entry.Key);
                continue;
            }

            if (queue.Count <= 0) { 
                print("Queue is empty! Should not be empty");
                launchDictionary.Remove(entry.Key);
                continue; 
            }

            Obstacle obstacle = entry.Key;
            ObstaclePrediction controller = obstacle.GetComponent<ObstaclePrediction>();

            // Launch obstacle - neccessary?
            //if (!obstacle.launched) obstacle.Launch(obstacle.hitSpeed);

            // Declare the ClientInputState that we're going to be using.
            ObstacleLaunch launchState;

            // Obtain CharacterInputState's from the queue. 
            while (queue.Count > 0 && (launchState = queue.Dequeue()).velocity.magnitude > 0)
            {
                // Process the input.
                controller.ProcessState(launchState);

                // Obtain the current SimulationState.
                ObstacleState state = controller.CurrentObstacleState(launchState);

                // Send the state to all clients.
                NetworkServer.SendToAll(state);
            }
        }

        //
        // Input
        //

        foreach (KeyValuePair<NetworkConnection, Queue<InputState>> entry in inputQueue)
        {
            NetworkConnection conn = entry.Key;
            Client controller = entry.Key.identity.GetComponent<Client>();
            Queue<InputState> queue = entry.Value;

            // Declare the ClientInputState that we're going to be using.
            InputState inputState;

            // Obtain CharacterInputState's from the queue. 
            while (queue.Count > 0 && (inputState = queue.Dequeue()).inputVector != null)
            {
                // Process the input.
                controller.ProcessInputs(inputState);

                // Obtain the current SimulationState.
                SimulationState state = controller.CurrentSimulationState(inputState);

                // Send the state back to the client.

                // NOTE: I dont know which one works better
                conn.Send(state);
                //NetworkServer.SendToAll(state);
            }
        }
    }

    //public Obstacle FindCorrectObstacle(uint netID)
    //{
    //    Obstacle obstacle = null;

    //    for (int i = 0; i < obstacles.Count; i++)
    //    {
    //        if (obstacles[i].netId == netID) obstacle = obstacles[i];
    //    }

    //    return obstacle;
    //}
}