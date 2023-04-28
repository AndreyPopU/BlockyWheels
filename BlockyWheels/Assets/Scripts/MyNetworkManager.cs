using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] private CarMovement localPlayerPrefab;
    public List<CarMovement> players { get; } = new List<CarMovement>();
    public float[] carLanes;
    public static bool multiplayer;

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby" || SceneManager.GetActiveScene().name == "CampaignScene")
        {
            CarMovement localPlayerInstance = Instantiate(localPlayerPrefab, new Vector3(46, -40, carLanes[players.Count]), Quaternion.Euler(new Vector3(0, 180, 0)));
            localPlayerInstance.connectionID = conn.connectionId;
            localPlayerInstance.playerIDNumber = players.Count + 1;
            localPlayerInstance.playerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.instance.lobbyID, players.Count);
            localPlayerInstance.carNameText.text = localPlayerInstance.playerName;
            localPlayerInstance.index = players.Count;

            NetworkServer.AddPlayerForConnection(conn, localPlayerInstance.gameObject);
            NetworkServer.SetClientReady(conn);
        }
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.identity != null)
        {
            CarMovement player = conn.identity.GetComponent<CarMovement>();
            players.Remove(player);
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        players.Clear();
        base.OnStopServer();
    }

    public void StartGame(string name)
    {
        SteamMatchmaking.SetLobbyJoinable(new CSteamID(LobbyManager.instance.lobbyID), false);
        ServerChangeScene(name);
    }
}
