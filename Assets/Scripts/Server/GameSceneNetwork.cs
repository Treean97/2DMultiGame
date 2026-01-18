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
        // Host는 로비(Start 버튼)에서 만들어둔 Allocation을 사용해야 함
        Allocation alloc = _LobbyManager._HostAllocation;
        if (alloc == null)
        {
            Debug.LogError("[GameNet] Host allocation null. 로비에서 Start 시 CreateAllocationAsync를 호출하고 캐시했는지 확인.");
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

        await Task.CompletedTask; // async 시그니처 유지(추후 확장 대비)
    }

    async Task StartClientAsync()
    {
        // Client는 로비 데이터에서 캐시한 JoinCode로 JoinAllocation
        string joinCode = _LobbyManager._RelayJoinCode;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[GameNet] relayJoinCode empty. 씬 로드 전 LobbyRoomUIManager에서 캐시했는지 확인.");
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
