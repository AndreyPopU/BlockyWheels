using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby instance;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> joinRequest;
    protected Callback<LobbyEnter_t> lobbyEnter;

    public ulong lobbyID;
    private const string HostAddress = "HostAddress";
    public string lobbyNickname;
    public string lobbyPassword;

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
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        joinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
    }

    public void CreateLobby()
    {
        lobbyPassword = FindObjectsOfType<InputField>()[0].text;
        lobbyNickname = FindObjectsOfType<InputField>()[1].text;
        FadePanel.instance.StartCoroutine(FadePanel.instance.FadeIn());
        Invoke("CreateLobbyForReal", .5f);
    }

    public void CreateLobbyForReal()
    {
        print("Creating lobby");
        MyNetworkManager.multiplayer = true;
        MyNetworkManager.singleton.onlineScene = "Lobby";
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, NetworkManager.maxConnections);
    }

    public void JoinLobby(ulong id)
    {
        MyNetworkManager.singleton.onlineScene = "Lobby";
        SteamMatchmaking.JoinLobby(new CSteamID(id));
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        NetworkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddress, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", lobbyNickname);
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "password", lobbyPassword);

        print("Lobby created successfuly " + callback.m_ulSteamIDLobby);
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        print("Incomming connection");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        // Everyone
        lobbyID = callback.m_ulSteamIDLobby;

        // Clients
        if (NetworkServer.active) { return; }

        NetworkManager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddress);

        NetworkManager.StartClient();

        print("Lobby entered: " + lobbyID);
    }
}
