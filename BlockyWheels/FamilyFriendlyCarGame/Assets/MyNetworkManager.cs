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

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (SceneManager.GetActiveScene().name == "Lobby" || SceneManager.GetActiveScene().name == "Campaign")
        {
            CarMovement localPlayerInstance = Instantiate(localPlayerPrefab);
            localPlayerInstance.connectionID = conn.connectionId;
            localPlayerInstance.playerIDNumber = players.Count + 1;
            localPlayerInstance.playerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.instance.lobbyID, players.Count);

            NetworkServer.AddPlayerForConnection(conn, localPlayerInstance.gameObject);
        }
    }

    public void StartGame(string name)
    {
        ServerChangeScene(name);
        for (int i = 0; i < players.Count; i++)
            players[i].StartCoroutine(players[i].PrepareForRace(i));
    }
}
