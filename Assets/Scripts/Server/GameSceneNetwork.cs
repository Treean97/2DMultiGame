using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class GameSceneNetwork : MonoBehaviour
{
    LobbyManager _LobbyManager;
    GameSessionManager _SessionManager;
    NetworkManager _NetworkManager;
    UnityTransport _Transport;

    async void Start()
    {
        try
        {
            await UgsBootstrap.EnsureInitAsync();

            if (!TryCacheRefs()) return;

            bool isHost = (_LobbyManager._CurrentLobby.HostId == AuthenticationService.Instance.PlayerId);

            if (isHost) await StartHostAsync();
            else await StartClientAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameNet] Exception: {e}");
        }
    }

    bool TryCacheRefs()
    {
        _LobbyManager = LobbyManager._Inst;
        if (_LobbyManager == null || _LobbyManager._CurrentLobby == null)
        {
            Debug.LogError("[GameNet] LobbyManager/CurrentLobby null. 로비 씬을 거쳐서 들어왔는지 확인.");
            return false;
        }

        _SessionManager = GameSessionManager._Inst;
        if (_SessionManager == null)
        {
            Debug.LogError("[GameNet] GameSessionManager null. 로비 씬에서 생성되어 DontDestroyOnLoad로 유지돼야 함.");
            return false;
        }

        _NetworkManager = NetworkManager.Singleton;
        if (_NetworkManager == null)
        {
            Debug.LogError("[GameNet] NetworkManager.Singleton null. GameScene에 NetworkManager가 필요.");
            return false;
        }

        _Transport = _NetworkManager.GetComponent<UnityTransport>();
        if (_Transport == null)
        {
            Debug.LogError("[GameNet] UnityTransport missing. NetworkManager 오브젝트에 UnityTransport 추가 필요.");
            return false;
        }

        return true;
    }

    async Task StartHostAsync()
    {
        Allocation alloc = _SessionManager._HostAllocation;
        if (alloc == null)
        {
            Debug.LogError("[GameNet] Host allocation null. Room Start에서 Allocation 캐시가 됐는지 확인.");
            return;
        }

        _Transport.SetHostRelayData(
            alloc.RelayServer.IpV4,
            (ushort)alloc.RelayServer.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData,
            isSecure: true
        );

        _NetworkManager.StartHost();
        Debug.Log("[GameNet] StartHost ok (Relay)");

        await Task.CompletedTask;
    }

    async Task StartClientAsync()
    {
        string joinCode = _SessionManager._RelayJoinCode;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[GameNet] relayJoinCode empty. 룸 폴링에서 JoinCode 캐시가 됐는지 확인.");
            return;
        }

        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        _Transport.SetClientRelayData(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData,
            isSecure: true
        );

        _NetworkManager.StartClient();
        Debug.Log("[GameNet] StartClient ok (Relay)");
    }
}
