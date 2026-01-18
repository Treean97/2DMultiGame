using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
    private const string K_RelayJoinCode = "relayJoinCode";

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
            HandleLobbyUpdated(_LobbyManager._CurrentLobby);
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

        if (_StartButton != null)
            _StartButton.interactable = (lobby.HostId == AuthenticationService.Instance.PlayerId);

        if (lobby.Data != null)
        {
            // relayJoinCode 캐시 (클라이언트가 GameScene에서 사용)
            if (lobby.Data.TryGetValue(K_RelayJoinCode, out var codeObj))
            {
                string joinCode = codeObj.Value;
                if (!string.IsNullOrWhiteSpace(joinCode))
                {
                    if (GameSessionManager._Inst != null)
                        GameSessionManager._Inst.SetRelayJoinCode(joinCode);
                }
            }

            // nextScene 감지 시 씬 이동
            if (lobby.Data.TryGetValue(K_NextScene, out var sceneObj))
            {
                string sceneName = sceneObj.Value;
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                    return;
                }
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

                    string name = p.Id;
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

        if (lobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        if (GameSessionManager._Inst == null)
        {
            Debug.LogError("[Room] GameSessionManager가 씬에 없습니다. (DontDestroyOnLoad 싱글톤 필요)");
            return;
        }

        try
        {
            int maxConnections = Mathf.Max(1, lobby.MaxPlayers - 1);
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            // Host 캐시는 GameSessionManager가 담당
            GameSessionManager._Inst.SetRelayAsHost(alloc, joinCode);

            var data = new Dictionary<string, DataObject>
            {
                { K_RelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                { K_NextScene,     new DataObject(DataObject.VisibilityOptions.Member, _NextSceneName) }
            };

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
    }
}
