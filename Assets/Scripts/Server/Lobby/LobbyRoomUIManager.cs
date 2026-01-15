using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyRoomUIManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private LobbyManager _LobbyManager;

    [Header("UI")]
    [SerializeField] private TMP_Text _RoomTitleText;
    [SerializeField] private TMP_Text _PlayersText;
    [SerializeField] private Button _LeaveButton;

    [Header("Start Game")]
    [SerializeField] private Button _StartButton;
    private const string K_NextScene = "nextScene";   // 키
    [SerializeField] private string _NextSceneName = "GameScene"; // 값

    void Awake()
    {
        if (_LeaveButton != null)
            _LeaveButton.onClick.AddListener(() => _ = OnClickLeave());

        if (_StartButton != null)
            _StartButton.onClick.AddListener(() => _ = OnClickStart());
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

        // Start 버튼은 호스트만 활성화
        if (_StartButton != null)
            _StartButton.interactable = (lobby.HostId == AuthenticationService.Instance.PlayerId);

        // Lobby Data에 nextScene이 세팅되면 모두 씬 전환
        if (lobby.Data != null && lobby.Data.TryGetValue(K_NextScene, out var sceneObj))
        {
            string sceneName = sceneObj.Value;
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
        }

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

    async Task OnClickStart()
    {
        if (_LobbyManager == null) return;

        var lobby = _LobbyManager._CurrentLobby;
        if (lobby == null) return;

        // 호스트만 Start 가능
        if (lobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        var data = new Dictionary<string, DataObject>
        {
            { K_NextScene, new DataObject(DataObject.VisibilityOptions.Member, _NextSceneName) }
        };

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(
                lobby.Id,
                new UpdateLobbyOptions { Data = data }
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[Room] Start failed: {e}");
        }
    }

    async Task OnClickLeave()
    {
        if (_LobbyManager == null) return;
        await _LobbyManager.LeaveAsync();
        // 화면 전환은 LobbyUIManager가 OnLobbyLeft로 처리
    }
}
