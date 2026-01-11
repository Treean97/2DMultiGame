using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomItem : MonoBehaviour
{
    [SerializeField] private TMP_Text _RoomNameText;
    [SerializeField] private TMP_Text _PlayersText;
    [SerializeField] private Button _JoinButton;

    Lobby _Lobby;
    Action<Lobby> _OnClickJoin;

    public void Bind(Lobby lobby, Action<Lobby> onClickJoin)
    {
        _Lobby = lobby;
        _OnClickJoin = onClickJoin;

        if (_RoomNameText != null) _RoomNameText.text = lobby?.Name ?? "(no name)";
        if (_PlayersText != null)
        {
            int count = lobby?.Players?.Count ?? 0;
            int max = lobby?.MaxPlayers ?? 0;
            _PlayersText.text = $"{count}/{max}";
        }

        if (_JoinButton != null)
        {
            _JoinButton.onClick.RemoveAllListeners();
            _JoinButton.onClick.AddListener(() => _OnClickJoin?.Invoke(_Lobby));
            _JoinButton.interactable = (lobby != null);
        }
    }
}
