using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItem : MonoBehaviour
{
    public string lobbyName;
    public ulong steamLobbyID;
    public string password;

    public Text lobbyNameText;
    public Text membersText;
    
    public void JoinLobby()
    {
        if (password != string.Empty)
        {
            // Type password
        }
        else
        {
            MyNetworkManager.multiplayer = true;
            SteamLobby.instance.JoinLobby(steamLobbyID);
        }
    }

    public void SetValues()
    {
        lobbyNameText.text = lobbyName;
    }
}
