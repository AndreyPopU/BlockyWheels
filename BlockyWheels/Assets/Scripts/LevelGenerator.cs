using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LevelGenerator : NetworkBehaviour
{
    public static LevelGenerator instance;

    public int piecesSpawned = 0;

    public GameObject[] mapPieces;
    public GameObject[] obstacles;
    public GameObject eventTriggerPrefab;
    public GameObject powerUpPrefab;

    public MapPiece lastPiece;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        if (!isServer) return;

        Vector3 pos;
        int random = 0;

        if (piecesSpawned > 0)
            random = Random.Range(0, mapPieces.Length);

        if (lastPiece != null)
            pos = new Vector3(lastPiece.transform.position.x - lastPiece.end - mapPieces[random].GetComponent<MapPiece>().middle * 2, 9.35f, -24.5f);
        else pos = new Vector3(-130, 9.35f, -24.5f);

        GameObject newPiece = Instantiate(mapPieces[random], pos, Quaternion.identity);
        lastPiece = newPiece.GetComponent<MapPiece>();
        NetworkServer.Spawn(newPiece);

        //StartCoroutine(SpawnPack(new Vector3(lastPiece.transform.position.x + lastPiece.middle - 20, 0, 0), Quaternion.Euler(0, 180, 0), Random.Range(8, 15), false));

        //if (piecesSpawned > 0)
        //{
        //    if (piecesSpawned % 3 == 0) SpawnPowerUps(new Vector3(lastPiece.transform.position.x - lastPiece.middle, 0, 0));
        //    if (piecesSpawned % 5 == 0) SpawnEventTrigger(new Vector3(lastPiece.transform.position.x - lastPiece.middle, 0, 0));
        //}
        //else StartCoroutine(SpawnPack(new Vector3(lastPiece.transform.position.x - lastPiece.middle, 0, 0), Quaternion.Euler(0, 180, 0), Random.Range(8, 15), false));

        piecesSpawned++;
    }

    public IEnumerator SpawnPack(Vector3 pos, Quaternion rotation, int randomCars, bool vertical)
    {
        int lastLane = 0;

        while(randomCars > 0)
        {
            // Select random car and random lane
            int randomObstacle = Random.Range(0, obstacles.Length);
            int randomLane = Random.Range(0, GameManager.instance.carLanes.Length);
            bool canChangeLane = true;
            bool _caresAboutLaw = true;

            if (randomLane == lastLane)
            {
                randomLane++;
                if (randomLane >= GameManager.instance.carLanes.Length) randomLane = 0;
            }

            lastLane = randomLane;

            if (!vertical)
            {
                if (rotation == Quaternion.identity)
                {
                    pos = new Vector3(pos.x, pos.y, GameManager.instance.carLanes[randomLane] + 31);
                    canChangeLane = false;
                    _caresAboutLaw = false;
                }
                else pos = new Vector3(pos.x, pos.y, GameManager.instance.carLanes[randomLane]);
            }

            Vector3 bonusX = Vector3.zero;
            if (vertical)
            {
                bonusX = new Vector3(GameManager.instance.carLanes[randomLane], 0, 0);
                canChangeLane = false;
            }

            GameObject obj = Instantiate(obstacles[randomObstacle], pos + bonusX, rotation, transform);
            obj.GetComponent<CarObstacle>().canChangeLane = canChangeLane;
            obj.GetComponent<CarObstacle>().caresAboutLaw = _caresAboutLaw;
            NetworkServer.Spawn(obj);

            randomCars--;
            if (vertical) pos -= Vector3.forward * 5;
            else pos -= Vector3.right * 15;

            yield return null;
        }
    }

    public void SpawnPowerUps(Vector3 pos)
    {
        GameObject obj = Instantiate(powerUpPrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(obj);
    }

    public void SpawnEventTrigger(Vector3 pos)
    {
        GameObject obj = Instantiate(eventTriggerPrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(obj);
    }
}
