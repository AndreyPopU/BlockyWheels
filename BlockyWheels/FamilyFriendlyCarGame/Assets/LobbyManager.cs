using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using UnityEngine.UI;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    // Singleton
    public static LobbyManager instance;

    // Lobby name
    public ulong lobbyID;
    public Text lobbyText;

    // Player
    public GameObject localPlayerObject; // Player prefab
    public CarMovement localPlayerInstance; // Player prefab instance

    // Lobby
    public List<LobbySlot> lobbyMemberSlots = new List<LobbySlot>(); // List of lobby slots
    public GameObject playerLobbySlot; // Lobby slot
    public bool playerSlotCreated = false; // The first slot
    public Transform lobbyPanel;

    private MyNetworkManager networkManager;

    MyNetworkManager NetworkManager
    {
        get
        {
            if (networkManager != null) return networkManager;
            return networkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void UpdateLobbyName()
    {
        lobbyID = NetworkManager.GetComponent<SteamLobby>().lobbyID;
        lobbyText.text = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyID), "name");
    }

    public void FindLocalPlayer()
    {
        localPlayerObject = GameObject.Find("LocalCar");
        localPlayerInstance = localPlayerObject.GetComponent<CarMovement>();
    }

    public void UpdatePlayerList()
    {
        if (!playerSlotCreated && lobbyMemberSlots.Count < NetworkManager.players.Count) CreateHost(); // If first slot is created, it is for the host
        if (playerSlotCreated && lobbyMemberSlots.Count < NetworkManager.players.Count) CreateClient(); // If player has connected and there isn't a slot for him, create one
        if (lobbyMemberSlots.Count > NetworkManager.players.Count) RemoveClient(); // If player has disconnected and there is a slot for him, delete it
        if (lobbyMemberSlots.Count == NetworkManager.players.Count) UpdateClient(); // Update slots
    }

    public void CreateHost()
    {
        print("Creating host");

        foreach (CarMovement player in NetworkManager.players)
        {
            GameObject newLobbySlot = Instantiate(playerLobbySlot) as GameObject;
            LobbySlot slot = newLobbySlot.GetComponent<LobbySlot>();

            slot.playerName = player.playerName;
            slot.connectionID = player.connectionID;
            slot.playerSteamID = player.playerSteamID;
            slot.lobbyID = SteamLobby.instance.lobbyID;
            if (player.isServer)
            {
                slot.startButton.SetActive(true);
                slot.leaveButton.SetActive(true);
            }
            slot.SetPlayerValues();

            lobbyMemberSlots.Add(slot);

            AlignSlot(newLobbySlot.transform);
        }
        playerSlotCreated = true;
    }

    public void CreateClient()
    {
        print("Creating Client");

        foreach (CarMovement player in NetworkManager.players)
        {
            if (!lobbyMemberSlots.Any(b => b.connectionID == player.connectionID)) // If we aren't in the list, add us
            {
                GameObject newLobbySlot = Instantiate(playerLobbySlot) as GameObject;
                LobbySlot slot = newLobbySlot.GetComponent<LobbySlot>();

                slot.playerName = player.playerName;
                slot.connectionID = player.connectionID;
                slot.playerSteamID = player.playerSteamID;
                slot.lobbyID = SteamLobby.instance.lobbyID;
                if (player.isServer) slot.kickButton.SetActive(true);
                else slot.leaveButton.SetActive(true);
                slot.SetPlayerValues();

                lobbyMemberSlots.Add(slot);

                AlignSlot(newLobbySlot.transform);
            }
        }
    }

    public void RemoveClient()
    {
        List<LobbySlot> slotsToRemove = new List<LobbySlot>();

        foreach (LobbySlot slot in slotsToRemove)
        {
            if (!NetworkManager.players.Any(b => b.connectionID == slot.connectionID)) // Each player that didn't disconnect adds the disconnected player to a list
                slotsToRemove.Add(slot);
        }

        if (slotsToRemove.Count > 0)
        {
            foreach (CarMovement player in NetworkManager.players) // Each player
            {
                foreach (LobbySlot slot in slotsToRemove) // Removes every slot in slotsToRemove
                {
                    GameObject objectToRemove = slot.gameObject;
                    lobbyMemberSlots.Remove(slot);
                    Destroy(objectToRemove);
                    objectToRemove = null;
                }
            }
        }
    }

    public void UpdateClient()
    {
        foreach (CarMovement player in NetworkManager.players)
        {
            foreach (LobbySlot slot in lobbyMemberSlots)
            {
                if (slot.connectionID == player.connectionID)
                {
                    slot.playerName = player.playerName;
                    slot.SetPlayerValues();
                }
            }
        }
    }

    public void AlignSlot(Transform newLobbySlot)
    {
        newLobbySlot.SetParent(lobbyPanel);
        RectTransform rect = newLobbySlot.GetComponent<RectTransform>();
        int playerNumber = lobbyMemberSlots.Count;

        switch(playerNumber)
        {
            case 1 : rect.transform.localPosition = new Vector3(-660, -50, 0); break;
            case 2 : rect.transform.localPosition = new Vector3(-225, -50, 0); break;
            case 3 : rect.transform.localPosition = new Vector3(225, -50, 0); break;
            case 4 : rect.transform.localPosition = new Vector3(660, -50, 0); break;
        }

    }
}
