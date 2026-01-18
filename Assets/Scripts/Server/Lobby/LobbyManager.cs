using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager _Inst { get; private set; }
    public Lobby _CurrentLobby { get; private set; }

    public event Action<Lobby> OnLobbyEntered; // Create/Join 성공
    public event Action<Lobby> OnLobbyUpdated; // 2초 폴링 갱신
    public event Action OnLobbyLeft; // Leave 성공
    public event Action<List<Lobby>> OnLobbyListUpdated; // Query 결과

    float _HeartbeatTimer;
    const float HEARTBEAT_INTERVAL = 15f;

    float _PollTimer;
    const float POLL_INTERVAL = 2f; // 매 n초 갱신

    public Allocation _HostAllocation { get; private set; } // Host만 세팅
    public string _RelayJoinCode { get; private set; }      // Host/Client 모두 사용


    void Awake()
    {
        if (_Inst != null) { Destroy(gameObject); return; }
        _Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (_CurrentLobby == null) return;

        // 호스트만 heartbeat
        if (_CurrentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            _HeartbeatTimer += Time.unscaledDeltaTime;
            if (_HeartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _HeartbeatTimer = 0f;
                _ = SendHeartbeatSafe();
            }
        }

        // 모두 2초 폴링
        _PollTimer += Time.unscaledDeltaTime;
        if (_PollTimer >= POLL_INTERVAL)
        {
            _PollTimer = 0f;
            _ = PollCurrentLobbySafe();
        }
    }

    public void SetRelayAsHost(Allocation allocation, string joinCode)
    {
        _HostAllocation = allocation;
        _RelayJoinCode = joinCode;
    }

    public void SetRelayJoinCode(string joinCode)
    {
        _RelayJoinCode = joinCode;
    }

    public void ClearRelayCache()
    {
        _HostAllocation = null;
        _RelayJoinCode = null;
    }

    async Task SendHeartbeatSafe()
    {
        try { await LobbyService.Instance.SendHeartbeatPingAsync(_CurrentLobby.Id); }
        catch (Exception e) { Debug.LogWarning($"[Lobby] Heartbeat failed: {e.Message}"); }
    }

    async Task PollCurrentLobbySafe()
    {
        if (_CurrentLobby == null) return;

        try
        {
            _CurrentLobby = await LobbyService.Instance.GetLobbyAsync(_CurrentLobby.Id);
            OnLobbyUpdated?.Invoke(_CurrentLobby);
        }
        catch (Exception e)
        {
            // 실패
            Debug.LogWarning($"[Lobby] Poll failed: {e.Message}");
        }
    }

    void SetLobby(Lobby lobby, bool fireEntered)
    {
        _CurrentLobby = lobby;
        _HeartbeatTimer = 0f;
        _PollTimer = 0f;

        if (fireEntered) OnLobbyEntered?.Invoke(_CurrentLobby);
        OnLobbyUpdated?.Invoke(_CurrentLobby);
    }

    public async Task<bool> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPublic)
    {
        await UgsBootstrap.EnsureInitAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            throw new InvalidOperationException("Not signed in.");

        try
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = !isPublic,
                Player = new Player(AuthenticationService.Instance.PlayerId, data: new Dictionary<string, PlayerDataObject>
                {
                    { "username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthManager._Inst.Username ?? "") }
                }),
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            SetLobby(lobby, fireEntered: true);

            Debug.Log($"[Lobby] Created. name={_CurrentLobby.Name}, code={_CurrentLobby.LobbyCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] Create failed: {e}");
            return false;
        }
    }

    public async Task<bool> JoinByIdAsync(string lobbyId)
    {
        await UgsBootstrap.EnsureInitAsync();

        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player(AuthenticationService.Instance.PlayerId, data: new Dictionary<string, PlayerDataObject>
                {
                    { "username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthManager._Inst.Username ?? "") }
                }),
            };

            var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            SetLobby(lobby, fireEntered: true);

            Debug.Log($"[Lobby] Joined by id. name={_CurrentLobby.Name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] JoinById failed: {e}");
            return false;
        }
    }

    public async Task<bool> JoinByCodeAsync(string code)
    {
        await UgsBootstrap.EnsureInitAsync();

        try
        {
            var options = new JoinLobbyByCodeOptions
            {
                Player = new Player(AuthenticationService.Instance.PlayerId, data: new Dictionary<string, PlayerDataObject>
                {
                    { "username", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthManager._Inst.Username ?? "") }
                }),
            };

            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
            SetLobby(lobby, fireEntered: true);

            Debug.Log($"[Lobby] Joined. name={_CurrentLobby.Name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] JoinByCode failed: {e}");
            return false;
        }
    }

    public async Task QueryAndNotifyAsync(int count = 20)
    {
        await UgsBootstrap.EnsureInitAsync();

        try
        {
            var res = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = count });
            OnLobbyListUpdated?.Invoke(res.Results ?? new List<Lobby>());
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] Query failed: {e}");
            OnLobbyListUpdated?.Invoke(new List<Lobby>());
        }
    }

    public async Task<bool> LeaveAsync()
    {
        await UgsBootstrap.EnsureInitAsync();

        if (_CurrentLobby == null) return true;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);

            
            _CurrentLobby = null;
            _HeartbeatTimer = 0f;
            _PollTimer = 0f;           
            OnLobbyLeft?.Invoke();
            ClearRelayCache();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] Leave failed: {e}");
            return false;
        }
    }
}
