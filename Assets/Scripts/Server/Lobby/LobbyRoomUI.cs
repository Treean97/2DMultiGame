using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private LobbyManager _LobbyManager;

    [Header("UI")]
    [SerializeField] private TMP_Text _RoomTitleText;
    [SerializeField] private TMP_Text _PlayersText;
    [SerializeField] private Button _LeaveButton;

    void Awake()
    {
        if (_LeaveButton != null)
            _LeaveButton.onClick.AddListener(() => _ = OnClickLeave());
    }

    void OnEnable()
    {
        if (_LobbyManager != null)
        {
            _LobbyManager.OnLobbyUpdated += HandleLobbyUpdated;
            HandleLobbyUpdated(_LobbyManager._CurrentLobby); // 진입 시 1회 반영
        }
    }

    void OnDisable()
    {
        if (_LobbyManager != null)
            _LobbyManager.OnLobbyUpdated -= HandleLobbyUpdated;
    }

    void HandleLobbyUpdated(Lobby lobby)
    {
        if (lobby == null) return;

        if (_RoomTitleText != null)
            _RoomTitleText.text = $"Room: {lobby.Name}";

        if (_PlayersText != null)
        {
            var sb = new StringBuilder();
            var players = lobby.Players;

            sb.AppendLine($"Players: {(players != null ? players.Count : 0)}");
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];

                    string name = p.Id; // fallback
                    if (p.Data != null && p.Data.TryGetValue("username", out var u) && !string.IsNullOrWhiteSpace(u.Value))
                        name = u.Value;

                    sb.AppendLine($"- {name}");
                }
            }

            _PlayersText.text = sb.ToString();
        }
    }

    async Task OnClickLeave()
    {
        if (_LobbyManager == null) return;
        await _LobbyManager.LeaveAsync();
        // 화면 전환은 LobbyUIManager가 OnLobbyLeft로 처리
    }
}
