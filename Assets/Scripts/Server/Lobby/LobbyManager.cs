using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager _Inst { get; private set; }
    public Lobby CurrentLobby { get; private set; }

    float _HeartbeatTimer;
    const float HEARTBEAT_INTERVAL = 15f;

    void Awake()
    {
        if (_Inst != null) { Destroy(gameObject); return; }
        _Inst = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (CurrentLobby == null) return;
        if (CurrentLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        _HeartbeatTimer += Time.unscaledDeltaTime;
        if (_HeartbeatTimer >= HEARTBEAT_INTERVAL)
        {
            _HeartbeatTimer = 0f;
            _ = SendHeartbeatSafe();
        }
    }

    async Task SendHeartbeatSafe()
    {
        try { await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id); }
        catch (Exception e) { Debug.LogWarning($"[Lobby] Heartbeat failed: {e.Message}"); }
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
                Player = new Player(AuthenticationService.Instance.PlayerId),
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"[Lobby] Created. name={CurrentLobby.Name}, code={CurrentLobby.LobbyCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] Create failed: {e}");
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
                Player = new Player(AuthenticationService.Instance.PlayerId),
            };
            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
            Debug.Log($"[Lobby] Joined. name={CurrentLobby.Name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] JoinByCode failed: {e}");
            return false;
        }
    }

    public async Task<List<Lobby>> QueryAsync(int count = 20)
    {
        await UgsBootstrap.EnsureInitAsync();
        try
        {
            var res = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions { Count = count });
            return res.Results ?? new List<Lobby>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] Query failed: {e}");
            return new List<Lobby>();
        }
    }

    public async Task<bool> JoinByIdAsync(string lobbyId)
    {
        await UgsBootstrap.EnsureInitAsync();

        try
        {
            var options = new JoinLobbyByIdOptions
            {
                Player = new Player(AuthenticationService.Instance.PlayerId),
            };

            CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            Debug.Log($"[Lobby] Joined by id. name={CurrentLobby.Name}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Lobby] JoinById failed: {e}");
            return false;
        }
    }
}
