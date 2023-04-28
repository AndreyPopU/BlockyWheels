using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;
using UnityEngine.UI;

public class LobbyListManager : MonoBehaviour
{
    public GameObject lobbyItemPrefab;
    public GameObject content;

    CallResult<LobbyMatchList_t> lobbyList;

    private void Awake()
    {
        if (!SteamManager.Initialized) return;

        lobbyList = CallResult<LobbyMatchList_t>.Create(OnLobbyListRequested);
    }

    public void OnLobbyListRequested(LobbyMatchList_t callback, bool bioFailure)
    {
        if (bioFailure) { Debug.LogError("Failed to obtain lobby list"); return; }

        print("refreshing lobbies");
        RefreshLobbies(callback);
    }

    public void ClearLobbies()
    {
        List<Transform> oldLobbies = new List<Transform>();
        oldLobbies.AddRange(content.GetComponentsInChildren<Transform>());
        oldLobbies.RemoveAt(0);

        foreach (Transform lobby in oldLobbies)
            Destroy(lobby.gameObject);
    }

    public void RefreshLobbies(LobbyMatchList_t callback)
    {
        ClearLobbies();

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.sizeDelta = Vector2.zero;
        List<Transform> rectChildren = new List<Transform>();
        rectChildren.AddRange(content.GetComponentsInChildren<Transform>());
        rectChildren.RemoveAt(0);

        foreach (Transform child in rectChildren)
            Destroy(child.gameObject);

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            GameObject itemInstance = Instantiate(lobbyItemPrefab);
            LobbyItem itemLogic = itemInstance.GetComponent<LobbyItem>();
            CSteamID lobbyID = new CSteamID(SteamMatchmaking.GetLobbyByIndex(i).m_SteamID);

            // Set lobby values in item
            itemLogic.lobbyName = SteamMatchmaking.GetLobbyData(lobbyID, "name");
            itemLogic.password = SteamMatchmaking.GetLobbyData(lobbyID, "password");
            itemLogic.membersText.text = SteamMatchmaking.GetNumLobbyMembers(lobbyID) + "/" + SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
            itemLogic.steamLobbyID = SteamMatchmaking.GetLobbyByIndex(i).m_SteamID;
            itemLogic.password = "";
            itemLogic.SetValues();

            // Align it in UI
            itemInstance.transform.SetParent(this.content.transform);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentRect.sizeDelta.y + 80);
            itemInstance.transform.localPosition = new Vector3(0, 1920 -120 * i + 1, 0);
            itemInstance.transform.localScale = Vector3.one;
        }
    }

    public void RefreshLobbies()
    {
        lobbyList.Set(SteamMatchmaking.RequestLobbyList());
    }

}
