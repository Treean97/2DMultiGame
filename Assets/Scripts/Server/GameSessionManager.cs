using Unity.Services.Relay.Models;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager _Inst { get; private set; }

    public Allocation _HostAllocation { get; private set; } // Host만 세팅
    public string _RelayJoinCode { get; private set; }      // Host/Client 모두 사용

    void Awake()
    {
        if (_Inst != null) { Destroy(gameObject); return; }
        _Inst = this;
        DontDestroyOnLoad(gameObject);
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
}
