using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Create Room UI")]
    [SerializeField] private TMP_InputField _RoomNameInput;
    [SerializeField] private Button _CreateButton;

    [Header("List UI")]
    [SerializeField] private Button _RefreshButton;
    [SerializeField] private Transform _ListContent;          // ScrollView/Viewport/Content
    [SerializeField] private LobbyRoomItem _RoomItemPrefab;   // 한 줄 프리팹

    [Header("Status")]
    [SerializeField] private TMP_Text _StatusText;

    [Header("Options")]
    [SerializeField] private int _MaxPlayers = 8;
    [SerializeField] private bool _IsPublic = true;

    readonly List<LobbyRoomItem> _Spawned = new();

    void Awake()
    {
        if (_CreateButton != null) _CreateButton.onClick.AddListener(() => _ = OnClickCreate());
        if (_RefreshButton != null) _RefreshButton.onClick.AddListener(() => _ = RefreshList());

        SetStatus("Lobby UI ready.");
    }

    async Task OnClickCreate()
    {
        if (!IsReady()) return;

        string roomName = _RoomNameInput != null ? _RoomNameInput.text : "";
        roomName = (roomName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(roomName))
        {
            SetStatus("방 이름이 비어있습니다.");
            return;
        }

        SetInteractable(false);
        SetStatus("방 생성 중...");

        bool ok = await LobbyManager._Inst.CreateLobbyAsync(roomName, _MaxPlayers, _IsPublic);
        if (!ok)
        {
            SetStatus("방 생성 실패(콘솔 확인).");
            SetInteractable(true);
            return;
        }

        // 생성 성공 로그 (요청한 수준)
        var lobby = LobbyManager._Inst.CurrentLobby;
        Debug.Log($"[LobbyUI] 방 생성 성공. name={lobby.Name}, id={lobby.Id}, code={lobby.LobbyCode}");
        SetStatus($"방 생성 성공: {lobby.Name}");

        await RefreshList();
        SetInteractable(true);
    }

    async Task RefreshList()
    {
        if (!IsReady()) return;

        SetInteractable(false);
        SetStatus("로비 목록 불러오는 중...");

        List<Lobby> lobbies = await LobbyManager._Inst.QueryAsync(20);
        RebuildList(lobbies);

        SetStatus($"로비 {lobbies.Count}개 표시됨.");
        SetInteractable(true);
    }

    void RebuildList(List<Lobby> lobbies)
    {
        // 기존 UI 제거
        for (int i = 0; i < _Spawned.Count; i++)
        {
            if (_Spawned[i] != null) Destroy(_Spawned[i].gameObject);
        }
        _Spawned.Clear();

        if (_RoomItemPrefab == null || _ListContent == null) return;

        for (int i = 0; i < lobbies.Count; i++)
        {
            Lobby lobby = lobbies[i];
            LobbyRoomItem item = Instantiate(_RoomItemPrefab, _ListContent);
            item.Bind(lobby, OnClickJoinLobby);
            _Spawned.Add(item);
        }
    }

    async void OnClickJoinLobby(Lobby lobby)
    {
        if (lobby == null) return;
        if (!IsReady()) return;

        SetInteractable(false);
        SetStatus($"참가 중... ({lobby.Name})");

        bool ok = await LobbyManager._Inst.JoinByIdAsync(lobby.Id);
        if (!ok)
        {
            SetStatus("참가 실패(콘솔 확인).");
            SetInteractable(true);
            return;
        }

        var joined = LobbyManager._Inst.CurrentLobby;
        Debug.Log($"[LobbyUI] 방 참가 성공. name={joined.Name}, id={joined.Id}");
        SetStatus($"참가 성공: {joined.Name}");
        SetInteractable(true);
    }

    bool IsReady()
    {
        if (AuthManager._Inst == null)
        {
            Debug.LogError("[LobbyUI] AuthManager가 씬에 없습니다.");
            return false;
        }
        if (LobbyManager._Inst == null)
        {
            Debug.LogError("[LobbyUI] LobbyManager가 씬에 없습니다.");
            return false;
        }
        if (!AuthManager._Inst.IsSignedIn)
        {
            SetStatus("먼저 로그인해야 합니다.");
            return false;
        }
        return true;
    }

    void SetInteractable(bool interactable)
    {
        if (_CreateButton != null) _CreateButton.interactable = interactable;
        if (_RefreshButton != null) _RefreshButton.interactable = interactable;
        if (_RoomNameInput != null) _RoomNameInput.interactable = interactable;
    }

    void SetStatus(string msg)
    {
        if (_StatusText != null) _StatusText.text = msg;
        Debug.Log($"[LobbyUI] {msg}");
    }
}
