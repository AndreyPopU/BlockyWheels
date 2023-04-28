using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class GameEventTrigger : NetworkBehaviour
{
    public enum GameEvent { OneLane, WrongLane, AirStrike, Random }

    public GameEvent gameEvent;
    public LayerMask groundMask;
    public GameObject smokePrefab;
    public GameObject wrongLanePrefab;
    public GameObject missilePrefab;
    public GameObject[] roadObstacles;

    public bool triggered;

    private MyNetworkManager networkManager;
    MyNetworkManager NetworkManager
    {
        get
        {
            if (networkManager != null) return networkManager;
            return networkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CarMovement>() && !triggered)
        {
            if (gameEvent == GameEvent.OneLane) OneLaneEvent();
            else if (gameEvent == GameEvent.AirStrike) StartCoroutine(AirStrike());
            else if (gameEvent == GameEvent.WrongLane) 
            { 
                WrongLane(1);
                if (isServer) RpcEventText("Wrong Lane!");
                else CmdEventText("Wrong Lane!");
            }

            triggered = true;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdEventText(string message)
    {
        RpcEventText(message);
    }

    [ClientRpc]
    public void RpcEventText(string message)
    {
        StartCoroutine(EventText(message));
    }

    public IEnumerator EventText(string eventName)
    {
        Text eventText = GameManager.instance.eventText;
        eventText.gameObject.SetActive(true);
        eventText.text = eventName;
        eventText.transform.localPosition = new Vector3(2000, eventText.transform.localPosition.y, 0);
        //eventText.alignment = TextAnchor.MiddleCenter;

        if (CarMovement.instance.speedEffect == null) CarMovement.instance.speedEffect = GameManager.instance.speedEffect;
        CarMovement.instance.controlsSpeedEffect = false;
        CarMovement.instance.speedEffect.Play();

        while (eventText.transform.localPosition.x > 0)
        {
            eventText.transform.Translate(new Vector3(-75, 0, 0));
            yield return null;
        }

        eventText.transform.localPosition = Vector3.zero;

        yield return new WaitForSeconds(2);

        while (eventText.transform.localPosition.x > -2000)
        {
            eventText.transform.Translate(new Vector3(-75, 0, 0));
            yield return null;
        }

        CarMovement.instance.controlsSpeedEffect = true;
        CarMovement.instance.speedEffect.Stop();

        eventText.gameObject.SetActive(false);
    }

    public IEnumerator AirStrike()
    {
        // CameraManager.instance.centerPoint.x - 70, CameraManager.instance.centerPoint.x - 60), new Vector2(-18, 10))
        float centerPoint = transform.position.x;//GetFarthestPlayer(); //(GetFarthestPlayer() - GetSlowestPlayer()) / 2;
        Vector2 minMaxX = new Vector2(centerPoint - 70, centerPoint - 60);
        Vector2 minMaxZ = new Vector2(-18, 10);

        if (isServer) RpcEventText("Airstrike Incoming!");
        else CmdEventText("Airstrike Incoming!");

        yield return new WaitForSeconds(3.5f);

        int loops = 20;

        while (loops > 0)
        {
            loops--;
            bool raycastHit = false;

            float randomX = Random.Range(minMaxX.x, minMaxX.y);
            float randomZ = Random.Range(minMaxZ.x, minMaxZ.y);
            Vector3 position = new Vector3(randomX, 20, randomZ);

            RaycastHit hit;
            Ray ray = new Ray(position, Vector3.down);

            if (Physics.Raycast(ray, out hit, 250f, groundMask))
            {
                position.y = hit.point.y;

                raycastHit = true;
            }

            if (!raycastHit) continue;

            // Spawn rocket
            GameObject missile = Instantiate(missilePrefab, position, Quaternion.identity);
            missile.transform.position += Vector3.up * .2f;
            missile.transform.SetParent(CameraManager.instance.transform);
            missile.transform.localPosition = new Vector3(Random.Range(0, 100), missile.transform.localPosition.y, missile.transform.localPosition.z);
            missile.transform.SetParent(null);
            NetworkServer.Spawn(missile);

            yield return new WaitForSeconds(5 * Time.fixedDeltaTime);
        }
    }

    public void OneLaneEvent()
    {
        if (isServer) RpcEventText("Everyone in one lane!");
        else CmdEventText("Everyone in one lane!");

        // Despawn road obstacles in front of farthest player
        Collider[] obstacles = Physics.OverlapBox(new Vector3(transform.position.x - 110, 0, 0), new Vector3(55, 5, 25));

        foreach (Collider obstacle in obstacles)
        {
            if (obstacle.GetComponent<CarObstacle>()) NetworkServer.Destroy(obstacle.gameObject);
        }

        // Pick random lane
        int randomLane = Random.Range(0, 4);

        int forcedLane = randomLane;

        // Spawn new obstacles
        for (int i = 0; i < NetworkManager.carLanes.Length; i++)
        {
            if (i == forcedLane) continue;

            for (int j = (int)transform.position.x - 120; j > (int)transform.position.x - 216; j -= 13)
            {
                GameObject newObstacle = roadObstacles[Random.Range(0, roadObstacles.Length)];
                GameObject obstInstance = Instantiate(newObstacle, new Vector3(transform.position.x + j + 150, 0, NetworkManager.carLanes[i]), newObstacle.transform.rotation);
                obstInstance.TryGetComponent(out CarObstacle obst);
                obst.canChangeLane = false;
                obst.speed = 400;
                NetworkServer.Spawn(obstInstance);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void WrongLane(int direction)
    {
        if (isServer)
        {
            foreach (CarMovement player in NetworkManager.players)
                player.ChangeLaneNetwork(3, direction);
        }

        // Runs only if you get put in the wrong lane
        if (direction > 0)
        {
            LevelGenerator levelGenerator = FindObjectOfType<LevelGenerator>();
            levelGenerator.StartCoroutine(levelGenerator.SpawnPack(new Vector3(transform.position.x - 300, 0, 31.3f), Quaternion.identity, Random.Range(25, 35), false));
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(new Vector3(transform.position.x - 110, 0, 0), new Vector3(110, 5, 25));
    }
}
