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

    // Ready
    public Text readyButtonText;
    public Button startButton;
    public bool allReady;
    public bool canStartGame;

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

    public void ReadyPlayer()
    {
        localPlayerInstance.ChangeReady();
    }

    public void UpdateButton()
    {
        if (localPlayerInstance.ready)
        {
            readyButtonText.text = "Ready";
        }
        else
        {
            readyButtonText.text = "Unready";
        }
    }

    public void CheckIfAllReady()
    {
        foreach (CarMovement player in NetworkManager.players)
        {
            if (player.ready) allReady = true;
            else { allReady = false; break; }
        }

        if (startButton == null) return;

        if (allReady)
        {
            canStartGame = true;
            if (localPlayerInstance.playerIDNumber == 1) startButton.interactable = true;
            else startButton.interactable = false;
        }
        else startButton.interactable = false;
    }


    public void UpdateLobbyName()
    {
        lobbyID = FindObjectOfType<SteamLobby>().lobbyID;
        lobbyText.text = SteamMatchmaking.GetLobbyData(new CSteamID(lobbyID), "name") + "\n" + lobbyID;
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
        foreach (CarMovement player in NetworkManager.players)
        {
            GameObject newLobbySlot = Instantiate(playerLobbySlot) as GameObject;
            LobbySlot slot = newLobbySlot.GetComponent<LobbySlot>();

            slot.slotID = 1;
            slot.playerName = player.playerName;
            slot.connectionID = player.connectionID;
            slot.playerSteamID = player.playerSteamID;
            slot.lobbyID = SteamLobby.instance.lobbyID;
            player.ready = true;
            slot.ready = player.ready;

            if (player.isServer)
            {
                slot.readyText.gameObject.SetActive(false);
                slot.startButton.SetActive(true);
                startButton = slot.startButton.GetComponent<Button>();
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
        foreach (CarMovement player in NetworkManager.players)
        {
            if (!lobbyMemberSlots.Any(b => b.connectionID == player.connectionID)) // If we aren't in the list, add us
            {
                GameObject newLobbySlot = Instantiate(playerLobbySlot) as GameObject;
                LobbySlot slot = newLobbySlot.GetComponent<LobbySlot>();

                slot.slotID = lobbyMemberSlots.Count + 1;
                slot.playerName = player.playerName;
                slot.connectionID = player.connectionID;
                slot.playerSteamID = player.playerSteamID;
                slot.ready = player.ready;
                slot.lobbyID = SteamLobby.instance.lobbyID;
                if (player.isServer) slot.kickButton.SetActive(true);
                else
                {
                    if (slot.playerSteamID == localPlayerInstance.playerSteamID)
                    {
                        slot.leaveButton.SetActive(true);
                        slot.readyButton.SetActive(true);
                        slot.readyButton.GetComponent<Button>().onClick.AddListener(delegate { ReadyPlayer(); });
                    }
                }
                slot.SetPlayerValues();
                print("Creating player slot " + player.playerSteamID);
                lobbyMemberSlots.Add(slot);

                AlignSlot(newLobbySlot.transform);
            }
        }
    }

    public void RemoveClient()
    {
        List<LobbySlot> slotsToRemove = new List<LobbySlot>();

        foreach (LobbySlot slot in lobbyMemberSlots)
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
                    if (slot != null)
                    {
                        GameObject objectToRemove = slot.gameObject;
                        lobbyMemberSlots.Remove(slot);
                        Destroy(objectToRemove);
                        objectToRemove = null;
                    }
                }

                foreach (LobbySlot slot in lobbyMemberSlots) // Then align his slot
                {
                    if (slot != null) AlignSlot(slot.transform);
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
                    slot.ready = player.ready;
                    slot.SetPlayerValues();
                    if (slot == localPlayerInstance) UpdateButton();
                }
            }
        }

        CheckIfAllReady();
    }

    public void AlignSlot(Transform newLobbySlot)
    {
        newLobbySlot.SetParent(lobbyPanel, false);
        RectTransform rect = newLobbySlot.GetComponent<RectTransform>();

        int index = 0;

        for (int i = 0; i < lobbyMemberSlots.Count; i++)
        {
            if (newLobbySlot.GetComponent<LobbySlot>().playerSteamID == lobbyMemberSlots[i].playerSteamID) index = i + 1;
        }

        int playerNumber = index;

        

        switch(playerNumber)
        {
            case 1 : rect.transform.localPosition = new Vector3(300, -50, 0);
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f); 
                break; // 300 - 50
            case 2 : rect.transform.localPosition = new Vector3(-225, -50, 0); break; // 735
            case 3 : rect.transform.localPosition = new Vector3(225, -50, 0); break;
            case 4 : rect.transform.localPosition = new Vector3(-300, -50, 0);
                rect.anchorMin = new Vector2(1, 0.5f);
                rect.anchorMax = new Vector2(1, 0.5f); 
                break;
        }
    }
}
