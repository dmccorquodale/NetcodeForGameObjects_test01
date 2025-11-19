using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject lobbyListPanel;
    [SerializeField] GameObject createLobbyPanel;

    [Header("Lobby list UI")]
    [SerializeField] Transform lobbyListContent;   // Content of ScrollView
    [SerializeField] LobbyListItemUI lobbyItemPrefab;
    [SerializeField] TMP_Text noLobbiesText;

    [Header("Create lobby UI")]
    [SerializeField] TMP_InputField lobbyNameInput;
    [SerializeField] TMP_InputField maxPlayersInput;
    [SerializeField] TMP_Text createErrorText;

    [Header("Status")]
    [SerializeField] TMP_Text statusText;

        void Start()
    {
        LobbyManager.Instance.OnLobbiesUpdated += HandleLobbiesUpdated;
        LobbyManager.Instance.OnStatusChanged += HandleStatusChanged;

        ShowCreatePanel(false);

        lobbyNameInput.text = "test name";
        maxPlayersInput.text = "2";

        // Wait for services to be ready before first refresh
        StartCoroutine(InitialRefreshRoutine());
    }

    IEnumerator InitialRefreshRoutine()
    {
        // Wait until LobbyManager exists and has finished initializing services
        while (LobbyManager.Instance == null || !LobbyManager.Instance.IsInitialized)
        {
            if (statusText != null)
                statusText.text = "Waiting for services to initialize...";

            yield return null; // wait a frame
        }

        // Now it's safe to query Lobby
        _ = LobbyManager.Instance.RefreshLobbiesAsync();
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbiesUpdated -= HandleLobbiesUpdated;
            LobbyManager.Instance.OnStatusChanged -= HandleStatusChanged;
        }
    }

    void HandleStatusChanged(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void HandleLobbiesUpdated(List<Lobby> lobbies)
    {
        // Clear old items
        foreach (Transform child in lobbyListContent)
        {
            Destroy(child.gameObject);
        }

        if (lobbies == null || lobbies.Count == 0)
        {
            if (noLobbiesText != null)
                noLobbiesText.gameObject.SetActive(true);
            return;
        }

        if (noLobbiesText != null)
            noLobbiesText.gameObject.SetActive(false);

        foreach (var lobby in lobbies)
        {
            var item = Instantiate(lobbyItemPrefab, lobbyListContent);
            item.Setup(lobby);
        }
    }

    // ---------- Button callbacks ----------

    public void OnClickRefresh()
    {
        _ = LobbyManager.Instance.RefreshLobbiesAsync();
    }

    public void OnClickOpenCreateLobby()
    {
        ShowCreatePanel(true);
    }

    public void OnClickCancelCreateLobby()
    {
        ShowCreatePanel(false);
    }

    public async void OnClickConfirmCreateLobby()
    {
        if (createErrorText != null) createErrorText.text = "";

        string name = string.IsNullOrWhiteSpace(lobbyNameInput.text)
            ? "My Lobby"
            : lobbyNameInput.text;

        int maxPlayers = 4;
        if (!int.TryParse(maxPlayersInput.text, out maxPlayers))
        {
            maxPlayers = 4;
        }
        if (maxPlayers < 2) maxPlayers = 2;

        await LobbyManager.Instance.CreateLobbyAsync(name, maxPlayers);

        ShowCreatePanel(false);
        await LobbyManager.Instance.RefreshLobbiesAsync();
    }

    void ShowCreatePanel(bool show)
    {
        if (createLobbyPanel != null) createLobbyPanel.SetActive(show);
        if (lobbyListPanel != null) lobbyListPanel.SetActive(!show);
    }

    public void HideLobbyUI()
    {
        gameObject.SetActive(false);
    }
}
