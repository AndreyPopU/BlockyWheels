using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Mirror;
using UnityEngine.SceneManagement;

public class LobbySlot : NetworkBehaviour
{
    protected Callback<AvatarImageLoaded_t> imageLoaded;

    public string playerName;
    public int slotID;
    public int connectionID;
    public ulong playerSteamID;
    public ulong lobbyID;
    public bool avatarReceived;
    public bool ready;

    // UI
    public Text nameText;
    public RawImage playerIcon;
    public GameObject startButton; // Visible only to host
    public GameObject kickButton; // Visible for host on other members, but not him
    public GameObject readyButton; // Visible only to clients
    public GameObject leaveButton; // Visible only for the client

    public Text readyText;
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
        imageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    public void ChangeReadyStatus()
    {
        if (ready)
        {
            readyText.text = "Ready";
            readyText.color = Color.green;
        }
        else
        {
            readyText.text = "Unready";
            readyText.color = Color.red;
        }
    }

    public void SetPlayerValues()
    {
        nameText.text = playerName;
        ChangeReadyStatus();
        if (!avatarReceived) GetPlayerIcon();
    }

    private void OnImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID.m_SteamID != playerSteamID) return; // not us
        playerIcon.texture = GetSteamImageAsTexture(callback.m_iImage);
    }

    private void GetPlayerIcon()
    {
        int imageID = SteamFriends.GetLargeFriendAvatar((CSteamID)playerSteamID);
        playerIcon.texture = GetSteamImageAsTexture(imageID);
    }

    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if (isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        avatarReceived = true;
        return texture;
    }

    public void KickPlayer()
    {
        int index = 0;
        for (int i = 0; i < NetworkManager.players.Count; i++)
        {
            if (NetworkManager.players[i].playerSteamID == playerSteamID) { index = i; break; }
        }

        NetworkManager.players[index].kicked = true;
    }

    public void LeaveLobby()
    {
        int index = 0;
        for (int i = 0; i < NetworkManager.players.Count; i++)
        {
            if (NetworkManager.players[i].playerSteamID == playerSteamID) { index = i; break; }
        }
        NetworkManager.players[index].LeaveLobby();
    }

    public void StartGame()
    {
        if (!LobbyManager.instance.allReady) return;

        LobbyManager.instance.localPlayerInstance.ChangeStart();
        Invoke("StartGameForReal", .5f);
    }

    private void StartGameForReal()
    {
        NetworkManager.StartGame("GeneratedScene");
    }
}
