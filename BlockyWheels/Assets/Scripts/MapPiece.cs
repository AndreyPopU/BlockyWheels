using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MapPiece : NetworkBehaviour
{
    public enum Type { Crossroad, Crosswalk, Road}

    public Type type;

    public float start, middle, end;
    public bool spawned;
    public bool spawnedPack = true;
    public GameObject[] obstacles;
    public Transform[] grassTiles;

    [Header("Optional stuff")]
    public Transform packSpawnPosition;
    public Transform packSpawnPosition1;
    public Transform preSpawnedPack;

    public LevelGenerator levelGenerator;

    // Generating levels has turned into a bit of a mess so I feel like I should declare how it works in case I come back to it (I did - future me) 
    // By default, when you spawn a road segment, it spawns a car pack, power ups and events (if the conditions are met)
    // But in the case of singleplayer, I want the levels to be the same, so there is a guarantee that the player can improve,
    // get better times and the levels aren't based on luck, so for that purpose there is a custom function that activates already spawned cars - SpawnPack(pack);

    // Aside from that there are two bools - spawned and spawnedPack; spawned checks if a road segment is spawned along with cars, power ups, events, etc.
    // spawnedPack on the other hand checks if you have spawned ONLY the car pack and by default is true, don't touch it unless it's for singleplayer levels

    // Bottom line: This will work on it's own for multiplayer, you don't need to change anything
    // For singleplayer go to the inspector: spawned must be true and spawnedPack must be false

    void Start()
    {
        levelGenerator = FindObjectOfType<LevelGenerator>();

        //if (packSpawnPosition != null)
        //{
        //    levelGenerator.StartCoroutine(levelGenerator.SpawnPack(packSpawnPosition.position, Quaternion.Euler(0, -90, 0), Random.Range(20, 25), true));
        //    levelGenerator.StartCoroutine(levelGenerator.SpawnPack(packSpawnPosition1.position, Quaternion.Euler(0, 90, 0), Random.Range(20, 25), true));
        //}

        if (!isServer) return;

        // Spawn tree - Index(3)
        SpawnTrees();

        if (type == Type.Crossroad) return; // If crossroad don't spawn anything else

        // Spawn humans - Index(length - 1)
        if (type == Type.Crosswalk) StartCoroutine(SpawnPedestrians());

        // Spawn bin/dumpster - Index (1,2)
        StartCoroutine(SpawnTrashCans());

        // Spawn light poles - Index(4)
        StartCoroutine(SpawnLightPoles());

        // Spawn parked cars - Index(0)
        StartCoroutine(SpawnParkedCars());
    }

    IEnumerator SpawnParkedCars()
    {
        float currentX = transform.position.x + start - 10;

        while (currentX > transform.position.x + end)
        {
            GameObject parkedCar = Instantiate(obstacles[0], new Vector3(currentX, 0.2f, -17.5f), Quaternion.Euler(0, -180, 0));
            NetworkServer.Spawn(parkedCar);

            currentX -= 55;

            yield return null;
        }
    }

    IEnumerator SpawnLightPoles()
    {
        float currentX = transform.position.x + start - 7;

        while (currentX > transform.position.x + end)
        {
            float randomZ = 11;
            float rotationY = -90;

            GameObject lightpole1 = Instantiate(obstacles[4], new Vector3(currentX, -.5f, randomZ), Quaternion.Euler(0, rotationY, 0));
            NetworkServer.Spawn(lightpole1);
            randomZ = -19.5f;
            rotationY = 90;

            GameObject lightpole2 = Instantiate(obstacles[4], new Vector3(currentX, -.5f, randomZ), Quaternion.Euler(0, rotationY, 0));
            NetworkServer.Spawn(lightpole2);

            currentX -= 40;

            yield return null;
        }
    }

    IEnumerator SpawnTrashCans()
    {
        float currentX = transform.position.x + start - 4;

        while (currentX > transform.position.x + end)
        {
            int random = Random.Range(1, 3);
            float randomZ = 11;
            float rotationY = -90;
            if (Random.Range(1, 3) == 2)
            {
                randomZ = -19.5f;
                rotationY = 90;
            }

            GameObject thrashCan = Instantiate(obstacles[random], new Vector3(currentX, -.25f, randomZ), Quaternion.Euler(0, rotationY, 0));
            NetworkServer.Spawn(thrashCan);

            currentX -= 30;

            yield return null;
        }
    }

    void SpawnTrees()
    {
        for (int i = 0; i < grassTiles.Length; i++)
        {
            GameObject tree = Instantiate(obstacles[3], grassTiles[i].position + Vector3.forward * .75f, Quaternion.Euler(0, Random.Range(0, 359), 0));
            NetworkServer.Spawn(tree);
        }
    }

    IEnumerator SpawnPedestrians()
    {
        // 1.5f x distance between -2 to 6
        float xDistance = -2;
        int loops = 8;

        while(loops > 0)
        {
            float z = Random.Range(10, -20);

            GameObject human = Instantiate(obstacles[obstacles.Length - 1], new Vector3(transform.position.x + middle + xDistance, -.5f, z), Quaternion.identity);
            NetworkServer.Spawn(human);

            xDistance += 1.5f;
            if (xDistance > 6) xDistance = -2;
            loops--;
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CarMovement>())
        {
            if (!spawnedPack)
                SpawnPack(preSpawnedPack);

            if (spawned) return;

            if (isServer) levelGenerator.SpawnPiece();
            else CmdSpawnPiece();

            spawned = true;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnPiece()
    {
        FindObjectOfType<LevelGenerator>();
        levelGenerator.SpawnPiece();
    }

    public void SpawnPack(Transform pack)
    {
        if (pack == null) return;

        for (int i = 0; i < pack.childCount; i++)
        {
            pack.GetChild(i).gameObject.SetActive(true);
        }
    }
}
