using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Mirror;
using UnityEngine.SceneManagement;

public class LobbySlot : MonoBehaviour
{
    public string playerName;
    public int connectionID;
    public ulong playerSteamID;
    public ulong lobbyID;

    // UI
    public Text nameText;
    public GameObject startButton; // Only visible for host
    public GameObject kickButton; // Visible for host on other members, but not him
    public GameObject leaveButton; // Visible only for the client

    private MyNetworkManager networkManager;

    MyNetworkManager NetworkManager
    {
        get
        {
            if (networkManager != null) return networkManager;
            return networkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    private void Start()
    {

    }

    public void SetPlayerValues()
    {
        nameText.text = playerName;
    }

    public void LeaveLobby()
    {
        int index = 0;
        for (int i = 0; i < NetworkManager.players.Count; i++)
        {
            if (NetworkManager.players[i].playerSteamID == playerSteamID) { index = i; break; }
        }
        networkManager.players[index].GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
        NetworkManager.players.RemoveAt(index);
    }

    public void StartGame()
    {
        NetworkManager.StartGame("Level2");
    }
}
