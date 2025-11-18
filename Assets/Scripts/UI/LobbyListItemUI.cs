using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyListItemUI : MonoBehaviour
{
    [SerializeField] TMP_Text lobbyNameText;
    [SerializeField] TMP_Text playersText;
    [SerializeField] Button joinButton;

    Lobby _lobby;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        if (lobbyNameText != null)
            lobbyNameText.text = lobby.Name;

        if (playersText != null)
            playersText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnClickJoin);
        }
    }

    void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnClickJoin);
    }

    void OnClickJoin()
    {
        if (_lobby != null)
        {
            _ = LobbyManager.Instance.JoinLobbyAsync(_lobby);
        }
    }
}
